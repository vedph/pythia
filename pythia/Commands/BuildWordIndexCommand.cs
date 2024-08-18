using Corpus.Sql;
using Pythia.Cli.Services;
using Pythia.Sql.PgSql;
using Pythia.Sql;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Threading;
using Fusi.Tools;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;

namespace Pythia.Cli.Commands;

internal sealed class BuildWordIndexCommand :
    AsyncCommand<BuildWordIndexCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context,
        BuildWordIndexCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red underline]INDEX WORDS[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");
        if (settings.BinCounts.Length > 0)
        {
            AnsiConsole.MarkupLine(
                $"Bin counts: {string.Join(",", settings.BinCounts)}");
        }
        if (settings.ExcludedDocAttrs.Length > 0)
        {
            AnsiConsole.MarkupLine(
                $"Excluded doc attrs: {string.Join(",", settings.ExcludedDocAttrs)}");
        }

        try
        {
            string cs = string.Format(
                CliAppContext.Configuration!.GetConnectionString("Default")!,
                settings.DbName);

            SqlIndexRepository repository = new PgSqlIndexRepository();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });

            string? prevMessage = null;
            int prevPercent = -1;

            await repository.BuildWordIndexAsync(
                settings.ParseBinCounts(),
                new HashSet<string>(settings.ExcludedDocAttrs),
                CancellationToken.None,
                new Progress<ProgressReport>(report =>
                {
                    prevMessage = report.Message;
                    prevPercent = report.Percent;

                    AnsiConsole.MarkupLine(
                        $"[yellow]{report.Percent:000}[/] " +
                        $"[green]{DateTime.Now:HH:mm:ss}[/] " +
                        $"[cyan]{report.Message}[/]");
                }));

            AnsiConsole.MarkupLine("[green]Completed[/]");
            return 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}

public class BuildWordIndexCommandSettings : CommandSettings
{
    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; } = "pythia";

    [Description("The class counts for document attribute bins (name=N, multiple)")]
    [CommandOption("-c|--class-counts <COUNTS>")]
    [DefaultValue(new string[] { "date_value=3" })]
    public string[] BinCounts { get; set; } = ["date_value=3"];

    [Description("The document attributes to exclude from word index (multiple)")]
    [CommandOption("-x|--exclude <ATTR>")]
    [DefaultValue(new string[] { "date" })]
    public string[] ExcludedDocAttrs { get; set; } = ["date"];

    public Dictionary<string, int> ParseBinCounts()
    {
        Regex r = new(@"^([^=]+)=([0-9]+)$");

        Dictionary<string, int> dct = [];
        foreach (string s in BinCounts)
        {
            Match m = r.Match(s);
            if (m.Success)
            {
                dct[m.Groups[1].Value] = int.Parse(
                    m.Groups[2].Value, CultureInfo.InvariantCulture);
            }
        }
        return dct;
    }
}
