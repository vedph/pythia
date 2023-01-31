using Corpus.Core.Analysis;
using Corpus.Core.Plugin.Analysis;
using Corpus.Core.Reading;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Microsoft.Extensions.Hosting;
using Pythia.Core.Analysis;
using Pythia.Core.Config;
using Pythia.Core.Plugin.Analysis;
using Pythia.Liz.Plugin;
using Pythia.Sql.PgSql;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace Pythia.Core.Test.Config
{
    public sealed class PythiaFactoryTest
    {
        private static readonly PythiaFactory _factory = new(GetHost());

        private static IHost GetHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    PythiaFactory.ConfigureServices(services, new[]
                    {
                        // Corpus.Core.Plugin
                        typeof(StandardDocSortKeyBuilder).Assembly,
                        // Pythia.Core.Plugin
                        typeof(StandardTokenizer).Assembly,
                        // Pythia.Liz.Plugin
                        typeof(LizHtmlTextRenderer).Assembly,
                        // Pythia.Sql.PgSql
                        typeof(PgSqlTextRetriever).Assembly
                    });
                })
                // extension method from Fusi library
                .AddInMemoryJson(LoadProfile())
                .Build();
        }

        private static string LoadProfile()
        {
            using var reader = new StreamReader(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Pythia.Core.Test.Assets.SampleProfile.json")!,
                Encoding.UTF8);
            return reader.ReadToEnd();
        }

        [Fact]
        public void GetSourceCollector_NotNull()
        {
            ISourceCollector? collector = _factory.GetSourceCollector();
            Assert.NotNull(collector);
        }

        [Fact]
        public void GetLiteralFilters_1()
        {
            IList<ILiteralFilter> filters = _factory.GetLiteralFilters();
            Assert.Single(filters);
        }

        [Fact]
        public void GetTextFilters_2()
        {
            IList<ITextFilter> filters = _factory.GetTextFilters();
            Assert.Equal(2, filters.Count);
        }

        [Fact]
        public void GetAttributeParsers_NotNull()
        {
            IList<IAttributeParser> parsers = _factory.GetAttributeParsers();
            Assert.Single(parsers);
        }

        [Fact]
        public void GetDocSortKeyBuilder_NotNull()
        {
            IDocSortKeyBuilder? builder = _factory.GetDocSortKeyBuilder();
            Assert.NotNull(builder);
        }

        [Fact]
        public void GetDocDateValueCalculator_NotNull()
        {
            IDocDateValueCalculator? calculator = _factory.GetDocDateValueCalculator();
            Assert.NotNull(calculator);
        }

        [Fact]
        public void GetTokenizer_NotNull()
        {
            ITokenizer? tokenizer = _factory.GetTokenizer();
            Assert.NotNull(tokenizer);
        }

        [Fact]
        public void GetStructureParsers_1()
        {
            IList<IStructureParser> parsers = _factory.GetStructureParsers();
            Assert.Single(parsers);
        }

        [Fact]
        public void GetTextRetriever_NotNull()
        {
            ITextRetriever? retriever = _factory.GetTextRetriever();
            Assert.NotNull(retriever);
        }

        [Fact]
        public void GetTextMapper_NotNull()
        {
            ITextMapper? mapper = _factory.GetTextMapper();
            Assert.NotNull(mapper);
        }

        [Fact]
        public void GetTextPicker_NotNull()
        {
            ITextPicker? picker = _factory.GetTextPicker();
            Assert.NotNull(picker);
        }

        [Fact]
        public void GetTextRenderer_NotNull()
        {
            ITextRenderer? renderer = _factory.GetTextRenderer();
            Assert.NotNull(renderer);
        }
    }
}
