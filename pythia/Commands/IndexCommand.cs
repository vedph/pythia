using Corpus.Core;
using Corpus.Sql;
using Fusi.Tools;
using MessagingApi.Extras;
using MessagingApi.Mailjet;
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

/// <summary>
/// Builds the Pythia index based on the specified configuration and parameters.
/// </summary>
internal sealed class IndexCommand : AsyncCommand<IndexCommandSettings>
{
    private MessageSink? _sink;

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

    private async Task Notify(MessageSinkEntry entry)
    {
        if (_sink == null) return;

        try
        {
            await _sink.AddEntry(entry);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Notification error: ", ex.ToString());
            AnsiConsole.WriteException(ex);
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        IndexCommandSettings settings, CancellationToken cancel)
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

        // setup notification if requested
        if (!string.IsNullOrEmpty(settings.NotifierEmail))
        {
            if (string.IsNullOrEmpty(settings.NotifierEmail))
            {
                AnsiConsole.MarkupLine("[red]Notifier email not set[/]");
                return 1;
            }

            AnsiConsole.MarkupLine(
                $"Notifier email: [cyan]{settings.NotifierEmail}[/]");
            AnsiConsole.MarkupLine(
                $"Notifier span: [cyan]{settings.NotifierSpan}[/]");
            AnsiConsole.MarkupLine(
                $"Notifier tail limit: [cyan]{settings.NotifierLimit}[/]");

            _sink = new(new MailjetMailerService(
                new MailjetMailerOptions
                {
                    SenderEmail = settings.NotifierEmail,
                    SenderName = "Pythia Indexing Bot",
                    IsEnabled = true
                }), new MessageSinkOptions
                {
                    RecipientAddress = settings.NotifierEmail!,
                    FlushSpan = TimeSpan.FromMinutes(settings.NotifierSpan),
                    MaxTailSize = settings.NotifierLimit,
                    ImmediateFlushThreshold = 1
                });
            _sink.OnFlush += (_) => AnsiConsole.MarkupLine(
                $"[yellow]Sending message #{_sink.MessageCount}[/]");
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

                if (_sink != null)
                {
                    await Notify(new MessageSinkEntry(0,
                        $"Indexing started: last {settings.NotifierLimit} entries " +
                        $"every {settings.NotifierSpan}'"));
                    if (_sink != null && settings.NotifyStart)
                        await _sink.FlushAsync();
                }

                await builder.Build(profile.Id!, settings.Source!,
                    CancellationToken.None,
                    new Progress<ProgressReport>(async report =>
                    {
                        AnsiConsole.MarkupLine(
                            $"[yellow]{report.Count}[/] " +
                            $"[green]{DateTime.Now:HH:mm:ss}[/] " +
                            $"[cyan]{report.Message}[/]");

                        if (_sink != null)
                        {
                            await Notify(new MessageSinkEntry(0,
                                $"{report.Count}: {report.Message}"));
                        }
                    }));

                if (!settings.IsDry)
                {
                    ctx.Status("Finalizing index...");
                    if (_sink != null)
                        await Notify(new MessageSinkEntry(0, "Finalizing index"));
                    repository.FinalizeIndex();
                }
            });

            AnsiConsole.MarkupLine("[green]Completed[/]");
            if (_sink != null)
            {
                await Notify(new MessageSinkEntry(0, "Indexing completed"));
                await _sink.FlushAsync();
            }

            return 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            AnsiConsole.WriteException(ex);
            if (_sink != null)
                await Notify(new MessageSinkEntry(1, $"Indexing error: {ex}"));
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

    [Description("The email address to send notifications to")]
    [CommandOption("--n-email <EMAIL>")]
    public string? NotifierEmail { get; set; }

    [Description("The timespan in minutes to wait between notifications (15')")]
    [CommandOption("--n-span <SPAN>")]
    [DefaultValue(15)]
    public int NotifierSpan { get; set; } = 15;

    [Description("The maximum number of entries to keep in the notifier's tail (100)")]
    [CommandOption("--n-limit <LIMIT>")]
    [DefaultValue(100)]
    public int NotifierLimit { get; set; } = 100;

    [Description("Whether to notify the start of the indexing process")]
    [CommandOption("--n-start")]
    public bool NotifyStart { get; set; }

    public IndexCommandSettings()
    {
        DbName = "pythia";
    }
}
