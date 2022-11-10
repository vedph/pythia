using Corpus.Sql;
using Fusi.Cli;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Core;
using Pythia.Sql;
using Pythia.Sql.PgSql;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands
{
    public sealed class AddProfilesCommand : ICommand
    {
        private readonly AddProfilesCommandOptions _options;

        public AddProfilesCommand(AddProfilesCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            command.Description =
                "Add profile(s) from JSON files to the Pythia database " +
                "with the specified name.";
            command.HelpOption("-?|-h|--help");

            CommandArgument inputFileMaskArgument = command.Argument("[inputFileMask]",
                "The input file(s) mask.");

            CommandArgument dbNameArgument = command.Argument("[dbName]",
                "The database name.");

            CommandOption indentOption = command.Option("-i|--indent",
                "Write indented JSON.", CommandOptionType.NoValue);

            CommandOption dryOption = command.Option("-d|--dry",
                "Dry run: do not write to database.", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new AddProfilesCommand(
                    new AddProfilesCommandOptions
                    {
                        AppOptions = options,
                        InputFileMask = inputFileMaskArgument.Value,
                        DbName = dbNameArgument.Value,
                        IsIndented = indentOption.HasValue(),
                        IsDry = dryOption.HasValue()
                    });
                return 0;
            });
        }

        public Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Add Profiles");
            SqlIndexRepository? repository = null;

            if (!_options.IsDry)
            {
                string cs = string.Format(
                    _options.AppOptions!.Configuration!.GetConnectionString("Default")!,
                    _options.DbName);
                repository = new PgSqlIndexRepository();
                repository.Configure(new SqlRepositoryOptions
                {
                    ConnectionString = cs
                });
            }

            int count = 0;
            foreach (string filePath in Directory.GetFiles(
                Path.GetDirectoryName(_options.InputFileMask) ?? "",
                Path.GetFileName(_options.InputFileMask)!).OrderBy(s => s))
            {
                Console.WriteLine($"{++count:000}: " + filePath);
                using Stream input = new FileStream(filePath, FileMode.Open,
                    FileAccess.Read, FileShare.Read);
                JsonDocument doc = JsonDocument.Parse(input);
                string id = Path.GetFileNameWithoutExtension(filePath);

                if (!_options.IsDry)
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
            }

            ColorConsole.WriteSuccess("Completed");
            return Task.FromResult(0);
        }
    }

    public class AddProfilesCommandOptions
    {
        public AppOptions? AppOptions { get; set; }
        public string? InputFileMask { get; set; }
        public string? DbName { get; set; }
        public bool IsDry { get; set; }
        public bool IsIndented { get; set; }
    }
}
