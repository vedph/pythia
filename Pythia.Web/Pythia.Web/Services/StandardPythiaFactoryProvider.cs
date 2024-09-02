using Corpus.Core.Plugin.Analysis;
using Corpus.Sql.PgSql;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Web.Shared.Services;

namespace Pythia.Web.Services;

/// <summary>
/// "Standard" Pythia factory provider. This uses the core Pythia plugin
/// components, and can be used as a sample implementation to create your
/// own providers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StandardPythiaFactoryProvider"/>
/// class.
/// </remarks>
/// <param name="connString">The connection string.</param>
/// <exception cref="ArgumentNullException">connString</exception>
public sealed class StandardPythiaFactoryProvider(string connString) :
    IPythiaFactoryProvider
{
    private readonly string _connString = connString
            ?? throw new ArgumentNullException(nameof(connString));
    private readonly Dictionary<int, PythiaFactory> _factories = [];

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

        int hash = profile.GetHashCode();
        if (!_factories.ContainsKey(hash))
        {
            _factories[hash] = new PythiaFactory(GetHost(profile))
            {
                ConnectionString = _connString
            };
        }
        return _factories[hash];
    }
}
