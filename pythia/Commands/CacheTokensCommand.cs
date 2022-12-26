using Corpus.Sql;
using Fusi.Cli;
using Fusi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Core.Analysis;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Sql;
using Pythia.Sql.PgSql;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

public sealed class CacheTokensCommand : ICommand
{
    private readonly CacheTokensCommandOptions _options;

    public CacheTokensCommand(CacheTokensCommandOptions options)
    {
        _options = options;
    }

    public static void Configure(CommandLineApplication command,
        AppOptions options)
    {
        command.Description = "Cache the tokens got from tokenizing " +
            "the texts from the specified source.";
        command.HelpOption("-?|-h|--help");

        CommandArgument sourceArgument = command.Argument("[source]",
            "The index source.");

        CommandArgument outputDirArgument = command.Argument("[output]",
            "The cache output directory.");

        CommandArgument profilePathArgument = command.Argument("[profilePath]",
            "The file path of the 1st tokenization profile");

        CommandArgument profileIdArgument = command.Argument("[profileId]",
            "The profile ID to be used in the 2nd tokenization. " +
            "This will be set as the profile ID of the documents " +
            "added to the index");

        CommandArgument dbNameArgument = command.Argument("[dbName]",
            "The database name.");

        CommandOption pluginTagOption = command.Option("-t|--tag",
            "The factory provider plugin tag.", CommandOptionType.SingleValue);

        command.OnExecute(() =>
        {
            options.Command = new CacheTokensCommand(
                new CacheTokensCommandOptions
                {
                    AppOptions = options,
                    Source = sourceArgument.Value,
                    OutputDir = outputDirArgument.Value,
                    TargetProfileId = profileIdArgument.Value,
                    ProfilePath = profilePathArgument.Value,
                    DbName = dbNameArgument.Value,
                    PluginTag = pluginTagOption.Value()
                        ?? AppOptions.DEFAULT_PLUGIN_TAG
                });
            return 0;
        });
    }

    private static string LoadTextFromFile(string path)
    {
        using StreamReader reader = new(path, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public async Task<int> Run()
    {
        ColorConsole.WriteWrappedHeader("Cache Tokens for Deferred Tagging");
        Console.WriteLine(
            $"Source: {_options.Source}\n" +
            $"Output: {_options.OutputDir}\n" +
            $"Target profile ID: {_options.TargetProfileId}\n" +
            $"Profile path: {_options.ProfilePath}\n" +
            $"Database name: {_options.DbName}\n" +
            $"Plugin tag: {_options.PluginTag}\n");

        Console.WriteLine("Loading profile...");
        string profile = LoadTextFromFile(_options.ProfilePath!);

        ITokenCache cache = new FsForwardTokenCache();
        cache.AllowedAttributes.Add("s0");
        cache.AllowedAttributes.Add("text");

        string cs = string.Format(
            _options.AppOptions!.Configuration!.GetConnectionString("Default")!,
            _options.DbName);

        SqlIndexRepository repository = new PgSqlIndexRepository();
        repository.Configure(new SqlRepositoryOptions
        {
            ConnectionString = cs
        });

        var factoryProvider = PluginPythiaFactoryProvider.GetFromTag
            (_options.PluginTag!);
        if (factoryProvider == null)
        {
            throw new FileNotFoundException(
                $"The requested tag {_options.PluginTag} was not found " +
                "among plugins in " +
                PluginPythiaFactoryProvider.GetPluginsDir());
        }

        PythiaFactory factory = factoryProvider.GetFactory(
            Path.GetFileNameWithoutExtension(_options.ProfilePath) ?? "",
            LoadTextFromFile(_options.ProfilePath!), cs);

        IndexBuilder builder = new(factory, repository)
        {
            Logger = _options.AppOptions.Logger
        };

        cache.Open(_options.OutputDir!);
        await builder.CacheTokensAsync(_options.TargetProfileId!,
            _options.Source!,
            cache, CancellationToken.None,
            new Progress<ProgressReport>(r => Console.Write(r.Message)));
        cache.Close();

        ColorConsole.WriteSuccess("Completed.");
        return 0;
    }
}

public class CacheTokensCommandOptions
{
    public AppOptions? AppOptions { get; set; }
    public string? Source { get; set; }
    public string? OutputDir { get; set; }
    public string? TargetProfileId { get; set; }
    public string? ProfilePath { get; set; }
    public string? DbName { get; set; }
    public string? PluginTag { get; set; }
}
