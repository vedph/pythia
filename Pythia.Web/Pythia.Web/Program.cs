using Corpus.Core;
using Corpus.Sql;
using Pythia.Core;
using Pythia.Sql.PgSql;
using Pythia.Web.Components;
using Pythia.Web.Services;
using Pythia.Web.Shared.Services;
using Radzen;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Extensions.Logging;

namespace Pythia.Web;

public static class Program
{
    private static void ConfigurePythiaServices(WebApplicationBuilder builder)
    {
        // corpus repository
        string cs = string.Format(
            builder.Configuration.GetConnectionString("Default")!,
            builder.Configuration.GetValue<string>("DatabaseName"));
        builder.Services.AddScoped<ICorpusRepository>(_ =>
        {
            PgSqlIndexRepository repository = new();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });
            return repository;
        });

        // index repository
        builder.Services.AddScoped<IIndexRepository>(_ =>
        {
            PgSqlIndexRepository repository = new();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });
            return repository;
        });

        // query pythia factory provider
        builder.Services.AddSingleton<IQueryPythiaFactoryProvider>(_ =>
        {
            // the "query" profile is reserved for literal filters, if any
            // IIndexRepository repository = new PgSqlIndexRepository(cs)
            PgSqlIndexRepository repository = new();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });
            string profile = repository.GetProfile("query")?.Content ?? "{}";
            return new StandardQueryPythiaFactoryProvider(profile, cs);
        });

        // pythia factory provider
        builder.Services.AddSingleton<IPythiaFactoryProvider>(
            _ => new StandardPythiaFactoryProvider(cs));
    }

    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // add configuration sources
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        // add services to the container
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        // Serilog
        // Install-Package Serilog.Exceptions Serilog.Sinks.MongoDB
        // https://github.com/RehanSaeed/Serilog.Exceptions
        // string maxSize = Configuration["Serilog:MaxMbSize"]
        Serilog.Core.Logger serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.WithExceptionDetails()
            .WriteTo.Console()
            .CreateLogger();
        Microsoft.Extensions.Logging.ILogger msLogger =
            new SerilogLoggerFactory(serilogLogger).CreateLogger("");
        builder.Services.AddSingleton<Microsoft.Extensions.Logging.ILogger>(
            msLogger);

        // Radzen
        builder.Services.AddRadzenComponents();

        // Pythia
        ConfigurePythiaServices(builder);

        WebApplication app = builder.Build();

        // configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

        app.Run();
    }
}
