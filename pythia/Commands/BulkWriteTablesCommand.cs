using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

public sealed class BulkWriteTablesCommand :
    AsyncCommand<BulkWriteTablesCommandSettings>
{
    private static readonly string[] PYTHIA_TABLES =
    [
        "app_role", "app_role_claim", "app_user", "app_user_claim",
        "app_user_login", "app_user_role", "app_user_token",
        "profile", "document", "document_attribute", "corpus",
        "document_corpus", "span", "span_attribute",
        "word", "lemma", "word_count", "lemma_count"
    ];

    public override Task<int> ExecuteAsync(CommandContext context,
        BulkWriteTablesCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]BUILD WRITE TABLES[/]");
        AnsiConsole.MarkupLine($"Output dir: [cyan]{settings.OutputDir}[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");

        try
        {
            string dir = settings.OutputDir ?? "";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string cs = string.Format(
                CliAppContext.Configuration!.GetConnectionString("Default")!,
                settings.DbName);

            IBulkTableCopier tableCopier = new PgSqlBulkTableCopier(cs);

            BulkTablesCopier copier = new(tableCopier);
            copier.Write(PYTHIA_TABLES, dir, CancellationToken.None,
                new Progress<string>((s) => Console.WriteLine(s)));

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

public class BulkWriteTablesCommandSettings : CommandSettings
{
    [Description("Output directory")]
    [CommandArgument(0, "<OUTPUT_DIR>")]
    public string? OutputDir { get; set; }

    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; }

    public BulkWriteTablesCommandSettings()
    {
        DbName = "pythia";
    }
}