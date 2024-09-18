using Corpus.Sql;
using Fusi.Tools.Data;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Core;
using Pythia.Sql;
using Pythia.Sql.PgSql;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class QueryCommand : AsyncCommand<QueryCommandSettings>
{
    private readonly List<string> _history;
    private readonly SearchRequest _request;
    private DataPage<SearchResult>? _page;
    private SqlIndexRepository? _repository;

    public QueryCommand()
    {
        _history = [];
        _request = new SearchRequest
        {
            Query = "[value=\"chommoda\"]",
            PageNumber = 1,
            PageSize = 20
        };
        _history.Add(_request.Query);
    }

    private void HandleHistory()
    {
        if (_history.Count == 0) return;

        string chosen = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title("Pick query")
            .AddChoices(_history.Select(s => s.EscapeMarkup())));
        chosen = chosen.Replace("[[", "[").Replace("]]", "]");

        _request.Query = chosen;
        AnsiConsole.MarkupLineInterpolated($"[cyan]{chosen}[/]");
    }

    private static void ShowResults(IList<SearchResult> results)
    {
        Table table = new();

        table.AddColumns("doc", "p1", "p2", "idx", "len", "et", "id",
            "value", "author", "title");
        foreach (SearchResult result in results)
        {
            table.AddRow($"{result.DocumentId}",
                $"{result.P1}",
                $"{result.P2}",
                $"{result.Index}",
                $"{result.Length}",
                result.Type ?? "",
                $"{result.Id}",
                result.Value ?? "",
                result.Author ?? "",
                result.Title ?? "");
        }

        AnsiConsole.Write(table);
    }

    private void ShowKwic(int contextSize = 3)
    {
        if (_repository == null || _page == null) return;

        int n = 0;
        IList<KwicSearchResult> kwics = _repository.GetResultContext(
            _page!.Items, contextSize);

        foreach (KwicSearchResult kwic in kwics)
        {
            AnsiConsole.MarkupLine(
                $"{++n:000} " +
                $"{string.Join(" | ", kwic.LeftContext)}" +
                $" | [yellow]{kwic.Text}[/] | " +
                $"{string.Join(" | ", kwic.RightContext)}");
        }
    }

    private void ShowPage()
    {
        if (_page == null || _page.Total == 0)
        {
            AnsiConsole.MarkupLine("[yellow](no result)[/]");
            return;
        }

        char c = '\0';
        while (true)
        {
            AnsiConsole.MarkupLine("[green underline] page " +
                $"{_page.PageNumber}/{_page.PageCount} ({_page.Total})[/]");

            if (c != 'k') ShowResults(_page.Items);

            AnsiConsole.MarkupLine(
                "[cyan]N[/]ext | [cyan]P[/]rev | [cyan]F[/]irst | " +
                "[cyan]L[/]ast | [cyan]K[/]WIC | [yellow]C[/]lose");
            c = char.ToLowerInvariant(Console.ReadKey().KeyChar);
            Console.WriteLine();

            switch (c)
            {
                case 'n':
                    if (_page.PageNumber == _page.PageCount) break;
                    _request.PageNumber++;
                    _page = _repository!.Search(_request);
                    break;

                case 'p':
                    if (_page.PageNumber == 1) break;
                    _request.PageNumber--;
                    _page = _repository!.Search(_request);
                    break;

                case 'f':
                    if (_page.PageNumber == 1) break;
                    _request.PageNumber = 1;
                    _page = _repository!.Search(_request);
                    break;

                case 'l':
                    if (_page.PageNumber == _page.PageCount) break;
                    _request.PageNumber = _page.PageCount;
                    _page = _repository!.Search(_request);
                    break;

                case 'k':
                    ShowKwic();
                    break;

                case 'c':
                    return;
            }
        }
    }

    private void AddToHistory(string text)
    {
        if (_history.Contains(text)) return;
        _history.Insert(0, text);
    }

    public override Task<int> ExecuteAsync(CommandContext context,
        QueryCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]QUERY[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");

        string cs = string.Format(
            CliAppContext.Configuration!.GetConnectionString("Default")!,
            settings.DbName);
        _repository = new PgSqlIndexRepository();
        _repository.Configure(new SqlRepositoryOptions
        {
            ConnectionString = cs
        });

        string prevQuery = "[value=\"chommoda\"]";
        while (true)
        {
            try
            {
                string query = AnsiConsole.Ask(
                    "Query ([red]x[/]=exit, [cyan]h[/]=history): ",
                    prevQuery.EscapeMarkup()).Replace("[[", "[").Replace("]]", "]");

                switch (query)
                {
                    case "x":
                        return Task.FromResult(0);

                    case "h":
                        HandleHistory();
                        _request.PageNumber = 1;
                        _page = _repository.Search(_request);
                        ShowPage();
                        break;

                    default:
                        AddToHistory(query);
                        _request.PageNumber = 1;
                        _request.Query = query;
                        _page = _repository.Search(_request);
                        ShowPage();
                        break;
                }

                prevQuery = query;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                AnsiConsole.WriteException(ex);
            }
        }
    }
}

internal class QueryCommandSettings : CommandSettings
{
    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; } = "pythia";
}
