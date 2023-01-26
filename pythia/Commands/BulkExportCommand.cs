using Fusi.DbManager.PgSql;
using Fusi.DbManager;
using Pythia.Cli.Services;
using Spectre.Console.Cli;
using Spectre.Console;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Pythia.Cli.Commands;

internal sealed class BulkExportCommand : AsyncCommand<BulkExportCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context,
        BulkExportCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]BULK DATA EXPORT[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");
        AnsiConsole.MarkupLine($"Target dir: [cyan]{settings.TargetDir}[/]");

        if (!Directory.Exists(settings.TargetDir))
            Directory.CreateDirectory(settings.TargetDir);

        string cs = string.Format(
            CliAppContext.Configuration!.GetConnectionString("Default")!,
            settings.DbName);

        IBulkTableCopier tableCopier = new PgSqlBulkTableCopier(cs);
        BulkTablesCopier copier = new(tableCopier);
        copier.Write(new[]
            {
                "app_role", "app_role_claim", "app_user", "app_user_claim",
                "app_user_login", "app_user_role", "app_user_token",
                "profile", "document", "document_attribute",
                "corpus", "document_corpus", "structure", "structure_attribute",
                "document_structure", "token", "occurrence", "occurrence_attribute",
                "token_occurrence_count"
            },
            settings.TargetDir,
            CancellationToken.None,
            new Progress<string>(Console.WriteLine));

        return Task.FromResult(0);
    }
}

internal class BulkExportCommandSettings : CommandSettings
{
    [CommandArgument(0, "<TARGET_DIR>")]
    public string TargetDir { get; set; }

    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; }

    public BulkExportCommandSettings()
    {
        TargetDir = "";
        DbName = "pythia";
    }
}
