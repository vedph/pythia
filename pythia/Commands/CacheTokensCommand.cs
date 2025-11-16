using Corpus.Sql;
using Fusi.Tools;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Core.Analysis;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
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
/// Caches tokens for deferred tagging operations using the specified settings.
/// </summary>
/// <remarks>This command is intended for use within a command-line application
/// to facilitate the caching of tokens based on a source, output directory,
/// profile, and database configuration. It interacts with plugin-based
/// token factories and a SQL index repository to perform the caching process.
/// </remarks>
internal sealed class CacheTokensCommand : AsyncCommand<CacheTokensCommandSettings>
{
    private static string LoadTextFromFile(string path)
    {
        using StreamReader reader = new(path, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        CacheTokensCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]CACHE TOKENS FOR DEFERRED TAGGING[/]");
        AnsiConsole.MarkupLine($"Source: [cyan]{settings.Source}[/]");
        AnsiConsole.MarkupLine($"Output dir: [cyan]{settings.OutputDir}[/]");
        AnsiConsole.MarkupLine($"Profile: [cyan]{settings.ProfilePath}[/]");
        AnsiConsole.MarkupLine($"Target profile ID: [cyan]{settings.TargetProfileId}[/]");
        AnsiConsole.MarkupLine($"Database name: [cyan]{settings.DbName}[/]");
        AnsiConsole.MarkupLine($"Plugin tag: [cyan]{settings.PluginTag}[/]");

        try
        {
            FsForwardTokenCache cache = new();
            cache.AllowedAttributes.Add("s0");
            cache.AllowedAttributes.Add("text");

            string cs = string.Format(
                CliAppContext.Configuration!.GetConnectionString("Default")!,
                settings.DbName);

            SqlIndexRepository repository = new PgSqlIndexRepository();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });

            var factoryProvider = PluginPythiaFactoryProvider.GetFromTag
                (settings.PluginTag!) ?? throw new FileNotFoundException(
                    $"The requested tag {settings.PluginTag} was not found " +
                    "among plugins in " +
                    PluginPythiaFactoryProvider.GetPluginsDir());
            PythiaFactory factory = factoryProvider.GetFactory(
                Path.GetFileNameWithoutExtension(settings.ProfilePath) ?? "",
                LoadTextFromFile(settings.ProfilePath!), cs);

            IndexBuilder builder = new(factory, repository)
            {
                Logger = CliAppContext.Logger
            };

            cache.Open(settings.OutputDir!);
            await builder.CacheTokensAsync(settings.TargetProfileId!,
                settings.Source!,
                cache, CancellationToken.None,
                new Progress<ProgressReport>(r => Console.Write(r.Message)));
            cache.Close();

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

public class CacheTokensCommandSettings : CommandSettings
{
    [Description("The documents source")]
    [CommandArgument(0, "<SOURCE>")]
    public string? Source { get; set; }

    [Description("The output directory")]
    [CommandArgument(1, "<OUTPUT_DIR>")]
    public string? OutputDir { get; set; }

    [Description("The profile file path")]
    [CommandArgument(2, "<PROFILE_PATH>")]
    public string? ProfilePath { get; set; }

    [Description("The profile ID")]
    [CommandArgument(3, "<PROFILE_ID>")]
    public string? TargetProfileId { get; set; }

    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; }

    [Description("The factory provider plugin tag")]
    [CommandOption("-t|--tag <PLUGIN_TAG>")]
    public string? PluginTag { get; set; }

    public CacheTokensCommandSettings()
    {
        DbName = "pythia";
    }
}
