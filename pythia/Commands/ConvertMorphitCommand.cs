using Fusi.Tools;
using Pythia.Tagger.Ita.Plugin;
using Pythia.Tagger.LiteDB;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class ConvertMorphitCommand :
    AsyncCommand<ConvertMorphitCommandSettings>
{
    private static void ShowSettings(
        ConvertMorphitCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]CONVERT MORPHIT[/]");
        AnsiConsole.MarkupLine($"Input: [cyan]{settings.Input}[/]");
        AnsiConsole.MarkupLine($"Output: [cyan]{settings.Output}[/]");
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ConvertMorphitCommandSettings settings)
    {
        ShowSettings(settings);

        using LiteDBLookupIndex index = new(settings.Output);
        MorphitConverter converter = new(index);

        try
        {
            using FileStream input = new(settings.Input, FileMode.Open,
                FileAccess.Read, FileShare.Read);
            using StreamReader reader = new(input, Encoding.UTF8);
            converter.Convert(reader, CancellationToken.None,
                new Progress<ProgressReport>(r =>
                {
                    Console.WriteLine(r.Message);
                }));

            index.Optimize();
            AnsiConsole.MarkupLine("[green]Completed[/]");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            Debug.WriteLine(ex.ToString());
            return Task.FromResult(1);
        }
    }
}

public class ConvertMorphitCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the input file path.
    /// </summary>
    [CommandArgument(0, "[input]")]
    public required string Input { get; set; }

    /// <summary>
    /// Gets or sets the output file path.
    /// </summary>
    [CommandArgument(1, "[output]")]
    public required string Output { get; set; }
}
