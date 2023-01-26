using Corpus.Sql;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Sql;
using Pythia.Sql.PgSql;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

internal sealed class AddProfilesCommand : AsyncCommand<AddProfilesCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context,
        AddProfilesCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red underline]ADD PROFILES[/]");
        SqlIndexRepository? repository = null;

        if (!settings.IsDry)
        {
            string cs = string.Format(
                CliAppContext.Configuration!.GetConnectionString("Default")!,
                settings.DbName);
            repository = new PgSqlIndexRepository();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });
        }

        // IDs if any
        List<string> presetIds = new();
        if (!string.IsNullOrEmpty(settings.ProfileId))
        {
            presetIds.AddRange(settings.ProfileId.Split(','));
        }

        int count = 0, index = 0;
        foreach (string filePath in Directory.GetFiles(
            Path.GetDirectoryName(settings.InputFileMask) ?? "",
            Path.GetFileName(settings.InputFileMask)!).OrderBy(s => s))
        {
            AnsiConsole.MarkupLine($"[yellow]{++count:000}[/]: [cyan]{filePath}[/]");

            using Stream input = new FileStream(filePath, FileMode.Open,
                FileAccess.Read, FileShare.Read);
            JsonDocument doc = JsonDocument.Parse(input);

            string id;
            if (index < presetIds.Count && presetIds[index].Length > 0)
                id = presetIds[index];
            else
                id = Path.GetFileNameWithoutExtension(filePath);

            AnsiConsole.MarkupLine($"  - [green]{id}[/]");

            if (!settings.IsDry)
            {
                string json;
                using (MemoryStream stream = new())
                {
                    Utf8JsonWriter writer = new(stream,
                        new JsonWriterOptions { Indented = false });
                    doc.WriteTo(writer);
                    writer.Flush();
                    json = Encoding.UTF8.GetString(stream.ToArray());
                }

                repository!.AddProfile(new Corpus.Core.Profile
                {
                    Id = id,
                    Content = json
                });
            }
            index++;
        }

        AnsiConsole.MarkupLine("[green]Completed[/]");
        return Task.FromResult(0);
    }
}

internal class AddProfilesCommandSettings : CommandSettings
{
    [Description("Input file(s) mask")]
    [CommandArgument(0, "<INPUT_FILES_MASK>")]
    public string? InputFileMask { get; set; }

    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; }

    [Description("Preflight mode: do not write to database")]
    [CommandOption("-p|--preflight|--dry")]
    public bool IsDry { get; set; }

    [Description("The profile ID(s) to assign to the profile added (CSV)")]
    [CommandOption("-i|--ids <CSV_IDS>")]
    public string? ProfileId { get; set; }

    public AddProfilesCommandSettings()
    {
        DbName = "pythia";
    }
}
