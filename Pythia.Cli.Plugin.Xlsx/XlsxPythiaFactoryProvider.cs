using Corpus.Core.Plugin.Analysis;
using Fusi.Tools.Configuration;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Pythia.Cli.Core;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Sql.PgSql;
using Pythia.Xlsx.Plugin;
using System;
using Microsoft.Extensions.Hosting;

namespace Pythia.Cli.Plugin.Xlsx
{
    /// <summary>
    /// Excel Pythia factory provider. This adds Pythia.Xlsx.Plugin components
    /// to the standard factory provider.
    /// Tag: <c>factory-provider.xlsx</c>.
    /// </summary>
    /// <seealso cref="ICliPythiaFactoryProvider" />
    [Tag("factory-provider.xlsx")]
    public sealed class XlsxPythiaFactoryProvider : ICliPythiaFactoryProvider
    {
        private static IHost GetHost(string config)
        {
            return new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    PythiaFactory.ConfigureServices(services, new[]
                    {
                        // Corpus.Core.Plugin
                        typeof(StandardDocSortKeyBuilder).Assembly,
                        // Pythia.Core.Plugin
                        typeof(StandardTokenizer).Assembly,
                        // Pythia.Xlsx.Plugin
                        typeof(FsExcelAttributeParser).Assembly,
                        // Pythia.Sql.PgSql
                        typeof(PgSqlTextRetriever).Assembly
                    });
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
}
