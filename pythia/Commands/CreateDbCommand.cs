using Fusi.Cli;
using Fusi.Cli.Commands;
using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Sql.PgSql;
using System;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

public sealed class CreateDbCommand : ICommand
{
    private readonly CreateDbCommandOptions _options;

    private CreateDbCommand(CreateDbCommandOptions options)
    {
        _options = options;
    }

    public static void Configure(CommandLineApplication app,
        ICliAppContext context)
    {
        app.Description = "Create or clear the Pythia database " +
            "with the specified name.";
        app.HelpOption("-?|-h|--help");

        CommandArgument dbNameArgument = app.Argument("[dbName]",
            "The database name");

        CommandOption clearOption = app.Option("-c|--clear",
            "Clear the database if it exists.", CommandOptionType.NoValue);

        app.OnExecute(() =>
        {
            context.Command = new CreateDbCommand(new CreateDbCommandOptions(context)
            {
                Name = dbNameArgument.Value,
                IsClearEnabled = clearOption.HasValue()
            });
            return 0;
        });
    }

    public Task<int> Run()
    {
        ColorConsole.WriteWrappedHeader("Create Pythia Database");
        IDbManager manager = new PgSqlDbManager(_options.Context!
            .Configuration!.GetConnectionString("Default")!);
        if (manager.Exists(_options.Name))
        {
            if (_options.IsClearEnabled)
            {
                Console.WriteLine("Clearing database " + _options.Name);
                manager.ClearDatabase(_options.Name);
            }
        }
        else
        {
            Console.WriteLine("Creating database " + _options.Name);

            manager.CreateDatabase(_options.Name,
                new PgSqlIndexRepository().GetSchema(),
                null);
        }

        ColorConsole.WriteSuccess("Completed");
        return Task.FromResult(0);
    }
}

internal class CreateDbCommandOptions :
    CommandOptions<PythiaCliAppContext>
{
    public string Name { get; set; }
    public bool IsClearEnabled { get; set; }

    public CreateDbCommandOptions(ICliAppContext options)
        : base((PythiaCliAppContext)options)
    {
        Name = "pythia";
    }
}
