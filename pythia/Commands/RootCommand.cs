using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

public sealed class RootCommand : ICommand
{
    private readonly CommandLineApplication _app;

    public RootCommand(CommandLineApplication app)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
    }

    public static void Configure(CommandLineApplication app, AppOptions options)
    {
        // configure all the app commands here
        app.Command("create-db", c => CreateDbCommand.Configure(c, options));
        app.Command("add-profiles", c => AddProfilesCommand.Configure(c, options));
        app.Command("index", c => IndexCommand.Configure(c, options));
        app.Command("build-sql", c => BuildSqlCommand.Configure(c, options));
        app.Command("query", c => QueryCommand.Configure(c, options));
        app.Command("cache-tokens", c => CacheTokensCommand.Configure(c, options));
        app.Command("dump-map", c => DumpMapCommand.Configure(c, options));
        app.Command("dump-udpc", c => DumpUdpChunkCommand.Configure(c, options));

        app.OnExecute(() =>
        {
            options.Command = new RootCommand(app);
            return 0;
        });
    }

    public Task<int> Run()
    {
        _app.ShowHelp();
        return Task.FromResult(0);
    }
}
