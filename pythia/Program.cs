using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Pythia.Cli.Commands;
using Spectre.Console.Cli;
using Spectre.Console;

namespace Pythia.Cli;

public static class Program
{
#if DEBUG
    private static void DeleteLogs()
    {
        foreach (var path in Directory.EnumerateFiles(
            AppDomain.CurrentDomain.BaseDirectory, "pythia-log*.txt"))
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
#endif

    public static async Task<int> Main(string[] args)
    {
        try
        {
            // https://github.com/serilog/serilog-sinks-file
            string logFilePath = Path.Combine(
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location) ?? "",
                    "pythia-log.txt");
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .Enrich.FromLogContext()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
#if DEBUG
            DeleteLogs();
#endif
            Stopwatch stopwatch = new();
            stopwatch.Start();

            CommandApp app = new();
            app.Configure(config =>
            {
                config.AddCommand<AddProfilesCommand>("add-profiles")
                    .WithDescription(
                    "Add profile(s) from JSON files to the Pythia database");

                config.AddCommand<BuildSqlCommand>("build-sql")
                    .WithDescription("Build SQL code from queries");

                config.AddCommand<CacheTokensCommand>("cache-tokens")
                    .WithDescription("Cache the tokens got from tokenizing " +
                    "the texts from the specified source.");

                config.AddCommand<CreateDbCommand>("create-db")
                    .WithDescription("Create or clear the Pythia database");

                config.AddCommand<DumpDocPairsCommand>("dump-pairs")
                    .WithDescription("Dump document pairs for word index");

                config.AddCommand<DumpMapCommand>("dump-map")
                    .WithDescription("Generate and dump the map " +
                        "for the specified document");

                config.AddCommand<DumpUdpChunkCommand>("dump-chunks")
                    .WithDescription("Dump UDP chunks for the specified document");

                config.AddCommand<DumpSpanCommand>("dump-spans")
                    .WithDescription(
                    "Dump text spans in console or CSV");

                config.AddCommand<IndexCommand>("index")
                    .WithDescription("Index documents from the specified source");

                config.AddCommand<BuildWordIndexCommand>("index-w")
                    .WithDescription("Build words index from tokens");

                config.AddCommand<QueryCommand>("query")
                    .WithDescription("Query the database");

                config.AddCommand<BulkWriteTablesCommand>("bulk-write")
                    .WithDescription("Bulk-write all the Pythia database " +
                    "tables into files");

                config.AddCommand<BulkReadTablesCommand>("bulk-read")
                    .WithDescription("Bulk-read all the Pythia database " +
                    "tables from files");

                config.AddCommand<CheckMetaFileCommand>("check-meta")
                    .WithDescription("Check that each source file for indexing " +
                    "has its companion meta file");

                config.AddCommand<ExportSearchCommand>("export-search")
                    .WithDescription(
                    "Export into CSV the results of the specified search");

                config.AddCommand<ConvertMorphitCommand>("convert-morphit")
                    .WithDescription(
                    "Convert MorphIt! TSV file into a lookup index");
            });

            int result = await app.RunAsync(args);

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                AnsiConsole.WriteLine("\nTime: {0}d{1}h{2}'{3}\"",
                    stopwatch.Elapsed.Days,
                    stopwatch.Elapsed.Hours,
                    stopwatch.Elapsed.Minutes,
                    stopwatch.Elapsed.Seconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, ex.ToString());
            Debug.WriteLine(ex.ToString());
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}
