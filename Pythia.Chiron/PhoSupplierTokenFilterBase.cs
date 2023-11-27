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
using Fusi.Tools.Configuration;
using System.Linq;

namespace Pythia.Chiron.Plugin;

/// <summary>
/// Base class for phonology supplier token filters relying on the Chiron system.
/// This relies on a Chiron phonemizer to supply syllable counts and phonological
/// analysis with or without syllabification.
/// </summary>
/// <seealso cref="ITokenFilter" />
public abstract class PhoSupplierTokenFilterBase : ITokenFilter,
    IConfigurable<PhoSupplierTokenFilterOptions>
{
    private readonly IAnalysisPipeline _pipeline;
    private PhoSupplierTokenFilterOptions _options;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PhoSupplierTokenFilterBase"/> class.
    /// </summary>
    /// <param name="profile">The profile to use.</param>
    protected PhoSupplierTokenFilterBase(string profile)
    {
        _pipeline = GetPipelineFactory(profile).GetPipeline();
        _options = new PhoSupplierTokenFilterOptions();
    }

    /// <summary>
    /// Configures the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(PhoSupplierTokenFilterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Build a request from the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>Request.</returns>
    /// <exception cref="ArgumentNullException">text</exception>
    public static AnalysisRequest GetRequest(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new AnalysisRequest(new TextUnit(1, text), "default", 1, "zeus");
    }

    /// <summary>
    /// Gets the additional assemblies specialized in the language
    /// handled by the derived class.
    /// </summary>
    /// <returns>Assemblies.</returns>
    protected abstract Assembly[] GetAdditionalAssemblies();

    /// <summary>
    /// Gets the DI services host from the specified JSON configuration.
    /// </summary>
    /// <param name="config">The JSON code representing the analysis
    /// configuration.</param>
    /// <returns>The host.</returns>
    /// <exception cref="ArgumentNullException">config</exception>
    protected IHost GetHost(string config)
    {
        ArgumentNullException.ThrowIfNull(config);

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

    /// <summary>
    /// Gets the analysis pipeline factory from the specified profile.
    /// </summary>
    /// <param name="profile">The JSON code representing the analysis profile.
    /// </param>
    /// <returns>Factory.</returns>
    /// <exception cref="ArgumentNullException">profile</exception>
    protected IAnalysisPipelineFactory GetPipelineFactory(string profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        IHost host = GetHost(profile)!;
        return new AnalysisPipelineFactory(host);
    }

    /// <summary>
    /// Apply the filter to the specified token. If the token value is empty,
    /// or if it contains any digits or the @ sign, or if it contains no letters,
    /// no action is taken.
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
        ArgumentNullException.ThrowIfNull(token);

        if (string.IsNullOrEmpty(token.Value) ||
            token.Value.Any(c => char.IsDigit(c) || c == '@') ||
            token.Value.All(c => !char.IsLetter(c)))
        {
            return;
        }

        try
        {
            AnalysisRequest request = GetRequest(token.Value);
            await AnalysisResponse response = _pipeline.ExecuteAsync(request);
            if (response.Type != AnalyzerResultType.Complete) return;

            _pipeline.Phonemizer.Syllabify();
            if (_pipeline.Phonemizer.Context.SyllableCount < 1) return;

            if (_options.Sylc)
            {
                token.AddAttribute(new Corpus.Core.Attribute
                {
                    Name = "sylc",
                    TargetId = token.DocumentId,
                    Type = Corpus.Core.AttributeType.Number,
                    Value = _pipeline.Phonemizer.Context.SyllableCount.ToString(
                        CultureInfo.InvariantCulture)
                });
            }
            if (_options.Ipa)
            {
                token.AddAttribute(new Corpus.Core.Attribute
                {
                    Name = "ipa",
                    TargetId = token.DocumentId,
                    Type = Corpus.Core.AttributeType.Number,
                    Value = _pipeline.Phonemizer.Context.Text.ToString("V",
                        CultureInfo.InvariantCulture)
                });
            }
            if (_options.Ipas)
            {
                token.AddAttribute(new Corpus.Core.Attribute
                {
                    Name = "ipas",
                    TargetId = token.DocumentId,
                    Type = Corpus.Core.AttributeType.Number,
                    Value = _pipeline.Phonemizer.Context.Text.ToString("Vp",
                        CultureInfo.InvariantCulture)
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }
}

/// <summary>
/// Options for phonology supplier token filters.
/// </summary>
public class PhoSupplierTokenFilterOptions
{
    /// <summary>
    /// True to supply <c>sylc</c>=syllable count attribute.
    /// </summary>
    public bool Sylc { get; set; }

    /// <summary>
    /// True to supply <c>ipa</c>=IPA phonemes attribute.
    /// </summary>
    public bool Ipa { get; set; }

    /// <summary>
    /// True to supply <c>ipas</c>=IPA phonemes with syllabification attribute.
    /// </summary>
    public bool Ipas { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhoSupplierTokenFilterOptions"/>
    /// class.
    /// </summary>
    public PhoSupplierTokenFilterOptions()
    {
        Sylc = true;
        Ipa = true;
    }
}
