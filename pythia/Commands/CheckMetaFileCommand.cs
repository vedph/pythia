using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class CheckMetaFileCommand :
    AsyncCommand<CheckMetaFileCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context,
        CheckMetaFileCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]CHECK METADATA FILES[/]");
        AnsiConsole.MarkupLine($"Input mask: [cyan]{settings.InputFileMask}[/]");
        AnsiConsole.MarkupLine($"Source find: [cyan]{settings.SourceFind}[/]");
        AnsiConsole.MarkupLine($"Source replace: [cyan]{settings.SourceReplace}[/]");

        int count = 0, missingCount = 0;
        Regex find = new(settings.SourceFind, RegexOptions.Compiled);

        foreach (string filePath in Directory.GetFiles(
            Path.GetDirectoryName(settings.InputFileMask) ?? "",
            Path.GetFileName(settings.InputFileMask)!).OrderBy(s => s))
        {
            AnsiConsole.MarkupLine($"[yellow]{++count:000}[/]: [cyan]{filePath}[/]");

            string metaFilePath = find.Replace(filePath, settings.SourceReplace);
            if (!File.Exists(metaFilePath))
            {
                AnsiConsole.MarkupLine(
                    $"[red]Missing metadata file: {metaFilePath}[/]");
                missingCount++;
            }
        }
        if (missingCount > 0)
            AnsiConsole.MarkupLine($"[red]Missing count: {missingCount}/{count}[/]");
        else
            AnsiConsole.MarkupLine($"[green]Missing count: 0/{count}[/]");

        return Task.FromResult(0);
    }
}

public class CheckMetaFileCommandSettings : CommandSettings
{
    [Description("Input file(s) mask")]
    [CommandArgument(0, "<INPUT_FILES_MASK>")]
    public string InputFileMask { get; set; }

    [Description("The pattern to find in source file path")]
    [CommandOption("-f|--find <REGEX_TO_FIND>")]
    [DefaultValue(@"\.xml$")]
    public string SourceFind { get; set; }

    [Description("The replacement for any pattern matched in source file path")]
    [CommandOption("-r|--replace <TEXT_TO_REPLACE>")]
    [DefaultValue(".xml.meta")]
    public string SourceReplace { get; set; }

    public CheckMetaFileCommandSettings()
    {
        InputFileMask = "*.xml";
        SourceFind = @"\.xml$";
        SourceReplace = ".xml.meta";
    }
}
