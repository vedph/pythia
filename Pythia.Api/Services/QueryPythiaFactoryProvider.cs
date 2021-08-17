﻿using Corpus.Core.Plugin.Analysis;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Microsoft.Extensions.Configuration;
using Pythia.Chiron.Plugin;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Liz.Plugin;
using Pythia.Sql.PgSql;
using SimpleInjector;
using System;

namespace Pythia.Api.Services
{
    /// <summary>
    /// Pythia factory provider for processing queries with literal filters.
    /// </summary>
    public sealed class QueryPythiaFactoryProvider
    {
        private readonly string _profile;
        private readonly string _connString;
        private PythiaFactory _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryPythiaFactoryProvider"/>
        /// class.
        /// </summary>
        /// <param name="profile">The profile content.</param>
        /// <param name="connString">The connection string.</param>
        /// <exception cref="ArgumentNullException">profile or connString</exception>
        public QueryPythiaFactoryProvider(string profile, string connString)
        {
            _profile = profile
                ?? throw new ArgumentNullException(nameof(profile));
            _connString = connString
                ?? throw new ArgumentNullException(nameof(connString));
        }

        /// <summary>
        /// Gets the factory.
        /// </summary>
        /// <returns>Factory</returns>
        /// <exception cref="ArgumentNullException">profileId or profile
        /// or connString</exception>
        public PythiaFactory GetFactory()
        {
            if (_factory != null) return _factory;

            Container container = new Container();
            PythiaFactory.ConfigureServices(container,
                // Corpus.Core.Plugin
                typeof(StandardDocSortKeyBuilder).Assembly,
                // Pythia.Core.Plugin
                typeof(StandardTokenizer).Assembly,
                // Pythia.Liz.Plugin
                typeof(LizHtmlTextRenderer).Assembly,
                // Pythia.Chiron.Plugin
                typeof(LatSylCountSupplierTokenFilter).Assembly,
                // Corpus.Sql.PgSql
                typeof(PgSqlTextRetriever).Assembly);
            container.Verify();

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddInMemoryJson(_profile);
            IConfiguration configuration = builder.Build();

            _factory = new PythiaFactory(container, configuration)
            {
                ConnectionString = _connString
            };
            return _factory;
        }
    }
}
