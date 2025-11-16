using Pythia.Sql.PgSql;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using CsvHelper;
using System.Globalization;
using Pythia.Core.Query;
using Corpus.Sql;
using Pythia.Cli.Services;
using Microsoft.Extensions.Configuration;

namespace Pythia.Cli.Commands;

/// <summary>
/// Exports document pairs from the index repository to a CSV file using the
/// specified settings.
/// </summary>
/// <remarks>This command retrieves document pairs from the configured PostgreSQL
/// index repository, applying any bin count filters and excluded document
/// attributes as specified in the settings. The results are written to a CSV
/// file at the output path provided.</remarks>
internal sealed class DumpDocPairsCommand : AsyncCommand<DumpDocPairsSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context,
        DumpDocPairsSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]INDEX WORDS[/]");
        if (settings.BinCounts.Length > 0)
        {
            AnsiConsole.MarkupLine(
                $"Bin counts: {string.Join(",", settings.BinCounts)}");
        }
        if (settings.ExcludedDocAttrs.Length > 0)
        {
            AnsiConsole.MarkupLine(
                $"Excluded doc attrs: {string.Join(",", settings.ExcludedDocAttrs)}");
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

            IList<DocumentPair> pairs = await repository.GetDocumentPairsAsync(
                settings.ParseBinCounts(),
                [.. settings.ExcludedDocAttrs]);

            AnsiConsole.Markup("Writing CSV file...");

            StreamWriter writer = new(Path.Combine(settings.OutputPath),
                false, Encoding.UTF8);
            CsvWriter csv = new(writer, CultureInfo.InvariantCulture);

            // header
            csv.WriteField("privileged");
            csv.WriteField("numeric");
            csv.WriteField("name");
            csv.WriteField("value");
            await csv.NextRecordAsync();

            // data
            foreach (DocumentPair pair in pairs)
            {
                csv.WriteField(pair.IsPrivileged);
                csv.WriteField(pair.IsNumeric);
                csv.WriteField(pair.Name);
                csv.WriteField(pair.Value);
                await csv.NextRecordAsync();
            }

            AnsiConsole.MarkupLine("[green]completed[/]");

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

public class DumpDocPairsSettings : BuildWordIndexCommandSettings
{
    [CommandOption("-o|--output")]
    [Description("The output file path.")]
    public string OutputPath { get; set; } = Environment.GetFolderPath(
        Environment.SpecialFolder.DesktopDirectory) + "/doc-pairs.csv";
}
