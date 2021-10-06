using Fusi.Cli;
using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Pythia.Sql.PgSql;
using System;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands
{
    public sealed class CreateDbCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _dbName;
        private readonly bool _clear;

        public CreateDbCommand(AppOptions options, string dbName, bool clear)
        {
            _config = options.Configuration;
            _dbName = dbName;
            _clear = clear;
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            command.Description = "Create or clear the Pythia database " +
                "with the specified name.";
            command.HelpOption("-?|-h|--help");

            CommandArgument dbNameArgument = command.Argument("[dbName]",
                "The database name");

            CommandOption clearOption = command.Option("-c|--clear",
                "Clear the database if it exists.", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new CreateDbCommand(options,
                    dbNameArgument.Value,
                    clearOption.HasValue());
                return 0;
            });
        }

        public Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Create Pythia Database");
            IDbManager manager =
                new PgSqlDbManager(_config.GetConnectionString("Default"));
            if (manager.Exists(_dbName))
            {
                if (_clear)
                {
                    Console.WriteLine("Clearing database " + _dbName);
                    manager.ClearDatabase(_dbName);
                }
            }
            else
            {
                Console.WriteLine("Creating database " + _dbName);
                //string cs = string.Format(
                //    _config.GetConnectionString("Default"),
                //    _dbName);

                manager.CreateDatabase(_dbName,
                    new PgSqlIndexRepository().GetSchema(),
                    null);
            }

            ColorConsole.WriteSuccess("Completed");
            return Task.FromResult(0);
        }
    }
}
