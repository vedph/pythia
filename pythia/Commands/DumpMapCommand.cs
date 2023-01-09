using Corpus.Core;
using Corpus.Core.Reading;
using Corpus.Sql;
using Fusi.Cli;
using Fusi.Cli.Commands;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Core.Config;
using Pythia.Sql;
using Pythia.Sql.PgSql;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class DumpMapCommand : ICommand
{
    private readonly DumpMapCommandOptions _options;

    private DumpMapCommand(DumpMapCommandOptions options)
    {
        _options = options;
    }

    public static void Configure(CommandLineApplication app,
        ICliAppContext context)
    {
        app.Description = "Generate and dump the map " +
            "for the specified document.";
        app.HelpOption("-?|-h|--help");

        CommandArgument sourceArgument = app.Argument("[source]",
            "The text source.");

        CommandArgument dbNameArgument = app.Argument("[dbName]",
            "The database name.");

        CommandArgument profileIdArgument = app.Argument("[profileId]",
            "The profile ID.");

        CommandArgument outputArgument = app.Argument("[outputPath]",
            "The output file path.");

        CommandOption pluginTagOption = app.Option("-t|--tag",
            "The factory provider plugin tag.", CommandOptionType.SingleValue);

        app.OnExecute(() =>
        {
            context.Command = new DumpMapCommand(
                new DumpMapCommandOptions(context)
                {
                    Source = sourceArgument.Value,
                    DbName = dbNameArgument.Value,
                    ProfileId = profileIdArgument.Value,
                    OutputPath = outputArgument.Value,
                    PluginTag = pluginTagOption.Value()
                        ?? PythiaCliAppContext.DEFAULT_PLUGIN_TAG
                });
            return 0;
        });
    }

    private static string DumpText(string text)
    {
        StringBuilder sb = new(text);
        sb.Replace("\r", "\\r");
        sb.Replace("\n", "\\n");
        sb.Replace("\t", "\\t");
        return sb.ToString();
    }

    public async Task<int> Run()
    {
        ColorConsole.WriteWrappedHeader("Dump Map");
        Console.WriteLine($"Plugin tag: {_options.PluginTag}\n");

        string cs = string.Format(
            _options.Context!.Configuration!.GetConnectionString("Default")!,
            _options.DbName);
        SqlIndexRepository repository = new PgSqlIndexRepository();
        repository.Configure(new SqlRepositoryOptions
        {
            ConnectionString = cs
        });

        IProfile? profile = repository.GetProfile(_options.ProfileId!);
        if (profile == null)
        {
            throw new ArgumentException("Profile ID not found: " +
                _options.ProfileId);
        }

        var factoryProvider = PluginPythiaFactoryProvider.GetFromTag
            (_options.PluginTag!);
        if (factoryProvider == null)
        {
            throw new FileNotFoundException(
                $"The requested tag {_options.PluginTag} was not found " +
                "among plugins in " +
                PluginPythiaFactoryProvider.GetPluginsDir());
        }
        PythiaFactory factory = factoryProvider.GetFactory(
            profile.Id!, profile.Content!, cs);

        // 1. retrieve text
        Console.WriteLine("Retrieving text...");
        ITextRetriever retriever = factory.GetTextRetriever()!;
        string? text = await retriever.GetAsync(new Document
        {
            Source = _options.Source
        });
        if (text == null) return 2;

        // 2. map text
        Console.WriteLine("Mapping text...");
        ITextMapper mapper = factory.GetTextMapper()!;
        TextMapNode map = mapper.Map(text, null)!;

        // 3. dump map
        Console.WriteLine("Dumping map...");
        string outDir = Path.GetDirectoryName(_options.OutputPath) ?? "";
        if (outDir.Length > 0 && !Directory.Exists(outDir))
            Directory.CreateDirectory(outDir);

        using (StreamWriter writer = File.CreateText(_options.OutputPath!))
        {
            writer.WriteLine("#Tree");
            writer.WriteLine($"Length (chars): {text.Length}");
            writer.WriteLine(map.DumpTree());

            map.Visit(node =>
            {
                writer.WriteLine($"#{node.Label}: {node.Location}");
                writer.WriteLine($"{node.StartIndex}-{node.EndIndex}");

                string sStart = text.Substring(node.StartIndex,
                    node.StartIndex + 100 > node.EndIndex ?
                    node.EndIndex - node.StartIndex : 100);
                writer.WriteLine($"From: {DumpText(sStart)} ...");

                int i = node.EndIndex - 100 < node.StartIndex ?
                    node.StartIndex : node.EndIndex - 100;
                string end = text[i..node.EndIndex];
                writer.WriteLine($"To: ... {DumpText(end)}");

                writer.WriteLine();

                return true;
            });

            writer.Flush();
        }
        Console.WriteLine("Completed.");
        return 0;
    }
}

public class DumpMapCommandOptions : CommandOptions<PythiaCliAppContext>
{
    public DumpMapCommandOptions(ICliAppContext options)
    : base((PythiaCliAppContext)options)
    {
    }

    public string? Source { get; set; }
    public string? DbName { get; set; }
    public string? ProfileId { get; set; }
    public string? OutputPath { get; set; }
    public string? PluginTag { get; set; }
}
