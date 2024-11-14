using Corpus.Core;
using Corpus.Core.Reading;
using Corpus.Sql;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Plugin.Standard;
using Pythia.Cli.Services;
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
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class DumpMapCommand : AsyncCommand<DumpMapCommandSettings>
{
    private static string DumpText(string text)
    {
        StringBuilder sb = new(text);
        sb.Replace("\r", "\\r");
        sb.Replace("\n", "\\n");
        sb.Replace("\t", "\\t");
        return sb.ToString();
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        DumpMapCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[underline green]DUMP MAP[/]");
        AnsiConsole.MarkupLine($"Document ID: [cyan]{settings.DocumentId}[/]");
        AnsiConsole.MarkupLine($"Profile ID: [cyan]{settings.ProfileId}[/]");
        AnsiConsole.MarkupLine($"Output path: [cyan]{settings.OutputPath}[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");
        if (settings.PluginTag != null)
            AnsiConsole.MarkupLine($"Plugin tag: [cyan]{settings.PluginTag}[/]");

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
                ?? throw new ArgumentException("Profile ID not found: " +
                    settings.ProfileId);

            var factoryProvider = (string.IsNullOrEmpty(settings.PluginTag)
                ? new StandardCliPythiaFactoryProvider()
                : PluginPythiaFactoryProvider.GetFromTag(settings.PluginTag))
                ?? throw new FileNotFoundException(
                    $"The requested tag {settings.PluginTag} was not found " +
                    "among plugins in " +
                    PluginPythiaFactoryProvider.GetPluginsDir());

            PythiaFactory factory = factoryProvider.GetFactory(
                profile.Id!, profile.Content!, cs);

            int result = 0;
            await AnsiConsole.Status().Start("Dumping...", async ctx =>
            {
                // 1. retrieve text
                ctx.Status("Retrieving text");
                ctx.Spinner(Spinner.Known.Star);

                ITextRetriever retriever = factory.GetTextRetriever()!;
                string? text = await retriever.GetAsync(new Document
                {
                    Id = settings.DocumentId
                });
                if (text == null)
                {
                    result = 1;
                }
                else
                {
                    // 2. map text
                    ctx.Status("Mapping text");
                    ctx.Spinner(Spinner.Known.Star);

                    ITextMapper mapper = factory.GetTextMapper()!;
                    TextMapNode map = mapper.Map(text, null)!;

                    // 3. dump map
                    ctx.Status("Dumping map");
                    ctx.Spinner(Spinner.Known.Star);

                    string outDir = Path.GetDirectoryName(settings.OutputPath) ?? "";
                    if (outDir.Length > 0 && !Directory.Exists(outDir))
                        Directory.CreateDirectory(outDir);

                    await using StreamWriter writer = File.CreateText(
                        settings.OutputPath!);
                    await writer.WriteLineAsync("#Tree");
                    await writer.WriteLineAsync($"Length (chars): {text.Length}");
                    await writer.WriteLineAsync(map.DumpTree());

                    map.Visit(node =>
                    {
                        writer.WriteLine($"#{node.Label}: {node.Location}");
                        writer.WriteLine($"{node.StartIndex}-{node.EndIndex}");

                        string sStart = text.Substring(node.StartIndex,
                            node.StartIndex + 100 > node.EndIndex ?
                            node.EndIndex - node.StartIndex : 100);
                        writer.WriteLine($"From: {DumpText(sStart)} ...");

                        int i = node.EndIndex - 100 < node.StartIndex ?
                            node.StartIndex : node.EndIndex - 100;
                        string end = text[i..node.EndIndex];
                        writer.WriteLine($"To: ... {DumpText(end)}");

                        writer.WriteLine();

                        return true;
                    });

                    await writer.FlushAsync();
                }
            });

            AnsiConsole.MarkupLine("[green]Completed.[/]");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}

internal class DumpMapCommandSettings : CommandSettings
{
    [Description("The document ID")]
    [CommandArgument(0, "<ID>")]
    public int DocumentId { get; set; }

    [Description("The profile ID")]
    [CommandArgument(1, "<PROFILE_ID>")]
    public string? ProfileId { get; set; }

    [Description("The output file path")]
    [CommandArgument(2, "<OUTPUT_PATH>")]
    public string? OutputPath { get; set; }

    [Description("The factory provider plugin tag")]
    [CommandOption("-t|--tag <PLUGIN_TAG>")]
    public string? PluginTag { get; set; }

    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; } = "pythia";
}
