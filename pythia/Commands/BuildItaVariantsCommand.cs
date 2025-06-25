using MessagePack;
using Pythia.Tagger;
using Pythia.Tagger.Ita.Plugin;
using Pythia.Tagger.Lookup;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

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

    private static IEnumerable<LookupEntry> ReadIndexEntries(string path)
    {
        MessagePackLookupEntrySerializer serializer = new(
            MessagePackSerializerOptions.Standard
            .WithCompression(MessagePackCompression.Lz4BlockArray));
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read,
            FileShare.Read);

        while (serializer.Deserialize(stream) is LookupEntry entry)
        {
            yield return entry;
        }
    }

    public override Task<int> ExecuteAsync(CommandContext context,
        BuildItaVariantsCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]BUILD ITALIAN VARIANTS[/]");
        RamLookupIndex? index = null;
        ItalianVariantBuilder builder = new();

        string prevForm = "facendone";
        while (true)
        {
            // load the index if not already loaded
            if (index == null)
            {
                try
                {
                    AnsiConsole.Write("Reading index... ");
                    index = new(ReadIndexEntries(settings.LookupIndexPath));
                    AnsiConsole.Write("complete.");
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    return Task.FromResult(1);
                }
            }
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

                    int n = 0;
                    foreach (VariantForm v in builder.Build(form, pos, index))
                    {
                        AnsiConsole.WriteLine(
                            $"{++n:00}. " +
                            $"[blue]{v.Value.EscapeMarkup()}[/] " +
                            $"[green]{v.Type}[/] " +
                            $"{(string.IsNullOrEmpty(v.Pos)
                                ? "" : $"[yellow]{v.Pos}[/]")}");
                    }
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

public class BuildItaVariantsCommandSettings : CommandSettings
{
    /// <summary>
    /// The path to the lookup index file to use for building variants.
    /// </summary>
    [CommandArgument(0, "[input]")]
    public required string LookupIndexPath { get; set; }
}