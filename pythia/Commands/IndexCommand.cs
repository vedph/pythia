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
using System.IO;
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

    public override async Task<int> ExecuteAsync(CommandContext context,
        IndexCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red]INDEX[/]");
        AnsiConsole.MarkupLine($"Profile ID: {settings.ProfileId}");
        AnsiConsole.MarkupLine($"Source: {settings.Source}");
        AnsiConsole.MarkupLine($"Database: {settings.DbName}");
        AnsiConsole.MarkupLine($"Contents: {settings.Contents ?? "TS"}");
        AnsiConsole.MarkupLine($"Store content: {settings.IsContentStored}");
        AnsiConsole.MarkupLine($"Preflight: {settings.IsDry}");
        AnsiConsole.MarkupLine($"Plugin tag: {settings.PluginTag}");

        string cs = string.Format(
            CliAppContext.Configuration!.GetConnectionString("Default")!,
            settings.DbName);

        SqlIndexRepository repository = new PgSqlIndexRepository();
        repository.Configure(new SqlRepositoryOptions
        {
            ConnectionString = cs
        });

        IProfile? profile = repository.GetProfile(settings.ProfileId!);
        if (profile == null)
        {
            throw new ArgumentException("Profile ID not found: "
                + settings.ProfileId);
        }

        ICliPythiaFactoryProvider? factoryProvider =
            string.IsNullOrEmpty(settings.PluginTag)
            ? new StandardCliPythiaFactoryProvider()
            : PluginPythiaFactoryProvider.GetFromTag(settings.PluginTag);
        if (factoryProvider == null)
        {
            throw new FileNotFoundException(
                $"The requested tag {settings.PluginTag} was not found " +
                "among plugins in " +
                PluginPythiaFactoryProvider.GetPluginsDir());
        }
        PythiaFactory factory = factoryProvider.GetFactory(
            profile.Id!, profile.Content!, cs);

        IndexBuilder builder = new(factory, repository)
        {
            Contents = ParseIndexContents(settings.Contents),
            IsDryMode = settings.IsDry,
            IsContentStored = settings.IsContentStored,
            Logger = CliAppContext.Logger
        };

        await AnsiConsole.Status().Start("Indexing...", async ctx =>
        {
            ctx.Status("Building index");
            ctx.Spinner(Spinner.Known.Star);

            await builder.Build(profile.Id!, settings.Source!,
                CancellationToken.None,
                new Progress<ProgressReport>(report =>
                {
                    ctx.Status($"[cyan]{report.Count}[/] {report.Message}");
                }));

            ctx.Status("Pruning tokens...");
            repository.PruneTokens();

            ctx.Status("Finalizing index...");
            repository.FinalizeIndex();
        });

        AnsiConsole.MarkupLine("[green]Completed[/]");
        return 0;
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

    public IndexCommandSettings()
    {
        DbName = "pythia";
    }
}
