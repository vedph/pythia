using Corpus.Core.Plugin.Analysis;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Microsoft.Extensions.Configuration;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Sql.PgSql;
using SimpleInjector;
using System;

namespace Pythia.Api.Services
{
    /// <summary>
    /// Pythia factory provider.
    /// </summary>
    public sealed class PythiaFactoryProvider
    {
        private readonly string _connString;

        /// <summary>
        /// Initializes a new instance of the <see cref="PythiaFactoryProvider"/>
        /// class.
        /// </summary>
        /// <param name="connString">The connection string.</param>
        /// <exception cref="ArgumentNullException">connString</exception>
        public PythiaFactoryProvider(string connString)
        {
            _connString = connString
                ?? throw new ArgumentNullException(nameof(connString));
        }

        /// <summary>
        /// Gets the factory for the specified profile.
        /// </summary>
        /// <param name="profile">The profile content.</param>
        /// <returns>Factory</returns>
        /// <exception cref="ArgumentNullException">profile</exception>
        public PythiaFactory GetFactory(string profile)
        {
            if (profile is null) throw new ArgumentNullException(nameof(profile));

            Container container = new Container();
            PythiaFactory.ConfigureServices(container,
                // Corpus.Core.Plugin
                typeof(StandardDocSortKeyBuilder).Assembly,
                // Pythia.Core.Plugin
                typeof(StandardTokenizer).Assembly,
                // Pythia.Liz.Plugin
                // typeof(LizHtmlTextRenderer).Assembly,
                // Pythia.Chiron.Plugin
                // typeof(LatSylCountSupplierTokenFilter).Assembly,
                // Pythia.Sql.PgSql
                typeof(PgSqlTextRetriever).Assembly);
            container.Verify();

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddInMemoryJson(profile);
            IConfiguration configuration = builder.Build();

            return new PythiaFactory(container, configuration)
            {
                ConnectionString = _connString
            };
        }
    }
}
