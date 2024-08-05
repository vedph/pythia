using Corpus.Core;
using Corpus.Sql;
using Fusi.Tools;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Core;
using Pythia.Cli.Plugin.Standard;
using Pythia.Cli.Services;
using Pythia.Core.Analysis;
using Pythia.Core.Config;
using Pythia.Sql;
using Pythia.Sql.PgSql;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class IndexCommand : AsyncCommand<IndexCommandSettings>
{
    private static IndexContents ParseIndexContents(string? text)
    {
        IndexContents contents = IndexContents.None;
        string cnt = text?.ToUpperInvariant() ?? "TS";
        if (cnt.IndexOf('T') > -1) contents |= IndexContents.Tokens;
        if (cnt.IndexOf('S') > -1) contents |= IndexContents.Structures;
        return contents;
    }

    private static void DumpFilteredText(string dir, string source, string text)
    {
        string path = Path.Combine(dir, Path.GetFileNameWithoutExtension(source)
            + ".dump.txt");
        using StreamWriter writer = new(path, false, Encoding.UTF8);
        writer.Write(text);
        writer.Flush();
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        IndexCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red underline]INDEX[/]");
        AnsiConsole.MarkupLine($"Profile ID: [cyan]{settings.ProfileId}[/]");
        AnsiConsole.MarkupLine($"Source: [cyan]{settings.Source}[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");
        AnsiConsole.MarkupLine($"Contents: [cyan]{settings.Contents ?? "TS"}[/]");
        AnsiConsole.MarkupLine($"Store content: [cyan]{settings.IsContentStored}[/]");
        AnsiConsole.MarkupLine($"Preflight: [cyan]{settings.IsDry}[/]");
        AnsiConsole.MarkupLine($"Plugin tag: [cyan]{settings.PluginTag}[/]");
        if (settings.DumpMode > 0)
        {
            AnsiConsole.MarkupLine($"Dump mode: [cyan]{settings.DumpMode}[/]");
            AnsiConsole.MarkupLine($"Dump dir: [cyan]{settings.DumpDir}[/]");
        }

        try
        {
            string cs = string.Format(
                CliAppContext.Configuration!.GetConnectionString("Default")!,
                settings.DbName);

            SqlIndexRepository repository = new PgSqlIndexRepository();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });

            IProfile? profile = repository.GetProfile(settings.ProfileId!)
                ?? throw new ArgumentException("Profile ID not found: "
                    + settings.ProfileId);

            ICliPythiaFactoryProvider? factoryProvider =
                (string.IsNullOrEmpty(settings.PluginTag)
                ? new StandardCliPythiaFactoryProvider()
                : PluginPythiaFactoryProvider.GetFromTag(settings.PluginTag))
                ?? throw new FileNotFoundException(
                    $"The requested tag {settings.PluginTag} was not found " +
                    "among plugins in " +
                    PluginPythiaFactoryProvider.GetPluginsDir());

            PythiaFactory factory = factoryProvider.GetFactory(
                profile.Id!, profile.Content!, cs);

            IndexBuilder builder = new(factory, repository)
            {
                Contents = ParseIndexContents(settings.Contents),
                IsDryMode = settings.IsDry,
                IsContentStored = settings.IsContentStored,
                Logger = CliAppContext.Logger
            };

            // dump mode
            if (!string.IsNullOrEmpty(settings.DumpDir) &&
                !Directory.Exists(settings.DumpDir))
            {
                Directory.CreateDirectory(settings.DumpDir);
            }
            switch (settings.DumpMode)
            {
                case 1:
                    builder.FilteredTextCallback = (source, filtered) =>
                    {
                        DumpFilteredText(settings.DumpDir ?? "", source, filtered);
                        return true;
                    };
                    break;
                case 2:
                    builder.FilteredTextCallback = (source, filtered) =>
                    {
                        DumpFilteredText(settings.DumpDir ?? "", source, filtered);
                        return false;
                    };
                    break;
            }

            await AnsiConsole.Status().Start("Indexing...", async ctx =>
            {
                ctx.Status("Building index");
                ctx.Spinner(Spinner.Known.Star);

                await builder.Build(profile.Id!, settings.Source!,
                    CancellationToken.None,
                    new Progress<ProgressReport>(report =>
                    {
                        AnsiConsole.MarkupLine(
                            $"[yellow]{report.Count}[/] " +
                            $"[green]{DateTime.Now:HH:mm:ss}[/] " +
                            $"[cyan]{report.Message}[/]");
                    }));

                if (!settings.IsDry)
                {
                    ctx.Status("Finalizing index...");
                    repository.FinalizeIndex();
                }
            });

            AnsiConsole.MarkupLine("[green]Completed[/]");
            return 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}

public class IndexCommandSettings : CommandSettings
{
    [Description("The ID of the profile to use")]
    [CommandArgument(0, "<PROFILE_ID>")]
    public string? ProfileId { get; set; }

    [Description("The documents source")]
    [CommandArgument(1, "<SOURCE>")]
    public string? Source { get; set; }

    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; }

    [Description("Content to index: T=token, S=structure")]
    [CommandOption("-c|--contents <TS>")]
    [DefaultValue("TS")]
    public string? Contents { get; set; }

    [Description("Store document's content in database")]
    [CommandOption("-o|--store-content")]
    public bool IsContentStored { get; set; }

    [Description("Preflight mode: do not write to database")]
    [CommandOption("-p|--preflight|--dry")]
    public bool IsDry { get; set; }

    [Description("The factory provider plugin tag")]
    [CommandOption("-t|--tag <PLUGIN_TAG>")]
    public string? PluginTag { get; set; }

    [Description("The optional dump mode to use: 0=none, " +
        "1=dump filtered, 2=dump filtered and don't index")]
    [CommandOption("-u|--dump <DUMP_MODE>")]
    public int DumpMode { get; set; }

    [Description("The directory to dump filtered texts to when dumping is enabled")]
    [CommandOption("-r|--dump-dir <DUMP_DIR>")]
    public string? DumpDir { get; set; }

    public IndexCommandSettings()
    {
        DbName = "pythia";
    }
}
