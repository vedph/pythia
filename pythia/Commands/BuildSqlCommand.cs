using Fusi.Cli;
using Microsoft.Extensions.CommandLineUtils;
using Pythia.Core;
using Pythia.Sql;
using Pythia.Sql.PgSql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

public sealed class BuildSqlCommand : ICommand
{
    private readonly SqlTermsQueryBuilder _termsBuilder;
    private TermFilter _filter;

    private readonly SqlQueryBuilder _textBuilder;
    private readonly History<string> _textHistory;
    private SearchRequest _request;
    private bool _includeCountSql;

    public BuildSqlCommand()
    {
        _termsBuilder = new SqlTermsQueryBuilder(new PgSqlHelper());
        _filter = new TermFilter();

        _textBuilder = new SqlQueryBuilder(new PgSqlHelper());
        _textHistory = new History<string>();
        _request = new SearchRequest
        {
            Query = "[value=\"chommoda\"]",
            PageNumber = 1,
            PageSize = 20
        };
        _textHistory.Add(_request.Query);
    }

    public static void Configure(CommandLineApplication command,
        AppOptions options)
    {
        command.Description = "Build SQL code from queries.";
        command.HelpOption("-?|-h|--help");

        command.OnExecute(() =>
        {
            options.Command = new BuildSqlCommand();
            return 0;
        });
    }

    private void ShowTextQuery()
    {
        string text = Prompt.ForString($"Text [{_request.Query}]: ",
            _request.Query!);
        _textHistory.Add(text);

        var t = _textBuilder.Build(new SearchRequest
        {
            PageNumber = 1,
            PageSize = 20,
            Query = text
        });

        ColorConsole.WriteSuccess("-- data");
        Console.WriteLine(t.Item1);

        if (_includeCountSql)
        {
            ColorConsole.WriteSuccess("-- count");
            Console.WriteLine(t.Item2);
        }
        Console.WriteLine();
    }

    private void HandleTextHistory()
    {
        if (_textHistory.Count == 0) return;

        _request.Query = Prompt.ForHistory("Enter nr.: ", _textHistory);
        ColorConsole.WriteInfo(_request.Query);
    }

    #region Terms Query
    private void ShowTermsQuery(TermFilter filter)
    {
        var t = _termsBuilder.Build(filter);
        ColorConsole.WriteSuccess("-- data");
        Console.WriteLine(t.Item1);

        if (_includeCountSql)
        {
            ColorConsole.WriteSuccess("-- count");
            Console.WriteLine(t.Item2);
        }
        Console.WriteLine();
    }

    private static void ShowProperty(int n, string name, object? value) =>
        ColorConsole.WriteEmbeddedColorLine($"[yellow]{n:00}[/yellow] " +
            $"[cyan]{name}[/cyan]: {value?.ToString()}");

    private static void HandleTermFilterMenu1(TermFilter filter)
    {
        switch (Prompt.ForInt("Pick 1-14", 1))
        {
            case 1:
                filter.PageNumber = Prompt.ForInt("PageNumber", filter.PageNumber);
                break;
            case 2:
                filter.PageSize = Prompt.ForInt("PageSize", filter.PageSize);
                break;
            case 3:
                filter.CorpusId = Prompt.ForString("CorpusId", filter.CorpusId!);
                break;
            case 4:
                filter.Author = Prompt.ForString("Author", filter.Author!);
                break;
            case 5:
                filter.Title = Prompt.ForString("Title", filter.Title!);
                break;
            case 6:
                filter.Source = Prompt.ForString("Source", filter.Source!);
                break;
            case 7:
                filter.ProfileId = Prompt.ForString("ProfileId", filter.ProfileId!);
                break;
            case 8:
                filter.MinDateValue = Prompt.ForDouble("MinDateValue",
                    filter.MinDateValue);
                break;
            case 9:
                filter.MaxDateValue = Prompt.ForDouble("MaxDateValue",
                    filter.MaxDateValue);
                break;
            case 10:
                filter.MinTimeModified = Prompt.ForNullableDateTime("MinTimeModified",
                    filter.MinTimeModified);
                break;
            case 11:
                filter.MaxTimeModified = Prompt.ForNullableDateTime("MaxTimeModified",
                    filter.MaxTimeModified);
                break;
            case 12:
                filter.ValuePattern = Prompt.ForString("ValuePattern",
                    filter.ValuePattern!);
                break;
            case 13:
                filter.MinCount = Prompt.ForInt("MinCount", filter.MinCount);
                break;
            case 14:
                filter.MaxCount = Prompt.ForInt("MaxCount", filter.MaxCount);
                break;
        }
    }

    private void ShowTermFilterMenu1(TermFilter filter)
    {
        while (true)
        {
            ColorConsole.WriteInfo("Term Filter - [1/2]");

            ShowProperty(1, "PageNumber", filter.PageNumber);
            ShowProperty(2, "PageSize", filter.PageSize);
            ShowProperty(3, "CorpusId", filter.CorpusId);
            ShowProperty(4, "Author", filter.Author);
            ShowProperty(5, "Title", filter.Title);
            ShowProperty(6, "Source", filter.Source);
            ShowProperty(7, "ProfileId", filter.ProfileId);
            ShowProperty(8, "MinDateValue", filter.MinDateValue);
            ShowProperty(9, "MaxDateValue", filter.MaxDateValue);
            ShowProperty(10, "MinTimeModified", filter.MinTimeModified);
            ShowProperty(11, "MaxTimeModified", filter.MaxTimeModified);
            ShowProperty(12, "ValuePattern", filter.ValuePattern);
            ShowProperty(13, "MinCount", filter.MinCount);
            ShowProperty(14, "MaxCount", filter.MaxCount);

            ColorConsole.WriteEmbeddedColorLine(
                "[green]P[/green]ick nr | " +
                "Page [green]2[/green] | " +
                "[cyan]B[/cyan]uild | " +
                "[yellow]C[/yellow]lose");

            switch (Prompt.ForChar("Press key"))
            {
                case 'p':
                    HandleTermFilterMenu1(filter);
                    break;
                case '2':
                    ShowTermFilterMenu2(filter);
                    return;
                case 'c':
                    return;
                case 'b':
                    ShowTermsQuery(filter);
                    break;
            }
        }
    }

    private static List<Tuple<string, string>>? ParseAttributes(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        List<Tuple<string, string>> attrs = new();

        foreach (string pair in text.Split(',',
            StringSplitOptions.RemoveEmptyEntries))
        {
            Match m = Regex.Match(pair, @"^\s*([^=]+)=(.*)");
            if (m.Success)
                attrs.Add(Tuple.Create(m.Groups[1].Value, m.Groups[2].Value));
        }

        return attrs;
    }

    private static void HandleTermFilterMenu2(TermFilter filter)
    {
        switch (Prompt.ForInt("Pick 1-4", 1))
        {
            case 1:
                filter.SortOrder = (TermSortOrder)Prompt.ForInt(
                    "SortOrder (0=default 1=val 2=rev-val 3=count): ", 0);
                break;
            case 2:
                filter.IsSortDescending = Prompt.ForBool("Descending", false);
                break;
            case 3:
                string? da = Prompt.ForString("DocAttrs (n=v,...): ",
                    filter.DocumentAttributes?.Count > 0
                    ? string.Join(", ", filter.DocumentAttributes
                        .Select(t => $"{t.Item1}={t.Item2}"))
                    : "");
                filter.DocumentAttributes = ParseAttributes(da);
                break;
            case 4:
                string? ta = Prompt.ForString("TokAttrs (n=v,...): ",
                    filter.OccurrenceAttributes?.Count > 0
                    ? string.Join(", ", filter.OccurrenceAttributes
                        .Select(t => $"{t.Item1}={t.Item2}"))
                    : "");
                filter.OccurrenceAttributes = ParseAttributes(ta);
                break;
        }
    }

    private void ShowTermFilterMenu2(TermFilter filter)
    {
        ColorConsole.WriteInfo("Term Filter - [2/2]");

        ShowProperty(1, "SortOrder", filter.SortOrder);
        ShowProperty(2, "Descending", filter.IsSortDescending);
        ShowProperty(3, "DocAttrs",
            filter.DocumentAttributes?.Count > 0
            ? string.Join(", ", filter.DocumentAttributes.Select(a => $"{a.Item1}={a.Item2}"))
            : "");
        ShowProperty(4, "TokAttrs",
            filter.OccurrenceAttributes?.Count > 0
            ? string.Join(", ", filter.OccurrenceAttributes.Select(a => $"{a.Item1}={a.Item2}"))
            : "");

        ColorConsole.WriteEmbeddedColorLine(
            "[green]P[/green]ick nr | " +
            "Page [green]1[/green] | " +
            "[cyan]B[/cyan]uild | " +
            "[yellow]C[/yellow]lose");

        switch (Prompt.ForChar("Press key"))
        {
            case 'p':
                HandleTermFilterMenu2(filter);
                break;
            case '1':
                ShowTermFilterMenu1(filter);
                return;
            case 'c':
                return;
            case 'b':
                ShowTermsQuery(filter);
                break;
        }
    }
    #endregion

    public Task<int> Run()
    {
        ColorConsole.WriteWrappedHeader("Build SQL",
            headerColor: ConsoleColor.Green);

        while (true)
        {
            try
            {
                char c = Prompt.ForChar(
                    "[green]Q[/green]uery | " +
                    "[green]C[/green]ount toggle | " +
                    "[green]T[/green]erms | " +
                    "[green]H[/green]istory | " +
                    "[yellow]R[/yellow]eset | " +
                    "e[red]X[/red]it");

                switch (c)
                {
                    case 'q':
                        ShowTextQuery();
                        break;
                    case 'c':
                        _includeCountSql = !_includeCountSql;
                        ColorConsole.WriteInfo("Include count SQL: " +
                            (_includeCountSql ? "yes" : "no"));
                        break;
                    case 't':
                        ShowTermFilterMenu1(_filter);
                        break;
                    case 'h':
                        HandleTextHistory();
                        break;
                    case 'r':
                        if (Prompt.ForBool("Reset", false))
                        {
                            _request = new SearchRequest();
                            _filter = new TermFilter();
                        }
                        break;
                    case 'x':
                        return Task.FromResult(0);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                ColorConsole.WriteError(e.Message);
            }
        }
    }
}
