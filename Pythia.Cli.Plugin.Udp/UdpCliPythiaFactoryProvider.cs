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

    public PythiaFactory GetFactory(string profileId, string profile,
        string connString)
    {
        if (profileId == null)
            throw new ArgumentNullException(nameof(profileId));
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
        if (connString == null)
            throw new ArgumentNullException(nameof(connString));

        return new PythiaFactory(GetHost(profile))
        {
            ConnectionString = connString
        };
    }
}
