using Corpus.Core.Plugin.Analysis;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Sql.PgSql;
using Pythia.Web.Shared.Services;

namespace Pythia.Web.Services;

/// <summary>
/// "Standard" Pythia factory provider for processing queries with literal
/// filters. This uses the core Pythia plugin components, and can be used
/// as a sample implementation to create your own providers. It is essentially
/// equal to the <see cref="IPythiaFactoryProvider"/>, except for the fact
/// that it always uses the same profile.
/// </summary>
/// <remarks>
/// Initializes a new instance of the
/// <see cref="StandardQueryPythiaFactoryProvider"/>
/// class.
/// </remarks>
/// <param name="profile">The profile.</param>
/// <param name="connString">The connection string.</param>
/// <exception cref="ArgumentNullException">profile or connString</exception>
public sealed class StandardQueryPythiaFactoryProvider(
    string profile, string connString) :
    IQueryPythiaFactoryProvider
{
    private readonly string _profile =
        profile ?? throw new ArgumentNullException(nameof(profile));
    private readonly string _connString = connString
            ?? throw new ArgumentNullException(nameof(connString));

    private PythiaFactory? _factory;

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
    /// Gets the factory.
    /// </summary>
    /// <returns>Factory</returns>
    /// <exception cref="ArgumentNullException">profileId or profile
    /// or connString</exception>
    public PythiaFactory GetFactory()
    {
        _factory ??= new PythiaFactory(GetHost(_profile))
            {
                ConnectionString = _connString
            };
        return _factory;
    }
}
