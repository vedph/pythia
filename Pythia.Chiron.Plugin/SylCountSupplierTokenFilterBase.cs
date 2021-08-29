using Chiron.Core;
using Chiron.Core.Config;
using Chiron.Core.Input;
using Chiron.Core.Phonology;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Microsoft.Extensions.Configuration;
using Pythia.Core;
using Pythia.Core.Analysis;
using SimpleInjector;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pythia.Chiron.Plugin
{
    /// <summary>
    /// Base class for syllable count supplier token filters.
    /// </summary>
    /// <seealso cref="ITokenFilter" />
    public abstract class SylCountSupplierTokenFilterBase : ITokenFilter
    {
        private readonly Pipeline _pipeline;
        private readonly IPhonemizer _phonemizer;

        /// <summary>
        /// Gets or sets the Chiron profile identifier.
        /// </summary>
        protected virtual string ProfileId { get; }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="SylCountSupplierTokenFilterBase"/> class.
        /// </summary>
        protected SylCountSupplierTokenFilterBase()
        {
            _pipeline = GetPipelineFactory().GetPipeline();
            _phonemizer = _pipeline.GetPhonemizer();
        }

        private static AnalysisRequest GetRequest(string text)
        {
            return new AnalysisRequest
            {
                TextUnit = new TextUnit
                {
                    DocumentId = 1,
                    Number = 1,
                    SourceText = text
                }
            };
        }

        private static Stream GetResourceStream(string resourceName)
        {
            Stream stream = typeof(SylCountSupplierTokenFilterBase)
                .GetTypeInfo().Assembly
                .GetManifestResourceStream(
                $"Pythia.Chiron.Plugin.Assets.{resourceName}");
            return stream;
        }

        private static string LoadResourceText(string resourceName)
        {
            using StreamReader reader = new StreamReader(
                GetResourceStream(resourceName),
                Encoding.UTF8);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Gets the additional assemblies specialized in the language
        /// handled by the derived class.
        /// </summary>
        /// <returns>Assemblies.</returns>
        protected abstract Assembly[] GetAdditionalAssemblies();

        private PipelineFactory GetPipelineFactory()
        {
            Container container = new Container();
            PipelineFactory.ConfigureServices(container,
                GetAdditionalAssemblies());
            container.Verify();

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddInMemoryJson(LoadResourceText(ProfileId + ".json"));
            IConfiguration configuration = builder.Build();

            return new PipelineFactory(container, configuration);
        }

        /// <summary>
        /// Apply the filter to the specified token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="position">The position which will be assigned to
        /// the resulting token, provided that it's not empty. Not used.
        /// </param>
        /// <exception cref="ArgumentNullException">token</exception>
        public void Apply(Token token, int position)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            try
            {
                if (token.Value.Length == 0) return;

                AnalysisRequest request = GetRequest(token.Value);
                AnalysisResponse response = _pipeline.Execute(request);
                if (response.ResponseType != AnalysisResponseType.Complete) return;

                _phonemizer.Syllabify();
                if (_phonemizer.Context.SyllableCount < 1) return;

                //token.Attributes.Add(new Corpus.Core.Attribute
                //{
                //    Name = "sylc",
                //    DocumentId = token.DocumentId,
                //    TargetId = token.Position,
                //    Type = Corpus.Core.AttributeType.Number,
                //    Value = _phonemizer.Context.SyllableCount.ToString(
                //        CultureInfo.InvariantCulture)
                //});
                token.Attributes.Add(new Corpus.Core.Attribute
                {
                    Name = "sylc",
                    TargetId = token.DocumentId,
                    Type = Corpus.Core.AttributeType.Number,
                    Value = _phonemizer.Context.SyllableCount.ToString(
                        CultureInfo.InvariantCulture)
                });
                //token.Attributes.Add(new Corpus.Core.Attribute
                //{
                //    Name = "sylc-pos",
                //    TargetId = token.DocumentId,
                //    Type = Corpus.Core.AttributeType.Number,
                //    Value = token.Position.ToString(CultureInfo.InvariantCulture)
                //});
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Serilog.Log.Logger.Error(ex, ex.Message);
            }
        }
    }
}
