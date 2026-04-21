using Corpus.Sql;
using CsvHelper;
using Fusi.Tools.Data;
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
using System.Threading;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

/// <summary>
/// Exports the results of the specified search into one or more CSV files.
/// </summary>
internal sealed class ExportSearchCommand : AsyncCommand<ExportSearchCommandSettings>
{
    private static List<string> LoadQueriesFromFile(string filePath)
    {
        List<string> queries = [];
        List<string> currentQuery = [];

        foreach (string line in File.ReadAllLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (currentQuery.Count > 0)
                {
                    queries.Add(string.Join("\n", currentQuery));
                    currentQuery.Clear();
                }
            }
            else
            {
                currentQuery.Add(line);
            }
        }

        if (currentQuery.Count > 0)
        {
            queries.Add(string.Join("\n", currentQuery));
        }

        return queries;
    }

    private static string BuildFileName(DateTime now, int n)
    {
        StringBuilder sb = new("py");
        sb.Append(now.Year);
        sb.Append(now.Month.ToString("00"));
        sb.Append(now.Day.ToString("00"));
        sb.Append('_');
        sb.Append(now.Hour.ToString("00"));
        sb.Append(now.Minute.ToString("00"));
        sb.Append(now.Second.ToString("00"));
        if (n > 0) sb.Append('_').Append(n.ToString("000"));
        sb.Append(".csv");
        return sb.ToString();
    }

    private static void WriteCsvHeader(int contextSize, CsvWriter csv)
    {
        csv.WriteField("id");
        csv.WriteField("doc_id");
        csv.WriteField("p1");
        csv.WriteField("p2");
        csv.WriteField("index");
        csv.WriteField("length");
        csv.WriteField("type");
        for (int i = 0; i < contextSize; i++)
            csv.WriteField($"c{i - contextSize}");
        csv.WriteField("value");
        for (int i = 0; i < contextSize; i++)
            csv.WriteField($"c{i + 1}");
        csv.WriteField("author");
        csv.WriteField("title");
        csv.WriteField("sort");
        csv.NextRecord();
    }

    private static void WriteCsvResult(KwicSearchResult result, CsvWriter csv)
    {
        csv.WriteField(result.Id);
        csv.WriteField(result.DocumentId);
        csv.WriteField(result.P1);
        csv.WriteField(result.P2);
        csv.WriteField(result.Index);
        csv.WriteField(result.Length);
        csv.WriteField(result.Type);
        foreach (string s in result.LeftContext)
            csv.WriteField(s);
        csv.WriteField(result.Value);
        foreach (string s in result.RightContext)
            csv.WriteField(s);
        csv.WriteField(result.Author);
        csv.WriteField(result.Title);
        csv.WriteField(result.SortKey);
        csv.NextRecord();
    }

    private static void ProcessQuery(string query, PgSqlIndexRepository repository,
        ExportSearchCommandSettings settings, string outputFileName)
    {
        SearchRequest request = new()
        {
            Query = query,
            PageNumber = settings.FirstPage,
            PageSize = settings.PageSize
        };

        AnsiConsole.Progress().Start(ctx =>
        {
            ProgressTask task = ctx.AddTask("[green]Exporting...[/]");

            DataPage<SearchResult> page = repository.Search(request);
            if (page.PageCount == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No results found[/]");
                return;
            }

            int lastPage = settings.LastPage == 0
                ? page.PageCount : settings.LastPage;

            StreamWriter writer = new(
                Path.Combine(settings.OutputDirectory, outputFileName),
                false, Encoding.UTF8);
            CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
            WriteCsvHeader(settings.ContextSize, csv);

            while (request.PageNumber <= lastPage)
            {
                task.Value = (double)request.PageNumber * 100 / page.PageCount;

                const int contextBatchSize = 20;
                for (int i = 0; i < page.Items.Count; i += contextBatchSize)
                {
                    int batchCount = Math.Min(contextBatchSize,
                        page.Items.Count - i);
                    List<SearchResult> batch = [.. page.Items
                        .Skip(i)
                        .Take(batchCount)];

                    IList<KwicSearchResult> results =
                        repository.GetResultContext(batch, settings.ContextSize);

                    foreach (KwicSearchResult result in results)
                    {
                        WriteCsvResult(result, csv);
                    }
                }

                request.PageNumber++;
                page = repository.Search(request);
            }

            csv.Flush();
        });
    }

    private static void ProcessQueryWithMultipleFiles(string query,
        PgSqlIndexRepository repository, ExportSearchCommandSettings settings,
        DateTime now)
    {
        SearchRequest request = new()
        {
            Query = query,
            PageNumber = settings.FirstPage,
            PageSize = settings.PageSize
        };

        AnsiConsole.Progress().Start(ctx =>
        {
            ProgressTask task = ctx.AddTask("[green]Exporting...[/]");

            DataPage<SearchResult> page = repository.Search(request);
            if (page.PageCount == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No results found[/]");
                return;
            }

            int lastPage = settings.LastPage == 0
                ? page.PageCount : settings.LastPage;

            string fileName = BuildFileName(now, 1);
            int rowCount = 0, fileNr = 1;

            StreamWriter writer = new(
                Path.Combine(settings.OutputDirectory, fileName),
                false, Encoding.UTF8);
            CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
            WriteCsvHeader(settings.ContextSize, csv);

            while (request.PageNumber <= lastPage)
            {
                task.Value = (double)request.PageNumber * 100 / page.PageCount;

                const int contextBatchSize = 20;
                for (int i = 0; i < page.Items.Count; i += contextBatchSize)
                {
                    int batchCount = Math.Min(contextBatchSize,
                        page.Items.Count - i);
                    List<SearchResult> batch = [.. page.Items
                        .Skip(i)
                        .Take(batchCount)];

                    IList<KwicSearchResult> results =
                        repository.GetResultContext(batch, settings.ContextSize);

                    foreach (KwicSearchResult result in results)
                    {
                        WriteCsvResult(result, csv);

                        if (++rowCount >= settings.MaxRowPerFile
                            && settings.MaxRowPerFile > 0)
                        {
                            csv.Flush();
                            rowCount = 0;
                            fileName = BuildFileName(now, ++fileNr);

                            writer = new StreamWriter(
                                Path.Combine(settings.OutputDirectory, fileName),
                                false, Encoding.UTF8);
                            csv = new CsvWriter(writer,
                                CultureInfo.InvariantCulture);
                            WriteCsvHeader(settings.ContextSize, csv);
                        }
                    }
                }

                request.PageNumber++;
                page = repository.Search(request);
            }

            csv.Flush();
        });
    }

    protected override Task<int> ExecuteAsync(CommandContext context,
        ExportSearchCommandSettings settings, CancellationToken cancel)
    {
        AnsiConsole.MarkupLine("[green underline]EXPORT SEARCH[/]");

        if (!Directory.Exists(settings.OutputDirectory))
            Directory.CreateDirectory(settings.OutputDirectory);

        try
        {
            string cs = string.Format(
                CliAppContext.Configuration!.GetConnectionString("Default")!,
                settings.DbName);
            PgSqlIndexRepository repository = new();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });

            if (!string.IsNullOrWhiteSpace(settings.QueryFilePath))
            {
                AnsiConsole.MarkupLine($"Query file: [cyan]{settings.QueryFilePath}[/]");
                AnsiConsole.MarkupLine(
                    $"Output directory: [cyan]{settings.OutputDirectory}[/]");
                AnsiConsole.MarkupLine($"Page size: [cyan]{settings.PageSize}[/]");
                if (settings.FirstPage != 1)
                    AnsiConsole.MarkupLine($"First page: [cyan]{settings.FirstPage}[/]");
                if (settings.LastPage != 0)
                    AnsiConsole.MarkupLine($"Last page: [cyan]{settings.LastPage}[/]");
                AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");

                List<string> queries = LoadQueriesFromFile(settings.QueryFilePath);
                AnsiConsole.MarkupLine($"Loaded [cyan]{queries.Count}[/] queries");

                for (int i = 0; i < queries.Count; i++)
                {
                    string query = queries[i];

                    if (!query.StartsWith('['))
                    {
                        query = $"[value=\"{query}\"]";
                    }

                    AnsiConsole.MarkupLine(
                        $"\n[yellow]Processing query #{i + 1}/{queries.Count}:[/]");
                    AnsiConsole.WriteLine(query);

                    string outputFileName = $"q{(i + 1):D5}.csv";
                    ProcessQuery(query, repository, settings, outputFileName);
                }
            }
            else
            {
                if (settings.Query.Length > 0)
                {
                    AnsiConsole.MarkupLineInterpolated(
                        $"Query: [cyan]{settings.Query}[/]");
                }
                AnsiConsole.MarkupLine(
                    $"Output directory: [cyan]{settings.OutputDirectory}[/]");
                AnsiConsole.MarkupLine($"Page size: [cyan]{settings.PageSize}[/]");
                if (settings.FirstPage != 1)
                    AnsiConsole.MarkupLine($"First page: [cyan]{settings.FirstPage}[/]");
                if (settings.LastPage != 0)
                    AnsiConsole.MarkupLine($"Last page: [cyan]{settings.LastPage}[/]");
                if (settings.MaxRowPerFile != 0)
                    AnsiConsole.MarkupLine($"Max rows per file: [cyan]{settings.MaxRowPerFile}[/]");
                AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");

                if (string.IsNullOrWhiteSpace(settings.Query))
                {
                    settings.Query = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter query:"));
                }

                if (string.IsNullOrEmpty(settings.Query)) return Task.FromResult(0);

                if (!settings.Query.StartsWith('['))
                {
                    settings.Query = $"[value=\"{settings.Query}\"]";
                }

                DateTime now = DateTime.Now;
                string fileName = BuildFileName(now,
                    settings.MaxRowPerFile > 0 ? 1 : 0);

                if (settings.MaxRowPerFile > 0)
                {
                    ProcessQueryWithMultipleFiles(settings.Query, repository,
                        settings, now);
                }
                else
                {
                    ProcessQuery(settings.Query, repository, settings, fileName);
                }
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

internal class ExportSearchCommandSettings : CommandSettings
{
    [Description("The search query")]
    [CommandOption("-q|--query <QUERY>")]
    public string Query { get; set; } = "";

    [Description("The output directory (default=desktop)")]
    [CommandOption("-o|--output <OUTPUT_DIR>")]
    public string OutputDirectory { get; set; } = Environment.GetFolderPath(
        Environment.SpecialFolder.DesktopDirectory);

    [Description("The virtual page size")]
    [CommandOption("-p|--page-size <PAGE_SIZE>")]
    [DefaultValue(100)]
    public int PageSize { get; set; } = 100;

    [Description("The first page to export")]
    [CommandOption("-f|--from-page <FIRST_PAGE>")]
    [DefaultValue(1)]
    public int FirstPage { get; set; } = 1;

    [Description("The last page to export (0=last)")]
    [CommandOption("-l|--last-page <LAST_PAGE>")]
    [DefaultValue(0)]
    public int LastPage { get; set; }

    [Description("The maximum number of rows per output file (0=unlimited)")]
    [CommandOption("-m|--max-rows <MAX_ROWS>")]
    [DefaultValue(0)]
    public int MaxRowPerFile { get; set; }

    [Description("The KWIC context size")]
    [CommandOption("-c|--context-size <CONTEXT_SIZE>")]
    [DefaultValue(5)]
    public int ContextSize { get; set; } = 5;

    [Description("The database name")]
    [CommandOption("-d|--db <DB_NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; } = "pythia";

    [Description("The file containing queries to process " +
        "(one per blank-line-separated block)")]
    [CommandOption("-s|--source-file <QUERY_SOURCE_FILE>")]
    public string? QueryFilePath { get; set; }
}
