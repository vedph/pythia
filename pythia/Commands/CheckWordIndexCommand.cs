using Corpus.Sql;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Core;
using Pythia.Sql.PgSql;
using Pythia.Tagger.Ita.Plugin;
using Pythia.Tagger.LiteDB;
using Pythia.Tools;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class CheckWordIndexCommand : AsyncCommand<CheckWordIndexCommandSettings>
{
    private readonly HashSet<string> _excludedPos =
    [
        "NUM", "PROPN", "ABBR", "EMAIL", "DATE"
    ];

    private static void ShowSettings(CheckWordIndexCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green]CHECK WORDS[/]");
        AnsiConsole.MarkupLine(
            $"Lookup index path: [cyan]{settings.LookupIndexPath}[/]");
        AnsiConsole.MarkupLine(
            $"Output path: [cyan]{settings.OutputPath}[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");
        AnsiConsole.WriteLine();
    }

    public override Task<int> ExecuteAsync(CommandContext context,
        CheckWordIndexCommandSettings settings)
    {
        try
        {
            ShowSettings(settings);

            // create the repository
            string cs = string.Format(
                CliAppContext.Configuration!.GetConnectionString("Default")!,
                settings.DbName);
            PgSqlIndexRepository repository = new();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });

            // create lookup index and variant builder
            using LiteDBLookupIndex index = new(settings.LookupIndexPath, true);
            ItalianVariantBuilder builder = new();

            // create the word checker
            WordChecker checker = new(index, builder, new ItalianPosTagBuilder());

            // open the output file
            using CsvWordReportWriter writer = new();
            writer.Open(settings.OutputPath);

            // check each token span in the repository
            int spanCount = 0, resultCount = 0;
            TextSpanFilter filter = new()
            {
                Type = "tok"
            };
            foreach (TextSpan span in repository.EnumerateSpans(filter)
                .Where(s => string.IsNullOrEmpty(s.Language) &&
                            !_excludedPos.Contains(s.Pos ?? "")))
            {
                spanCount++;
                AnsiConsole.WriteLine(span.ToString());

                IList<WordCheckResult> results = checker.Check(new WordToCheck
                {
                    Id = span.Id,
                    Language = span.Language,
                    Pos = span.Pos,
                    Value = span.Value,
                    Lemma = span.Lemma
                });
                foreach (WordCheckResult result in results
                    .Where(r => r.Type != WordCheckResultType.Info))
                {
                    writer.Write(result);
                }
                resultCount += results.Count;
            }

            writer.Close();

            AnsiConsole.MarkupLine($"Completed: [yellow]{spanCount}[/] spans, " +
                $"[yellow]{resultCount}[/] results.");

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

public class CheckWordIndexCommandSettings : CommandSettings
{
    /// <summary>
    /// The path to the lookup index file.
    /// </summary>
    [CommandArgument(0, "[LookupIndexPath]")]
    public required string LookupIndexPath { get; set; }

    [CommandOption("-o|--output")]
    [Description("The output path.")]
    public string OutputPath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "word-check.csv");

    [CommandOption("-d|--db-name")]
    [Description("The name of the database to use.")]
    public string DbName { get; set; } = "pythia";
}
