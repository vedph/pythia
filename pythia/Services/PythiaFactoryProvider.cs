using Corpus.Core.Plugin.Analysis;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Microsoft.Extensions.Configuration;
using Pythia.Chiron.Plugin;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Liz.Plugin;
using Pythia.Sql.PgSql;
using SimpleInjector;
using System;

namespace Pythia.Cli.Services
{
    public static class PythiaFactoryProvider
    {
        public static PythiaFactory GetFactory(string profileId, string profile,
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
                // Pythia.Crusca.Plugin
                // typeof(CruscaHtmlTextRenderer).Assembly,
                // Pythia.Liz.Plugin
                typeof(LizHtmlTextRenderer).Assembly,
                // Pythia.Chiron.Plugin
                typeof(LatSylCountSupplierTokenFilter).Assembly,
                // Corpus.Sql.PgSql
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
