using Microsoft.Extensions.CommandLineUtils;
using System;
using Serilog.Extensions.Logging;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Commands;

namespace Pythia.Cli;

public class AppOptions
{
    public ICommand? Command { get; set; }
    public IConfiguration? Configuration { get; private set; }
    public ILogger? Logger { get; private set; }

    public const string DEFAULT_PLUGIN_TAG = "factory-provider.standard";

    public AppOptions()
    {
        BuildConfiguration();
    }

    private void BuildConfiguration()
    {
        ConfigurationBuilder cb = new();
        Configuration = cb
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        Logger = new SerilogLoggerProvider(Serilog.Log.Logger)
            .CreateLogger(nameof(Program));
    }

    public static AppOptions? Parse(string[] args)
    {
        if (args == null) throw new ArgumentNullException(nameof(args));

        AppOptions options = new();
        CommandLineApplication app = new()
        {
            Name = "Pythia CLI",
            FullName = "Pythia command line interface (PgSql) - "
                + Assembly.GetEntryAssembly()!.GetName().Version
        };
        app.HelpOption("-?|-h|--help");

        // app-level options
        RootCommand.Configure(app, options);

        int result = app.Execute(args);
        return result != 0 ? null : options;
    }
}
