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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

/// <summary>
/// Checks the Pythia word index from a database.
/// </summary>
internal sealed class CheckWordIndexCommand : AsyncCommand<CheckWordIndexCommandSettings>
{
    private readonly HashSet<string> _excludedPos =
    [
        "NUM", "PROPN", "ABBR", "EMAIL", "DATE", "SYM",
        "DET", // DET is confused with PRON
        "SCONJ", "CCONJ", // SCONJ and CCONJ are often confused
        "AUX", // AUX is often confused with VERB (e.g. "dovere"),
        "ADP" // ADP like "come"
    ];

    /// <summary>
    /// Determines whether the specified span should be excluded based on
    /// its attributes and the excluded attributes settings.
    /// </summary>
    /// <param name="span">The span to check.</param>
    /// <param name="excludedAttributes">The excluded attributes patterns.</param>
    /// <returns>True if the span should be excluded; otherwise, false.</returns>
    private static bool ShouldExcludeByAttributes(TextSpan span,
        string[]? excludedAttributes)
    {
        if (excludedAttributes == null || excludedAttributes.Length == 0)
            return false;

        if (span.Attributes == null || span.Attributes.Count == 0)
            return false;

        foreach (string pattern in excludedAttributes)
        {
            int equalsIndex = pattern.IndexOf('=');
            if (equalsIndex == -1)
            {
                // only name specified - exclude if span has any attribute
                // with this name
                if (span.HasAttribute(pattern))
                    return true;
            }
            else
            {
                // name and value specified - exclude only if both match
                string name = pattern[..equalsIndex];
                string value = pattern[(equalsIndex + 1)..];
                if (span.HasAttribute(name, value))
                    return true;
            }
        }

        return false;
    }

    private static void ShowSettings(CheckWordIndexCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green]CHECK WORDS[/]");
        AnsiConsole.MarkupLine(
            $"Lookup index path: [cyan]{settings.LookupIndexPath}[/]");
        AnsiConsole.MarkupLine(
            $"Output path: [cyan]{settings.OutputPath}[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");
        AnsiConsole.MarkupLine($"Context: [cyan]{settings.ContextSize}[/]");
        AnsiConsole.MarkupLine(
            $"Ignore POS mismatches: [cyan]{settings.IgnorePosMismatches}[/]");
        if (!string.IsNullOrEmpty(settings.WhitelistPath))
        {
            AnsiConsole.MarkupLine(
                $"Whitelist: [cyan]{settings.WhitelistPath}[/]");
        }
        if (settings.ExcludedAttributes != null && settings.ExcludedAttributes.Length > 0)
        {
            AnsiConsole.MarkupLine("Excluded attributes: " +
                $"[cyan]{string.Join(", ", settings.ExcludedAttributes)}[/]");
        }
        AnsiConsole.WriteLine();
    }

    private static HashSet<string> LoadWhitelist(string path)
    {
        HashSet<string> whitelist = [];
        foreach (string line in File.ReadAllLines(path))
        {
            string word = line.Trim();
            if (!string.IsNullOrEmpty(word))
            {
                whitelist.Add(word);
            }
        }
        return whitelist;
    }

    public override Task<int> ExecuteAsync(CommandContext context,
        CheckWordIndexCommandSettings settings, CancellationToken cancel)
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
            WordChecker checker = new(index, builder, new ItalianPosTagBuilder())
            {
                IgnorePosMismatches = settings.IgnorePosMismatches
            };

            // load whitelist if specified
            if (!string.IsNullOrEmpty(settings.WhitelistPath))
            {
                if (!File.Exists(settings.WhitelistPath))
                {
                    AnsiConsole.MarkupLine(
                        $"[red]Whitelist file not found: {settings.WhitelistPath}[/]");
                    return Task.FromResult(1);
                }
                checker.Whitelist = LoadWhitelist(settings.WhitelistPath);
                AnsiConsole.MarkupLine(
                    $"[green]Loaded {checker.Whitelist.Count} words from whitelist[/]");
            }

            // open the output file
            using CsvWordReportWriter writer = new();
            writer.Open(settings.OutputPath);

            // check each token span in the repository
            int spanCount = 0, resultCount = 0;
            TextSpanFilter filter = new()
            {
                Type = "tok"
            };
            // load attributes if we need to filter by them
            bool loadAttributes = settings.ExcludedAttributes != null &&
                                  settings.ExcludedAttributes.Length > 0;

            foreach (TextSpan span in repository.EnumerateSpans(filter, loadAttributes)
                .Where(s => string.IsNullOrEmpty(s.Language) &&
                            !_excludedPos.Contains(s.Pos ?? "") &&
                            !ShouldExcludeByAttributes(s, settings.ExcludedAttributes)))
            {
                spanCount++;
                AnsiConsole.WriteLine(span.ToString());

                IList<WordCheckResult> results = checker.Check(new WordToCheck
                {
                    Id = span.Id,
                    Language = span.Language,
                    Pos = span.Pos,
                    Value = span.Value,
                    Lemma = span.Lemma,
                });
                SearchResult sr = new();

                foreach (WordCheckResult result in results
                    .Where(r => r.Type != WordCheckResultType.Info))
                {
                    result.Data ??= [];

                    result.Data["doc_id"] = span.DocumentId
                        .ToString(CultureInfo.InvariantCulture);

                    if (settings.ContextSize > 0)
                    {
                        sr.P1 = span.P1;
                        sr.P2 = span.P2;
                        sr.DocumentId = span.DocumentId;
                        sr.Index = span.Index;
                        sr.Length = span.Length;
                        KwicSearchResult cr = repository.GetResultContext(
                            [sr], settings.ContextSize)[0];
                        result.Data["context"] =
                            ($"{string.Join(" ", cr.LeftContext)}" +
                            $" [{cr.Text}] " +
                            $"{string.Join(" ", cr.RightContext)}").Trim();
                    }
                    else result.Data["context"] = span.Text;

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

    [CommandOption("-c|--context")]
    [Description("The size of the context to retrieve for each result (0=none).")]
    [DefaultValue(5)]
    public int ContextSize { get; set; } = 5;

    [CommandOption("-p|--no-pos-mismatch")]
    [Description("If set, ignores POS mismatches when checking words.")]
    [DefaultValue(true)]
    public bool IgnorePosMismatches { get; set; } = true;

    [CommandOption("-w|--whitelist")]
    [Description("The path to a whitelist file containing word forms to ignore " +
        "during checking (one per line).")]
    public string? WhitelistPath { get; set; }

    [CommandOption("-x|--exclude-attr")]
    [Description("Attributes to exclude. Can be specified multiple times. " +
        "Format: 'name' to exclude any attribute with that name, or 'name=value' " +
        "to exclude only when both name and value match.")]
    public string[]? ExcludedAttributes { get; set; }
}
