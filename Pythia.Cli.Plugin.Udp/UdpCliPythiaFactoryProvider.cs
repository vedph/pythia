using Corpus.Core.Plugin.Analysis;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Fusi.Tools.Configuration;
using Microsoft.Extensions.Hosting;
using Pythia.Cli.Core;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Sql.PgSql;
using Pythia.Udp.Plugin;
using System;

namespace Pythia.Cli.Plugin.Udp;

/// <summary>
/// UDP Pythia factory provider.
/// Tag: <c>factory-provider.udp</c>.
/// </summary>
/// <seealso cref="ICliPythiaFactoryProvider" />
[Tag("factory-provider.udp")]
public class UdpCliPythiaFactoryProvider : ICliPythiaFactoryProvider
{
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
                    // Pythia.Udp.Plugin
                    typeof(UdpTextFilter).Assembly,
                    // Pythia.Sql.PgSql
                    typeof(PgSqlTextRetriever).Assembly);
            })
            // extension method from Fusi library
            .AddInMemoryJson(config)
            .Build();
    }

    /// <summary>
    /// Creates and configures a new instance of the PythiaFactory using the
    /// specified profile and connection string.
    /// </summary>
    /// <param name="profileId">The unique identifier for the profile.</param>
    /// <param name="profile">The name of the profile to use when creating the
    /// factory.</param>
    /// <param name="connString">The connection string to use for establishing
    /// a connection.</param>
    /// <returns>A new PythiaFactory instance configured with the specified
    /// profile and connection string.</returns>
    public PythiaFactory GetFactory(string profileId, string profile,
        string connString)
    {
        ArgumentNullException.ThrowIfNull(profileId);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(connString);

        return new PythiaFactory(GetHost(profile))
        {
            ConnectionString = connString
        };
    }
}
