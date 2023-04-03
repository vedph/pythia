using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System;
using Pythia.Core.Analysis;
using Chiron.Core;
using Microsoft.Extensions.Hosting;
using Chiron.Core.Config;
using Fusi.Microsoft.Extensions.Configuration.InMemoryJson;
using Pythia.Core;
using Fusi.Tools;
using Chiron.Core.Input;
using System.IO;
using System.Text;

namespace Pythia.Chiron.Ita.Plugin;

/// <summary>
/// Base class for syllable count (<c>sylc</c>) supplier token filters
/// relying on the Chiron system.
/// </summary>
/// <seealso cref="ITokenFilter" />
public abstract class SylCountSupplierTokenFilterBase : ITokenFilter
{
    private readonly IAnalysisPipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="SylCountSupplierTokenFilterBase"/> class.
    /// </summary>
    /// <param name="profile">The profile to use.</param>
    protected SylCountSupplierTokenFilterBase(string profile)
    {
        _pipeline = GetPipelineFactory(profile).GetPipeline();
    }

    public static AnalysisRequest GetRequest(string text)
    {
        return new AnalysisRequest(new TextUnit(1, text), "default", "");
    }

    /// <summary>
    /// Gets the additional assemblies specialized in the language
    /// handled by the derived class.
    /// </summary>
    /// <returns>Assemblies.</returns>
    protected abstract Assembly[] GetAdditionalAssemblies();

    protected IHost GetHost(string config)
    {
        return new HostBuilder()
            .ConfigureServices((_, services) =>
            {
                AnalysisPipelineFactory.ConfigureServices(
                    services,
                    true,
                    GetAdditionalAssemblies());
            })
            // extension method from Fusi library
            .AddInMemoryJson(config)
            .Build();
    }

    protected IAnalysisPipelineFactory GetPipelineFactory(string profile)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));

        IHost host = GetHost(profile)!;
        return new AnalysisPipelineFactory(host);
    }

    /// <summary>
    /// Apply the filter to the specified token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="position">The position which will be assigned to
    /// the resulting token, provided that it's not empty. Some filters
    /// may use this value, e.g. to identify tokens like in deferred
    /// POS tagging.</param>
    /// <param name="context">The optional context.</param>
    /// <exception cref="ArgumentNullException">token</exception>
    public void Apply(Token token, int position, IHasDataDictionary? context = null)
    {
        if (token == null) throw new ArgumentNullException(nameof(token));
        if (string.IsNullOrEmpty(token.Value)) return;

        try
        {
            AnalysisRequest request = GetRequest(token.Value);
            AnalysisResponse response = _pipeline.Execute(request);
            if (response.Type != AnalyzerResultType.Complete) return;

            _pipeline.Phonemizer.Syllabify();
            if (_pipeline.Phonemizer.Context.SyllableCount < 1) return;

            token.AddAttribute(new Corpus.Core.Attribute
            {
                Name = "sylc",
                TargetId = token.DocumentId,
                Type = Corpus.Core.AttributeType.Number,
                Value = _pipeline.Phonemizer.Context.SyllableCount.ToString(
                    CultureInfo.InvariantCulture)
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }
}
