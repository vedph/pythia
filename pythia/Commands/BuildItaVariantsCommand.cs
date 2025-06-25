using Pythia.Tagger.Ita.Plugin;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

public sealed class BuildItaVariantsCommand : AsyncCommand
{
    private readonly List<string> _history = [];

    private void AddToHistory(string text)
    {
        if (_history.Contains(text)) return;
        _history.Insert(0, text);
        if (_history.Count > 10)
        {
            _history.RemoveAt(_history.Count - 1);
        }
    }

    private string? PickFromHistory()
    {
        if (_history.Count == 0) return null;

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title("Pick form")
            .AddChoices(_history.Select(s => s.EscapeMarkup())));
    }

    public override Task<int> ExecuteAsync(CommandContext context)
    {
        AnsiConsole.MarkupLine("[green underline]BUILD ITALIAN VARIANTS[/]");

        string prevForm = "facendone";
        while (true)
        {
            try
            {
                string? form = AnsiConsole.Ask(
                    "Form or POS Form ([red]x[/]=exit, [cyan]h[/]=history): ",
                    prevForm.EscapeMarkup());

                switch (form)
                {
                    case "x":
                        return Task.FromResult(0);

                    case "h":
                        form = PickFromHistory();
                        break;

                    default:
                        AddToHistory(form);
                        break;
                }

                if (form != null)
                {
                    prevForm = form;

                    string? pos = null;
                    int i = form.IndexOf(' ');
                    if (i > -1)
                    {
                        pos = form[..i];
                        form = form[(i + 1)..];
                    }

                    //foreach (Variant v in _builder.Build(form, pos, _index))
                    //{
                    //}
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                AnsiConsole.WriteException(ex);
            }
        }
    }
}

