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
using System.Text;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

/// <summary>
/// Export the results of the specified search into one or more CSV files.
/// </summary>
internal sealed class ExportSearchCommand : AsyncCommand<ExportSearchCommandSettings>
{
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

    public override Task<int> ExecuteAsync(CommandContext context,
        ExportSearchCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]EXPORT SEARCH[/]");
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

        try
        {
            // prompt if query is empty
            if (string.IsNullOrWhiteSpace(settings.Query))
            {
                settings.Query = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter query")
                    .DefaultValue(settings.Query));
            }
            if (string.IsNullOrEmpty(settings.Query)) return Task.FromResult(0);

            // create the repository
            string cs = string.Format(
                CliAppContext.Configuration!.GetConnectionString("Default")!,
                settings.DbName);
            PgSqlIndexRepository repository = new();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });

            // prepare the search request
            SearchRequest request = new()
            {
                Query = settings.Query,
                PageNumber = settings.FirstPage,
                PageSize = settings.PageSize
            };

            // search page by page
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

                // file name
                DateTime now = DateTime.Now;
                string fileName = BuildFileName(now,
                    settings.MaxRowPerFile > 0 ? 1 : 0);
                int rowCount = 0, fileNr = 1;

                StreamWriter writer = new(
                    Path.Combine(settings.OutputDirectory, fileName),
                    false, Encoding.UTF8);
                CsvWriter csvWriter = new(writer, CultureInfo.InvariantCulture);

                while (request.PageNumber <= lastPage)
                {
                    IList<KwicSearchResult> results =
                        repository.GetResultContext(
                            page.Items, settings.ContextSize);

                    foreach (SearchResult result in results)
                    {
                        csvWriter.WriteRecord(result);

                        if (++rowCount >= settings.MaxRowPerFile
                            && settings.MaxRowPerFile > 0)
                        {
                            csvWriter.Flush();
                            rowCount = 0;
                            fileName = BuildFileName(now, ++fileNr);

                            writer = new StreamWriter(
                                Path.Combine(settings.OutputDirectory, fileName),
                                false, Encoding.UTF8);
                            csvWriter = new CsvWriter(writer,
                                CultureInfo.InvariantCulture);
                        }
                    }
                    task.Value = (double)request.PageNumber * 100 / page.PageCount;
                    request.PageNumber++;
                    page = repository.Search(request);
                }

                csvWriter.Flush();
            });
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
    [DefaultValue(20)]
    public int PageSize { get; set; } = 20;

    [Description("The first page to export")]
    [CommandOption("-f|--from-page <FROM_PAGE>")]
    [DefaultValue(1)]
    public int FirstPage { get; set; } = 1;

    [Description("The last page to export (0=last)")]
    [CommandOption("-l|--last-page <FROM_PAGE>")]
    [DefaultValue(0)]
    public int LastPage { get; set; }

    [Description("The maximum number of rows per output file (0=unlimited)")]
    [CommandOption("-m|--max-rows <MAX_ROW_NUMBER>")]
    [DefaultValue(0)]
    public int MaxRowPerFile { get; set; }

    [Description("The KWIC context size")]
    [CommandOption("-c|--context-size <CONTEXT_SIZE>")]
    [DefaultValue(5)]
    public int ContextSize { get; set; } = 5;

    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; } = "pythia";
}
