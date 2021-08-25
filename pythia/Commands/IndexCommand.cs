using Corpus.Core;
using Fusi.Cli;
using Fusi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Core;
using Pythia.Core.Analysis;
using Pythia.Core.Config;
using Pythia.Sql.PgSql;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands
{
    public sealed class IndexCommand : ICommand
    {
        private readonly IndexCommandOptions _options;

        public IndexCommand(IndexCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            command.Description =
                "Index the specified source into the Pythia database " +
                "with the specified name.";
            command.HelpOption("-?|-h|--help");

            CommandArgument profileIdArgument = command.Argument("[profileId]",
                "The ID of the profile to use.");

            CommandArgument sourceArgument = command.Argument("[source]",
                "The documents source.");

            CommandArgument dbNameArgument = command.Argument("[dbName]",
                "The database name.");

            CommandOption contentOption = command.Option("-c|--content",
                "Content to index: include T=token, S=structure.",
                CommandOptionType.SingleValue);

            CommandOption docContentOption = command.Option("-o|--doc-content",
                "True to store the document's content.",
                CommandOptionType.NoValue);

            CommandOption dryOption = command.Option("-d|--dry",
                "Dry run: do not write to database.", CommandOptionType.NoValue);

            CommandOption pluginTagOption = command.Option("-t|--tag",
                "The factory provider plugin tag.", CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                IndexContents contents = IndexContents.None;
                string cnt = contentOption.Value()?.ToUpperInvariant() ?? "TS";
                if (cnt.IndexOf('T') > -1) contents |= IndexContents.Tokens;
                if (cnt.IndexOf('S') > -1) contents |= IndexContents.Structures;

                options.Command = new IndexCommand(
                    new IndexCommandOptions
                    {
                        AppOptions = options,
                        ProfileId = profileIdArgument.Value,
                        Source = sourceArgument.Value,
                        DbName = dbNameArgument.Value,
                        Contents = contents,
                        DocumentHasContent = docContentOption.HasValue(),
                        IsDry = dryOption.HasValue(),
                        PluginTag = pluginTagOption.Value()
                            ?? AppOptions.DEFAULT_PLUGIN_TAG
                    });
                return 0;
            });
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Index");
            Console.WriteLine($"Plugin tag: {_options.PluginTag}\n");

            Console.WriteLine("Indexing " + _options.Source);

            string cs = string.Format(
                _options.AppOptions.Configuration.GetConnectionString("Default"),
                _options.DbName);

            IIndexRepository repository = new PgSqlIndexRepository(cs);
            Profile profile = repository.GetProfile(_options.ProfileId);
            if (profile == null)
            {
                throw new ApplicationException("Profile ID not found: "
                    + _options.ProfileId);
            }

            //PythiaFactory factory = PythiaFactoryProvider.GetFactory(
            //    profile.Id, profile.Content, cs);
            var factoryProvider = PluginPythiaFactoryProvider.GetFromTag
                (_options.PluginTag);
            if (factoryProvider == null)
            {
                throw new FileNotFoundException(
                    $"The requested tag {_options.PluginTag} was not found " +
                    "among plugins in " +
                    PluginPythiaFactoryProvider.GetPluginsDir());
            }
            PythiaFactory factory = factoryProvider.GetFactory(
                profile.Id, profile.Content, cs);

            IndexBuilder builder = new IndexBuilder(factory, repository)
            {
                Contents = _options.Contents,
                IsDryMode = _options.IsDry,
                Logger = _options.AppOptions.Logger
            };

            await builder.Build(profile.Id, _options.Source, CancellationToken.None,
                new Progress<ProgressReport>(report =>
                ColorConsole.WriteEmbeddedColorLine(
                    $"[cyan]{report.Count:00000}[/cyan] {report.Message}")));

            Console.WriteLine("Pruning tokens...");
            repository.PruneTokens();

            ColorConsole.WriteSuccess("Completed");
            return 0;
        }
    }

    public class IndexCommandOptions
    {
        public AppOptions AppOptions { get; set; }
        public string ProfileId { get; set; }
        public string Source { get; set; }
        public string DbName { get; set; }
        public IndexContents Contents { get; set; }
        public bool DocumentHasContent { get; set; }
        public bool IsDry { get; set; }
        public string PluginTag { get; set; }
    }
}
