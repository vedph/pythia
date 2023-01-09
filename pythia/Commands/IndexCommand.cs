using Corpus.Core;
using Corpus.Sql;
using Fusi.Cli;
using Fusi.Cli.Commands;
using Fusi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Core;
using Pythia.Cli.Plugin.Standard;
using Pythia.Cli.Services;
using Pythia.Core.Analysis;
using Pythia.Core.Config;
using Pythia.Sql;
using Pythia.Sql.PgSql;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class IndexCommand : ICommand
{
    private readonly IndexCommandOptions _options;

    private IndexCommand(IndexCommandOptions options)
    {
        _options = options;
    }

    public static void Configure(CommandLineApplication app,
        ICliAppContext context)
    {
        app.Description =
            "Index the specified source into the Pythia database " +
            "with the specified name.";
        app.HelpOption("-?|-h|--help");

        CommandArgument profileIdArgument = app.Argument("[profileId]",
            "The ID of the profile to use.");

        CommandArgument sourceArgument = app.Argument("[source]",
            "The documents source.");

        CommandArgument dbNameArgument = app.Argument("[dbName]",
            "The database name.");

        CommandOption contentOption = app.Option("-c|--content",
            "Content to index: include T=token, S=structure.",
            CommandOptionType.SingleValue);

        CommandOption contentStoredOption = app.Option("-o|--doc-content",
            "True to store the document's content.",
            CommandOptionType.NoValue);

        CommandOption dryOption = app.Option("-d|--dry",
            "Dry run: do not write to database.", CommandOptionType.NoValue);

        CommandOption pluginTagOption = app.Option("-t|--tag",
            "The factory provider plugin tag.", CommandOptionType.SingleValue);

        app.OnExecute(() =>
        {
            IndexContents contents = IndexContents.None;
            string cnt = contentOption.Value()?.ToUpperInvariant() ?? "TS";
            if (cnt.IndexOf('T') > -1) contents |= IndexContents.Tokens;
            if (cnt.IndexOf('S') > -1) contents |= IndexContents.Structures;

            context.Command = new IndexCommand(
                new IndexCommandOptions(context)
                {
                    ProfileId = profileIdArgument.Value,
                    Source = sourceArgument.Value,
                    DbName = dbNameArgument.Value,
                    Contents = contents,
                    IsContentStored = contentStoredOption.HasValue(),
                    IsDry = dryOption.HasValue(),
                    PluginTag = pluginTagOption.Value()
                });
            return 0;
        });
    }

    public async Task Run()
    {
        ColorConsole.WriteWrappedHeader("Index");
        Console.WriteLine($"Plugin tag: {_options.PluginTag}\n");

        Console.WriteLine("Indexing " + _options.Source);

        string cs = string.Format(
            _options.Context!.Configuration!.GetConnectionString("Default")!,
            _options.DbName);

        SqlIndexRepository repository = new PgSqlIndexRepository();
        repository.Configure(new SqlRepositoryOptions
        {
            ConnectionString = cs
        });

        IProfile? profile = repository.GetProfile(_options.ProfileId!);
        if (profile == null)
        {
            throw new ArgumentException("Profile ID not found: "
                + _options.ProfileId);
        }

        ICliPythiaFactoryProvider? factoryProvider =
            string.IsNullOrEmpty(_options.PluginTag)
            ? new StandardCliPythiaFactoryProvider()
            : PluginPythiaFactoryProvider.GetFromTag(_options.PluginTag);
        if (factoryProvider == null)
        {
            throw new FileNotFoundException(
                $"The requested tag {_options.PluginTag} was not found " +
                "among plugins in " +
                PluginPythiaFactoryProvider.GetPluginsDir());
        }
        PythiaFactory factory = factoryProvider.GetFactory(
            profile.Id!, profile.Content!, cs);

        IndexBuilder builder = new(factory, repository)
        {
            Contents = _options.Contents,
            IsDryMode = _options.IsDry,
            IsContentStored = _options.IsContentStored,
            Logger = _options.Context.Logger
        };

        await builder.Build(profile.Id!, _options.Source!,
            CancellationToken.None,
            new Progress<ProgressReport>(report =>
            ColorConsole.WriteEmbeddedColorLine(
                $"[cyan]{report.Count:00000}[/cyan] {report.Message}")));

        Console.WriteLine("Pruning tokens...");
        repository.PruneTokens();

        Console.WriteLine("Finalizing index...");
        repository.FinalizeIndex();

        ColorConsole.WriteSuccess("Completed");
    }
}

public class IndexCommandOptions : CommandOptions<PythiaCliAppContext>
{
    public IndexCommandOptions(ICliAppContext options)
    : base((PythiaCliAppContext)options)
    {
    }

    public string? ProfileId { get; set; }
    public string? Source { get; set; }
    public string? DbName { get; set; }
    public IndexContents Contents { get; set; }
    public bool IsContentStored { get; set; }
    public bool IsDry { get; set; }
    public string? PluginTag { get; set; }
}
