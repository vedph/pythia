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
                // TODO get from args
                new Dictionary<string, int>
                {
                    ["date_value"] = 3,
                    ["date-value"] = 3
                },
                CancellationToken.None,
                new Progress<ProgressReport>(report =>
                {
                    task.Value = report.Percent;
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
}
