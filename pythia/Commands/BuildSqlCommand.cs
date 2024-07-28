﻿using Pythia.Core;
using Pythia.Sql;
using Pythia.Sql.PgSql;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class BuildSqlCommand : AsyncCommand
{
    private readonly ISqlWordQueryBuilder _wordBuilder;
    private WordFilter _filter;

    private readonly SqlQueryBuilder _textBuilder;
    private readonly List<string> _textHistory;
    private SearchRequest _request;
    private bool _includeCountSql;

    public BuildSqlCommand()
    {
        _wordBuilder = new SqlWordQueryBuilder(new PgSqlHelper());
        _filter = new WordFilter();

        _textBuilder = new SqlQueryBuilder(new PgSqlHelper());
        _textHistory = [];
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
        text = text.Replace("[[", "[").Replace("]]", "]");
        _textHistory.Add(text);

        var t = _textBuilder.Build(new SearchRequest
        {
            PageNumber = 1,
            PageSize = 20,
            Query = text
        });

        AnsiConsole.MarkupLine("[green underline] data [/]");
        AnsiConsole.MarkupLine($"[cyan]{Markup.Escape(t.Item1)}[/]");

        if (_includeCountSql)
        {
            AnsiConsole.MarkupLine("[green underline] count [/]");
            AnsiConsole.MarkupLine($"[cyan]{Markup.Escape(t.Item2)}[/]");
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

    #region Word Query
    private void ShowWordQuery(WordFilter filter)
    {
        var t = _wordBuilder.Build(filter);
        AnsiConsole.MarkupLine("[green underline] data [/]");
        AnsiConsole.MarkupLine($"[cyan]{t.Item1}[/]");

        if (_includeCountSql)
        {
            AnsiConsole.MarkupLine("[green underline]  count  [/]");
            AnsiConsole.MarkupLine($"[cyan]{t.Item2}[/]");
        }
    }

    //private static DateTime? PromptForNullableDateTime(string message,
    //    string defaultValue = "")
    //{
    //    while (true)
    //    {
    //        string text = AnsiConsole.Prompt(
    //            new TextPrompt<string>(message)
    //            .AllowEmpty()
    //            .DefaultValue(defaultValue));
    //        if (text.Length == 0) return null;

    //        if (DateTime.TryParse(text, out DateTime value)) return value;
    //        AnsiConsole.MarkupLine("[red]Invalid DateTime[/]");
    //    }
    //}

    private void ShowWordFilterMenu(WordFilter filter)
    {
        switch (AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Pick filter property")
            .AddChoices("PageNumber", "PageSize", "Language", "Lemma", "Pos",
                "ValuePattern", "IsValuePatternReversed",
                "MinValueLength", "MaxValueLength",
                "MinCount", "MaxCount",
                "SortOrder", "IsSortDescending",
                "BACK"
            )))
        {
            case "PageNumber":
                filter.PageNumber = AnsiConsole.Ask("PageNumber", filter.PageNumber);
                break;
            case "PageSize":
                filter.PageSize = AnsiConsole.Ask("PageSize", filter.PageSize);
                break;
            case "Language":
                filter.Language = AnsiConsole.Ask("Language", filter.Language);
                break;
            case "Lemma":
                filter.Lemma = AnsiConsole.Ask("Lemma", filter.Lemma);
                break;
            case "Pos":
                filter.Pos = AnsiConsole.Ask("Pos", filter.Pos);
                break;
            case "MinValueLength":
                filter.MinValueLength = AnsiConsole.Ask("MinValueLength",
                    filter.MinValueLength);
                break;
            case "MaxValueLength":
                filter.MaxValueLength = AnsiConsole.Ask("MaxValueLength",
                    filter.MaxValueLength);
                break;
            case "ValuePattern":
                filter.ValuePattern = AnsiConsole.Ask("ValuePattern",
                    filter.ValuePattern!);
                break;
            case "IsValuePatternReversed":
                filter.IsValuePatternReversed = AnsiConsole.Confirm(
                    "IsValuePatternReversed");
                break;
            case "MinCount":
                filter.MinCount = AnsiConsole.Ask("MinCount", filter.MinCount);
                break;
            case "MaxCount":
                filter.MaxCount = AnsiConsole.Ask("MaxCount", filter.MaxCount);
                break;
            case "SortOrder":
                filter.SortOrder = (WordSortOrder)Enum.Parse(typeof(WordSortOrder),
                    AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .Title("Sort order")
                        .AddChoices(nameof(WordSortOrder.Default),
                            nameof(WordSortOrder.ByValue),
                            nameof(WordSortOrder.ByReversedValue),
                            nameof(WordSortOrder.ByCount)
                        )));
                break;
            case "Descending":
                filter.IsSortDescending = AnsiConsole.Confirm(
                    "IsSortDescending?", false);
                break;
            case "BACK":
                ShowWordQuery(filter);
                break;
        }
    }
    #endregion

    public override Task<int> ExecuteAsync(CommandContext context)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[green underline]BUILD SQL[/]");

        while (true)
        {
            try
            {
                AnsiConsole.MarkupLine(
                    "[green]Q[/]uery | " +
                    "[green]C[/]ount toggle | " +
                    "[green]W[/]ords | " +
                    "[green]H[/]istory | " +
                    "[yellow]R[/]eset | " +
                    "e[red]X[/]it");
                char c = char.ToLowerInvariant(Console.ReadKey().KeyChar);
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
                    case 'w':   // words
                        ShowWordFilterMenu(_filter);
                        break;
                    case 'h':   // history
                        HandleTextHistory();
                        break;
                    case 'r':   // reset
                        if (AnsiConsole.Confirm("Reset?", false))
                        {
                            _request = new SearchRequest();
                            _filter = new WordFilter();
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
            }
        }
    }
}
