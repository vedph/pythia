using Fusi.DbManager.PgSql;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Sql.PgSql;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class CreateDbCommand : AsyncCommand<CreateDbCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context,
        CreateDbCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[underline red]CREATE DATABASE[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");
        AnsiConsole.MarkupLine($"Clear: [cyan]{settings.IsClearEnabled}[/]");

        try
        {
            PgSqlDbManager manager = new(CliAppContext.Configuration!
        .GetConnectionString("Default")!);

            AnsiConsole.Status().Start("Processing...", ctx =>
            {
                if (manager.Exists(settings.DbName))
                {
                    if (settings.IsClearEnabled)
                    {
                        ctx.Status("Clearing database");
                        ctx.Spinner(Spinner.Known.Star);
                        manager.ClearDatabase(settings.DbName);
                    }
                }
                else
                {
                    ctx.Status("Creating database");
                    ctx.Spinner(Spinner.Known.Star);
                    manager.CreateDatabase(settings.DbName,
                        new PgSqlIndexRepository().GetSchema(),
                        null);
                }
            });
            AnsiConsole.MarkupLine("[green]Completed[/]");

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

internal class CreateDbCommandSettings : CommandSettings
{
    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; }

    [Description("Clear database if exists")]
    [CommandOption("-c|--clear")]
    public bool IsClearEnabled { get; set; }

    public CreateDbCommandSettings()
    {
        DbName = "pythia";
    }
}
