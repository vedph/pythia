using Fusi.Tools;
using MessagePack;
using Pythia.Tagger.Ita.Plugin;
using Pythia.Tagger.Lookup;
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

        MorphitConverter converter = new(new MessagePackLookupEntrySerializer(
            MessagePackSerializerOptions.Standard
            .WithCompression(MessagePackCompression.Lz4BlockArray)));

        try
        {
            using FileStream input = new(settings.Input, FileMode.Open,
                FileAccess.Read, FileShare.Read);
            using StreamReader reader = new(input, Encoding.UTF8);
            using FileStream output = new(settings.Output, FileMode.Create,
                FileAccess.Write);
            converter.Convert(reader, output, CancellationToken.None,
                new Progress<ProgressReport>(r =>
                {
                    Console.WriteLine(r.Message);
                }));

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
