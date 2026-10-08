using Pythia.Tagger;
using Pythia.Tagger.Ita.Plugin;
using Pythia.Tagger.LiteDB;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

/// <summary>
/// Interactively builds and displays Italian word variants using a lookup index
/// and user input.
/// </summary>
/// <remarks>This command prompts the user for Italian forms or part-of-speech
/// forms, maintains a history of recent inputs for quick selection, and displays
/// generated variants with relevant details.</remarks>
internal sealed class BuildItaVariantsCommand :
    AsyncCommand<BuildItaVariantsCommandSettings>
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

    public override Task<int> ExecuteAsync(CommandContext context,
        BuildItaVariantsCommandSettings settings, CancellationToken cancel)
    {
        AnsiConsole.MarkupLine("[green underline]BUILD ITALIAN VARIANTS[/]");
        try
        {
            using LiteDBLookupIndex index = new(settings.LookupIndexPath, true);
            ItalianVariantBuilder builder = new();

            string prevForm = "facendone";
            while (true)
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

                    int n = 0;
                    foreach (VariantForm v in builder.Build(form, pos, index))
                    {
                        AnsiConsole.MarkupLine(
                            $"{++n:00}. " +
                            $"[cyan]{v.Value.EscapeMarkup()}[/] " +
                            $"[white]{v.Source?.EscapeMarkup()}[/] " +
                            $"[green]{v.Type}[/] " +
                            $"[yellow]{v.Pos}[/]");
                    }
                    AnsiConsole.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            AnsiConsole.WriteException(ex);
            return Task.FromResult(1);
        }
    }
}

public class BuildItaVariantsCommandSettings : CommandSettings
{
    /// <summary>
    /// The path to the lookup index file to use for building variants.
    /// </summary>
    [CommandArgument(0, "[input]")]
    public required string LookupIndexPath { get; set; }
}