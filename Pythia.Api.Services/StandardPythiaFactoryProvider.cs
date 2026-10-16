using Corpus.Core.Plugin.Analysis;
using Corpus.Sql.PgSql;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Microsoft.Extensions.Hosting;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using System;
using System.Collections.Concurrent;

namespace Pythia.Api.Services;

/// <summary>
/// "Standard" Pythia factory provider. This uses the core Pythia plugin
/// components, and can be used as a sample implementation to create your
/// own providers.
/// </summary>
public sealed class StandardPythiaFactoryProvider : IPythiaFactoryProvider
{
    private readonly string _connString;
    // this provider is a singleton shared by concurrent requests, and
    // factories are keyed by the full profile content (a hash code might
    // collide, returning the factory of another profile)
    private readonly ConcurrentDictionary<string, Lazy<PythiaFactory>>
        _factories;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardPythiaFactoryProvider"/>
    /// class.
    /// </summary>
    /// <param name="connString">The connection string.</param>
    /// <exception cref="ArgumentNullException">connString</exception>
    public StandardPythiaFactoryProvider(string connString)
    {
        _connString = connString
            ?? throw new ArgumentNullException(nameof(connString));
        _factories = new();
    }

    private static IHost GetHost(string config)
    {
        return new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                PythiaFactory.ConfigureServices(services,
                    // Corpus.Core.Plugin
                    typeof(StandardDocSortKeyBuilder).Assembly,
                    // Pythia.Core.Plugin
                    typeof(StandardTokenizer).Assembly,
                    // Pythia.Sql.PgSql
                    typeof(PgSqlTextRetriever).Assembly
                );
            })
            .AddInMemoryJson(config)
            .Build();
    }

    /// <summary>
    /// Gets the factory for the specified profile.
    /// </summary>
    /// <param name="profile">The profile content.</param>
    /// <returns>Factory</returns>
    /// <exception cref="ArgumentNullException">profile</exception>
    public PythiaFactory GetFactory(string profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return _factories.GetOrAdd(profile, p => new Lazy<PythiaFactory>(
            () => new PythiaFactory(GetHost(p))
            {
                ConnectionString = _connString
            })).Value;
    }
}
