using Fusi.Cli;
using Microsoft.Extensions.CommandLineUtils;
using Pythia.Udp.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

public sealed class DumpUdpChunkCommand : ICommand
{
    private readonly DumpUdpChunkCommandOptions _options;

    public DumpUdpChunkCommand(DumpUdpChunkCommandOptions options)
    {
        _options = options;
    }

    public static void Configure(CommandLineApplication command,
        AppOptions options)
    {
        command.Description = "Dump the UDP chunk builder output " +
            "for the specified file.";
        command.HelpOption("-?|-h|--help");

        CommandArgument inputArgument = command.Argument("[inputPath]",
            "The input file path.");

        CommandArgument outputArgument = command.Argument("[outputPath]",
            "The output file path.");

        CommandOption maxLengthOption = command.Option("--max|-m",
            "The maximum chunk length (default=1000)",
            CommandOptionType.SingleValue);

        CommandOption fillChunkTagsOption = command.Option("--fill-tags|-f",
            "Blank-fill XML chunk tags before UDP", CommandOptionType.NoValue);

        CommandOption blackTagOption = command.Option("--black-tag|-x",
            "Exclude matches in the specified XML element",
            CommandOptionType.MultipleValue);

        command.OnExecute(() =>
        {
            options.Command = new DumpUdpChunkCommand(
                new DumpUdpChunkCommandOptions
                {
                    AppOptions = options,
                    InputPath = inputArgument.Value,
                    OutputPath = outputArgument.Value,
                    MaxLength = maxLengthOption.HasValue() &&
                        int.TryParse(maxLengthOption.Value(), out int n) ?
                        n : 1000,
                    FillChunkTags = fillChunkTagsOption.HasValue(),
                    BlackTags = blackTagOption.HasValue()
                        ? new HashSet<string>(blackTagOption.Values)
                        : null
                });
            return 0;
        });
    }

    public async Task<int> Run()
    {
        ColorConsole.WriteWrappedHeader("Dump UDP Chunks");
        Console.WriteLine($"Input path: {_options.InputPath}\n");
        Console.WriteLine($"Output path: {_options.OutputPath}\n");

        string text;
        using (StreamReader reader = new(_options.InputPath!, Encoding.UTF8))
        {
            text = reader.ReadToEnd();
        }

        UdpChunkBuilder builder = new()
        {
            MaxLength = _options.MaxLength,
            BlackTags = _options.BlackTags,
        };
        using StreamWriter writer = new(_options.OutputPath!, false, Encoding.UTF8);
        foreach (UdpChunk chunk in builder.Build(text))
        {
            writer.WriteLine($"-----#{chunk}-----");
            writer.WriteLine(text.Substring(chunk.Range.Start, chunk.Range.Length));
        }
        writer.Flush();

        return 0;
    }
}

public class DumpUdpChunkCommandOptions
{
    public AppOptions? AppOptions { get; set; }
    public string? InputPath { get; set; }
    public string? OutputPath { get; set; }
    public int MaxLength { get; set; }
    public bool FillChunkTags { get; set; }
    public HashSet<string>? BlackTags { get; set; }
}
