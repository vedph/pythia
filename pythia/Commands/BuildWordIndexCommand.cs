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

namespace Pythia.Cli.Commands;

internal sealed class BuildWordIndexCommand :
    AsyncCommand<BuildWordIndexCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context,
        BuildWordIndexCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red underline]INDEX WORDS[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");

        string cs = string.Format(
            CliAppContext.Configuration!.GetConnectionString("Default")!,
            settings.DbName);

        SqlIndexRepository repository = new PgSqlIndexRepository();
        repository.Configure(new SqlRepositoryOptions
        {
            ConnectionString = cs
        });

        await AnsiConsole.Progress().Start(async ctx =>
        {
            var task = ctx.AddTask("[green]Processing...[/]");
            await repository.BuildWordIndexAsync(
                settings.ParseBinCounts(),
                //new Dictionary<string, int>
                //{
                //    ["date_value"] = 3,
                //    ["date-value"] = 3
                //},
                new HashSet<string>(settings.ExcludedDocAttrs),
                CancellationToken.None,
                new Progress<ProgressReport>(report =>
                {
                    task.Value = report.Percent;
                    task.Description = report.Message ?? "";
                }));
        });

        AnsiConsole.MarkupLine("[green]Completed[/]");
        return 0;
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
