using Pythia.Core;
using Pythia.Sql;
using Pythia.Sql.PgSql;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class BuildSqlCommand : AsyncCommand
{
    private readonly ISqlTermsQueryBuilder _termsBuilder;
    private TermFilter _filter;

    private readonly SqlQueryBuilder _textBuilder;
    private readonly List<string> _textHistory;
    private SearchRequest _request;
    private bool _includeCountSql;

    public BuildSqlCommand()
    {
        _termsBuilder = new SqlTermsQueryBuilder(new PgSqlHelper());
        _filter = new TermFilter();

        _textBuilder = new SqlQueryBuilder(new PgSqlHelper());
        _textHistory = new List<string>();
        _request = new SearchRequest
        {
            Query = "[value=\"chommoda\"]",
            PageNumber = 1,
            PageSize = 20
        };
        _textHistory.Add(_request.Query);
    }

    private void ShowTextQuery()
    {
        string text = AnsiConsole.Ask($"Text ({_request.Query.EscapeMarkup()}): ",
            _request.Query?.EscapeMarkup() ?? "");
        _textHistory.Add(text);

        var t = _textBuilder.Build(new SearchRequest
        {
            PageNumber = 1,
            PageSize = 20,
            Query = text
        });

        AnsiConsole.MarkupLine("[green underline] data [/]");
        AnsiConsole.MarkupLine($"[cyan]{t.Item1}[/]");

        if (_includeCountSql)
        {
            AnsiConsole.MarkupLine("[green underline] count [/]");
            AnsiConsole.MarkupLine($"[cyan]{t.Item2}[/]");
        }
        Console.WriteLine();
    }

    private void HandleTextHistory()
    {
        if (_textHistory.Count == 0) return;

        _request.Query = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Pick query from history")
            .PageSize(5)
            .MoreChoicesText("Move up/down to see more")
            .AddChoices(_textHistory));

        AnsiConsole.MarkupLine($"[cyan]{_request.Query}[/]");
    }

    #region Terms Query
    private void ShowTermsQuery(TermFilter filter)
    {
        var t = _termsBuilder.Build(filter);
        AnsiConsole.MarkupLine("[green underline] data [/]");
        AnsiConsole.MarkupLine($"[cyan]{t.Item1}[/]");

        if (_includeCountSql)
        {
            AnsiConsole.MarkupLine("[green underline]  count  [/]");
            AnsiConsole.MarkupLine($"[cyan]{t.Item2}[/]");
        }
    }

    private static DateTime? PromptForNullableDateTime(string message,
        string defaultValue = "")
    {
        while (true)
        {
            string text = AnsiConsole.Prompt(
                new TextPrompt<string>(message)
                .AllowEmpty()
                .DefaultValue(defaultValue));
            if (text.Length == 0) return null;

            if (DateTime.TryParse(text, out DateTime value)) return value;
            AnsiConsole.MarkupLine("[red]Invalid DateTime[/]");
        }
    }

    private void ShowTermFilterMenu(TermFilter filter)
    {
        switch (AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title("Pick filter property")
            .AddChoices(new[]
            {
                "PageNumber", "PageSize", "CorpusId", "Author", "Title",
                "Source", "ProfileId", "MinDateValue", "MaxDateValue",
                "MinTimeModified", "MaxTimeModified", "ValuePattern",
                "MinCount", "MaxCount", "SortOrder", "Descending",
                "DocAttrs", "TokAttrs", "BACK"
            })))
        {
            case "PageNumber":
                filter.PageNumber = AnsiConsole.Ask("PageNumber", filter.PageNumber);
                break;
            case "PageSize":
                filter.PageSize = AnsiConsole.Ask("PageSize", filter.PageSize);
                break;
            case "CorpusId":
                filter.CorpusId = AnsiConsole.Ask("CorpusId", filter.CorpusId!);
                break;
            case "Author":
                filter.Author = AnsiConsole.Ask("Author", filter.Author!);
                break;
            case "Title":
                filter.Title = AnsiConsole.Ask("Title", filter.Title!);
                break;
            case "Source":
                filter.Source = AnsiConsole.Ask("Source", filter.Source!);
                break;
            case "ProfileId":
                filter.ProfileId = AnsiConsole.Ask("ProfileId", filter.ProfileId!);
                break;
            case "MinDateValue":
                filter.MinDateValue = AnsiConsole.Ask("MinDateValue",
                    filter.MinDateValue);
                break;
            case "MaxDateValue":
                filter.MaxDateValue = AnsiConsole.Ask("MaxDateValue",
                    filter.MaxDateValue);
                break;
            case "MinTimeModified":
                filter.MinTimeModified = PromptForNullableDateTime("MinTimeModified",
                    filter.MinTimeModified?.ToString(CultureInfo.InvariantCulture) ?? "");
                break;
            case "MaxTimeModified":
                filter.MaxTimeModified = PromptForNullableDateTime("MaxTimeModified",
                    filter.MaxTimeModified?.ToString(CultureInfo.InvariantCulture) ?? "");
                break;
            case "ValuePattern":
                filter.ValuePattern = AnsiConsole.Ask("ValuePattern",
                    filter.ValuePattern!);
                break;
            case "MinCount":
                filter.MinCount = AnsiConsole.Ask("MinCount", filter.MinCount);
                break;
            case "MaxCount":
                filter.MaxCount = AnsiConsole.Ask("MaxCount", filter.MaxCount);
                break;
            case "SortOrder":
                filter.SortOrder = (TermSortOrder)Enum.Parse(typeof(TermSortOrder),
                    AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .Title("Sort order")
                        .AddChoices(new[]
                        {
                            nameof(TermSortOrder.Default),
                            nameof(TermSortOrder.ByValue),
                            nameof(TermSortOrder.ByReversedValue),
                            nameof(TermSortOrder.ByCount),
                        })));
                break;
            case "Descending":
                filter.IsSortDescending = AnsiConsole.Confirm("Descending?", false);
                break;
            case "DocAttrs":
                string da = filter.DocumentAttributes != null
                    ? string.Join(",", filter.DocumentAttributes.Select(
                        t => $"{t.Item1}={t.Item2}"))
                    : "";
                filter.DocumentAttributes = ParseAttributes(
                    AnsiConsole.Ask("DocAttrs (n=v,...)?", da)).ToList();
                break;
            case "TokAttrs":
                string ta = filter.OccurrenceAttributes != null
                    ? string.Join(",", filter.OccurrenceAttributes.Select(
                        t => $"{t.Item1}={t.Item2}"))
                    : "";
                filter.OccurrenceAttributes = ParseAttributes(
                    AnsiConsole.Ask("TokAttrs (n=v,...)?", ta)).ToList();
                break;
            case "BACK":
                ShowTermsQuery(filter);
                break;
        }
    }

    private static IList<Tuple<string, string>> ParseAttributes(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<Tuple<string,string>>();

        List<Tuple<string, string>> attrs = new();

        foreach (string pair in text.Split(',',
            StringSplitOptions.RemoveEmptyEntries))
        {
            Match m = Regex.Match(pair, @"^\s*([^=]+)=(.*)",
                RegexOptions.Compiled);
            if (m.Success)
                attrs.Add(Tuple.Create(m.Groups[1].Value, m.Groups[2].Value));
        }

        return attrs;
    }
    #endregion

    public override Task<int> ExecuteAsync(CommandContext context)
    {
        AnsiConsole.MarkupLine("[green underline]BUILD SQL[/]");

        while (true)
        {
            try
            {
                AnsiConsole.MarkupLine(
                    "[green]Q[/]uery | " +
                    "[green]C[/]ount toggle | " +
                    "[green]T[/]erms | " +
                    "[green]H[/]istory | " +
                    "[yellow]R[/]eset | " +
                    "e[red]X[/]it");
                char c = Console.ReadKey().KeyChar;
                Console.WriteLine();

                switch (c)
                {
                    case 'q':   // query
                        ShowTextQuery();
                        break;
                    case 'c':   // count toggle
                        _includeCountSql = !_includeCountSql;
                        AnsiConsole.MarkupLine(
                            "Include count SQL: [cyan]" +
                            $"{(_includeCountSql ? "yes" : "no")}[/]");
                        break;
                    case 't':   // terms
                        ShowTermFilterMenu(_filter);
                        break;
                    case 'h':   // history
                        HandleTextHistory();
                        break;
                    case 'r':   // reset
                        if (AnsiConsole.Confirm("Reset?", false))
                        {
                            _request = new SearchRequest();
                            _filter = new TermFilter();
                        }
                        break;
                    case 'x':   // exit
                        return Task.FromResult(0);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                AnsiConsole.WriteException(e);
                return Task.FromResult(2);
            }
        }
    }
}
