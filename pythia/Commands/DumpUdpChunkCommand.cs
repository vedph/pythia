using Pythia.Udp.Plugin;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

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
        using StreamWriter writer = new(settings.OutputPath!, false, Encoding.UTF8);
        foreach (UdpChunk chunk in builder.Build(text))
        {
            writer.WriteLine($"-----#{chunk}-----");
            writer.WriteLine(text.Substring(chunk.Range.Start, chunk.Range.Length));
        }
        writer.Flush();

        return Task.FromResult(0);
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
    [DefaultValue(1000)]
    public int MaxLength { get; set; }

    [Description("Blank-fill XML chunk tags before UDP")]
    [CommandOption("-f|--fill")]
    public bool FillChunkTags { get; set; }

    [Description("Exclude matches in the specified CSV list of elements")]
    [CommandOption("-x|--black-tags <BLACK_TAG>")]
    public string? BlackTags { get; set; }
}
