using Fusi.DbManager.PgSql;
using Fusi.DbManager;
using Pythia.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Pythia.Cli.Commands;

public sealed class BulkReadTablesCommand :
    AsyncCommand<BulkReadTablesCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context,
        BulkReadTablesCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red underline]BUILD READ TABLES[/]");
        AnsiConsole.MarkupLine($"Input dir: [cyan]{settings.InputDir}[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");

        try
        {
            string cs = string.Format(
        CliAppContext.Configuration!.GetConnectionString("Default")!,
        settings.DbName);

            IBulkTableCopier tableCopier = new PgSqlBulkTableCopier(cs);

            BulkTablesCopier copier = new(tableCopier);
            copier.Begin();
            copier.Read(settings.InputDir!, CancellationToken.None,
                new Progress<string>((s) =>
                {
                    Console.WriteLine(s);
                }));
            copier.End();

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

public class BulkReadTablesCommandSettings : CommandSettings
{
    [Description("Input directory")]
    [CommandArgument(0, "<INPUT_DIR>")]
    public string? InputDir { get; set; }

    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; }

    public BulkReadTablesCommandSettings()
    {
        DbName = "pythia";
    }
}
