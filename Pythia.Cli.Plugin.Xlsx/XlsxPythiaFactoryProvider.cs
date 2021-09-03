using Corpus.Core.Plugin.Analysis;
using Fusi.Tools.Config;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Core;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Sql.PgSql;
using Pythia.Xlsx.Plugin;
using SimpleInjector;
using System;

namespace Pythia.Cli.Plugin.Xlsx
{
    /// <summary>
    /// Excel Pythia factory provider. This adds Pythia.Xlsx.Plugin components
    /// to the standard factory provider.
    /// </summary>
    /// <seealso cref="ICliPythiaFactoryProvider" />
    [Tag("factory-provider.xlsx")]
    public sealed class XlsxPythiaFactoryProvider : ICliPythiaFactoryProvider
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
                // Pythia.Xlsx.Plugin
                typeof(FsExcelAttributeParser).Assembly,
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
