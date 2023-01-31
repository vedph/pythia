using Corpus.Core.Plugin.Analysis;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Microsoft.Extensions.Hosting;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Sql.PgSql;
using System;

namespace Pythia.Api.Services
{
    /// <summary>
    /// "Standard" Pythia factory provider for processing queries with literal
    /// filters. This uses the core Pythia plugin components, and can be used
    /// as a sample implementation to create your own providers. It is essentially
    /// equal to the <see cref="IPythiaFactoryProvider"/>, except for the fact
    /// that it always uses the same profile.
    /// </summary>
    public sealed class StandardQueryPythiaFactoryProvider :
        IQueryPythiaFactoryProvider
    {
        private readonly string _profile;
        private readonly string _connString;
        private PythiaFactory? _factory;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="StandardQueryPythiaFactoryProvider"/>
        /// class.
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <param name="connString">The connection string.</param>
        /// <exception cref="ArgumentNullException">profile or connString</exception>
        public StandardQueryPythiaFactoryProvider(string profile, string connString)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _connString = connString
                ?? throw new ArgumentNullException(nameof(connString));
        }

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
                        // Pythia.Sql.PgSql
                        typeof(PgSqlTextRetriever).Assembly
                    });
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
            if (_factory == null)
            {
                _factory = new PythiaFactory(GetHost(_profile))
                {
                    ConnectionString = _connString
                };
            }
            return _factory;
        }
    }
}
