using Corpus.Sql;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Core;
using Pythia.Sql.PgSql;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

/// <summary>
/// Exports text span data from the database to a CSV file or the console,
/// using the specified filter and output settings.
/// </summary>
/// <remarks>Use this command to extract spans from the database, optionally
/// filtering by type, position range, document IDs, and attributes.
/// The output can be directed to a file or printed to the console in CSV format.
/// </remarks>
internal sealed class DumpSpanCommand : AsyncCommand<DumpSpanSettings>
{
    private static void WriteSpanFields(TextSpan span, CsvWriter csv)
    {
        csv.WriteField(span.Id);
        csv.WriteField(span.DocumentId);
        csv.WriteField(span.Type);
        csv.WriteField(span.P1);
        csv.WriteField(span.P2);
        csv.WriteField(span.Index);
        csv.WriteField(span.Length);
        csv.WriteField(span.Language);
        csv.WriteField(span.Pos);
        csv.WriteField(span.Lemma);
        csv.WriteField(span.Value);
        csv.WriteField(span.Text);
    }

    public override Task<int> ExecuteAsync(CommandContext context,
        DumpSpanSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]DUMP SPANS[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");
        if (!string.IsNullOrEmpty(settings.Type))
            AnsiConsole.MarkupLine($"Type: [cyan]{settings.Type}[/]");
        if (settings.PositionMin > 0)
            AnsiConsole.MarkupLine($"Position min: [cyan]{settings.PositionMin}[/]");
        if (settings.PositionMax > 0)
            AnsiConsole.MarkupLine($"Position max: [cyan]{settings.PositionMax}[/]");
        if (settings.DocumentIds.Length > 0)
        {
            AnsiConsole.MarkupLine(
                $"Document IDs: [cyan]{string.Join(",", settings.DocumentIds)}[/]");
        }
        if (settings.Attributes.Length > 0)
        {
            AnsiConsole.MarkupLine(
                $"Attributes: [cyan]{string.Join(",", settings.Attributes)}[/]");
        }

        try
        {
            // create the repository
            string cs = string.Format(
                CliAppContext.Configuration!.GetConnectionString("Default")!,
                settings.DbName);
            PgSqlIndexRepository repository = new();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });

            // open CSV writer using either console or OutputPath
            bool isConsole = string.IsNullOrEmpty(settings.OutputPath);
            CsvWriter csv;
            if (isConsole)
            {
                csv = new CsvWriter(Console.Out, CultureInfo.InvariantCulture);
            }
            else
            {
                StreamWriter writer = new(settings.OutputPath!, false, Encoding.UTF8);
                csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                // write CSV header for TextSpan
                csv.WriteField("id");
                csv.WriteField("doc_id");
                csv.WriteField("type");
                csv.WriteField("P1");
                csv.WriteField("P2");
                csv.WriteField("index");
                csv.WriteField("len");
                csv.WriteField("lang");
                csv.WriteField("pos");
                csv.WriteField("lemma");
                csv.WriteField("value");
                csv.WriteField("text");
                csv.WriteField("at_n");
                csv.WriteField("at_v");
                csv.NextRecord();
            }

            // build filter from settings
            TextSpanFilter filter = new()
            {
                Type = settings.Type,
                PositionMin = settings.PositionMin,
                PositionMax = settings.PositionMax,
                DocumentIds = settings.DocumentIds.Length == 0
                    ? null
                    : new HashSet<int>(settings.DocumentIds.Select(int.Parse)),
                Attributes = settings.Attributes.Length == 0
                    ? null
                    : settings.Attributes.Select(attr =>
                    {
                        string[] parts = attr.Split('=', 2);
                        return new KeyValuePair<string, string>(parts[0], parts[1]);
                    }).ToDictionary(pair => pair.Key, pair => pair.Value)
            };

            // enumerate spans
            foreach (TextSpan span in repository.EnumerateSpans(filter))
            {
                if (span.Attributes?.Count > 0)
                {
                    foreach (var attr in span.Attributes)
                    {
                        WriteSpanFields(span, csv);
                        csv.WriteField(attr.Name);
                        csv.WriteField(attr.Value);
                        csv.NextRecord();
                    }
                }
                else
                {
                    WriteSpanFields(span, csv);
                    csv.NextRecord();
                }
            }

            if (!isConsole)
            {
                csv.Flush();
                AnsiConsole.MarkupLine("[green]completed[/]");
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            AnsiConsole.WriteException(ex);
            return Task.FromResult(1);
        }
    }
}

public class DumpSpanSettings : CommandSettings
{
    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; } = "pythia";

    [Description("The CSV output path")]
    [CommandOption("-o|--output <PATH>")]
    public string? OutputPath { get; set; }

    [Description("The span type filter (e.g. tok)")]
    [CommandOption("-t|--type <TYPE>")]
    public string? Type { get; set; }

    [Description("The minimum span position filter")]
    [CommandOption("-n|--pos-min <POS>")]
    public int PositionMin { get; set; }

    [Description("The maximum span position filter")]
    [CommandOption("-m|--pos-max <POS>")]
    public int PositionMax { get; set; }

    [Description("The document IDs filter (multiple)")]
    [CommandOption("-i|--doc-id <ID>")]
    public string[] DocumentIds { get; set; } = [];

    [Description("The span attributes filter (multiple)")]
    [CommandOption("--attr <NAME=VALUE>")]
    public string[] Attributes { get; set; } = [];
}
