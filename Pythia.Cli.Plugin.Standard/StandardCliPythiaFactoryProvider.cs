using Corpus.Core.Plugin.Analysis;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Fusi.Tools.Config;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Core;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Sql.PgSql;
using SimpleInjector;
using System;

namespace Pythia.Cli.Plugin.Standard
{
    /// <summary>
    /// "Standard" Pythia factory provider.
    /// </summary>
    /// <seealso cref="ICliPythiaFactoryProvider" />
    [Tag("factory-provider.standard")]
    public class StandardCliPythiaFactoryProvider : ICliPythiaFactoryProvider
    {
        public PythiaFactory GetFactory(string profileId, string profile,
            string connString)
        {
            if (profileId == null)
                throw new ArgumentNullException(nameof(profileId));
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (connString == null)
                throw new ArgumentNullException(nameof(connString));

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
                ConnectionString = connString
            };
        }
    }
}
