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

                config.AddCommand<BulkExportCommand>("export")
                    .WithDescription("Bulk-export the Pythia database");

                config.AddCommand<DumpMapCommand>("dump-map")
                    .WithDescription("Generate and dump the map " +
                        "for the specified document");

                config.AddCommand<DumpUdpChunkCommand>("dump-chunks")
                    .WithDescription("Dump UDP chunks for the specified document");

                config.AddCommand<IndexCommand>("index")
                    .WithDescription("Index documents from the specified source");

                config.AddCommand<QueryCommand>("query")
                    .WithDescription("Query the database");
            });

            int result = await app.RunAsync(args);

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                Console.WriteLine("\nTime: {0}h{1}'{2}\"",
                    stopwatch.Elapsed.Hours,
                    stopwatch.Elapsed.Minutes,
                    stopwatch.Elapsed.Seconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, ex.Message);
            Debug.WriteLine(ex.ToString());
            AnsiConsole.WriteException(ex);
            return 2;
        }
    }
}
