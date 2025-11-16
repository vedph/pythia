using Pythia.Udp.Plugin;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

/// <summary>
/// Processes UDP chunk data from an input file and writes the results to an
/// output file.
/// </summary>
/// <remarks>This command is typically used in scenarios where UDP chunk
/// segmentation and tagging are required for analysis. The command reads the
/// input file, applies chunking logic based on the provided settings, and
/// outputs the processed chunks.</remarks>
internal sealed class DumpUdpChunkCommand : AsyncCommand<DumpUdpChunkCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context,
        DumpUdpChunkCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[underline green]DUMP UDP CHUNKS[/]");
        AnsiConsole.MarkupLine($"Input path: [cyan]{settings.InputPath}[/]");
        AnsiConsole.MarkupLine($"Output path: [cyan]{settings.OutputPath}[/]");
        AnsiConsole.MarkupLine($"Max chunk length: [cyan]{settings.MaxLength}[/]");
        AnsiConsole.MarkupLine($"Fill chunk tags: [cyan]{settings.FillChunkTags}[/]");
        AnsiConsole.MarkupLine($"Black tags: [cyan]{settings.BlackTags}[/]");

        try
        {
            string text;
            using (StreamReader reader = new(settings.InputPath!, Encoding.UTF8))
            {
                text = reader.ReadToEnd();
            }

            UdpChunkBuilder builder = new()
            {
                MaxLength = settings.MaxLength,
                BlackTags = string.IsNullOrEmpty(settings.BlackTags)
                    ? null : new HashSet<string>(settings.BlackTags.Split(','))
            };
            using StreamWriter writer = new(settings.OutputPath!, false,
                Encoding.UTF8);
            foreach (UdpChunk chunk in builder.Build(text))
            {
                writer.WriteLine($"-----#{chunk}-----");
                writer.WriteLine(text.AsSpan(
                    chunk.Range.Start, chunk.Range.Length));
            }
            writer.Flush();
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

public class DumpUdpChunkCommandSettings : CommandSettings
{
    [Description("The input file path")]
    [CommandArgument(0, "<INPUT_PATH>")]
    public string? InputPath { get; set; }

    [Description("The output file path")]
    [CommandArgument(0, "<OUTPUT_PATH>")]
    public string? OutputPath { get; set; }

    [Description("Maximum chunk length")]
    [CommandOption("-l|--len <CHUNK_LENGTH>")]
    [DefaultValue(5000)]
    public int MaxLength { get; set; }

    [Description("Blank-fill XML chunk tags before UDP")]
    [CommandOption("-f|--fill")]
    public bool FillChunkTags { get; set; }

    [Description("Exclude matches in the specified CSV list of elements")]
    [CommandOption("-x|--black-tags <BLACK_TAG>")]
    public string? BlackTags { get; set; }
}
