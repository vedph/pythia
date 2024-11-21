using Corpus.Core;
using Corpus.Core.Reading;
using Corpus.Sql;
using CsvHelper;
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
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class DumpMapCommand : AsyncCommand<DumpMapCommandSettings>
{
    private static void WriteNodeToText(int n, TextMapNode node, string text,
        TextWriter writer)
    {
        writer.WriteLine($"## {n}. {node.Label} " +
            $"[{node.StartIndex}-{node.EndIndex}]");
        writer.WriteLine();
        writer.WriteLine($"- {node.Location}");
        writer.WriteLine($"- len: {node.EndIndex - node.StartIndex}");
        writer.WriteLine();
        writer.WriteLine("```txt");
        writer.WriteLine(text[node.StartIndex..node.EndIndex]);
        writer.WriteLine("```");

        writer.WriteLine();
    }

    private static void WriteNodeToCsv(TextMapNode node, CsvWriter csv)
    {
        csv.WriteField(node.StartIndex);
        csv.WriteField(node.EndIndex);
        csv.WriteField(node.EndIndex - node.StartIndex);
        csv.WriteField(node.Label);
        csv.WriteField(node.GetPath());
        csv.WriteField(node.Location);
        csv.NextRecord();
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        DumpMapCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[underline green]DUMP MAP[/]");
        AnsiConsole.MarkupLine($"Document ID: [cyan]{settings.DocumentId}[/]");
        AnsiConsole.MarkupLine($"Profile ID: [cyan]{settings.ProfileId}[/]");
        AnsiConsole.MarkupLine($"Output directory: [cyan]{settings.OutputDir}[/]");
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

                    if (!Directory.Exists(settings.OutputDir))
                        Directory.CreateDirectory(settings.OutputDir);

                    // CSV output
                    CsvWriter csv = new(new StreamWriter(Path.Combine(
                        settings.OutputDir, $"{settings.DocumentId}.csv"),
                        false, Encoding.UTF8),
                        CultureInfo.InvariantCulture);
                    csv.WriteField("start");
                    csv.WriteField("end");
                    csv.WriteField("length");
                    csv.WriteField("label");
                    csv.WriteField("path");
                    csv.WriteField("location");
                    await csv.NextRecordAsync();

                    // text output
                    await using StreamWriter writer = new(Path.Combine(
                        settings.OutputDir, $"{settings.DocumentId}.txt"),
                        false, Encoding.UTF8);
                    await writer.WriteLineAsync($"# Map of {settings.DocumentId}");
                    await writer.WriteLineAsync();
                    await writer.WriteLineAsync($"Length: {text.Length}.");
                    await writer.WriteLineAsync();

                    int n = 0;
                    map.Visit(node =>
                    {
                        WriteNodeToText(++n, node, text, writer);
                        WriteNodeToCsv(node, csv);

                        return true;
                    });

                    await writer.FlushAsync();
                    await csv.FlushAsync();
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

    [Description("The output directory")]
    [CommandArgument(2, "<OUTPUT_DIR>")]
    public string OutputDir { get; set; } = "";

    [Description("The factory provider plugin tag")]
    [CommandOption("-t|--tag <PLUGIN_TAG>")]
    public string? PluginTag { get; set; }

    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; } = "pythia";
}
