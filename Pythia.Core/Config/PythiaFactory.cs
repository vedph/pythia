using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corpus.Core.Analysis;
using Corpus.Core.Reading;
using Fusi.Text.Unicode;
using Fusi.Tools.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pythia.Core.Analysis;

namespace Pythia.Core.Config;

/// <summary>
/// A factory for Pythia plugin components.
/// </summary>
public sealed class PythiaFactory : ComponentFactory
{
    /// <summary>
    /// The name of the connection string property to be supplied
    /// in POCO option objects (<c>ConnectionString</c>).
    /// </summary>
    public const string CONNECTION_STRING_NAME = "ConnectionString";

    /// <summary>
    /// The optional general connection string to supply to any component
    /// requiring an option named <see cref="CONNECTION_STRING_NAME"/>
    /// (=<c>ConnectionString</c>), when this option is not specified
    /// in its configuration.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PythiaFactory" /> class.
    /// </summary>
    /// <param name="host">The host</param>
    public PythiaFactory(IHost host) : base(host)
    {
    }

    /// <summary>
    /// Configures the container services to use components from
    /// <c>Pythia.Core</c>.
    /// This is just a helper method: at any rate, the configuration of
    /// the container is external to the VSM factory. You could use this
    /// method as a model and create your own, or call this method to
    /// register the components from these two assemblies, and then
    /// further configure the container, or add more assemblies when
    /// calling this via <paramref name="additionalAssemblies"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="additionalAssemblies">The optional additional
    /// assemblies.</param>
    /// <exception cref="ArgumentNullException">container</exception>
    public static void ConfigureServices(IServiceCollection services,
        params Assembly[] additionalAssemblies)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.AddSingleton<UniData>();

        Assembly[] assemblies = new[]
        {
            // Pythia.Core
            typeof(PythiaFactory).Assembly,
        };
        if (additionalAssemblies?.Length > 0)
            assemblies = assemblies.Concat(additionalAssemblies).ToArray();

        foreach (Type it in new[]
        {
            typeof(IAttributeParser),
            typeof(IDocSortKeyBuilder),
            typeof(IDocDateValueCalculator),
            typeof(IStructureValueFilter),
            typeof(IStructureParser),
            typeof(ILiteralFilter),
            typeof(ITextFilter),
            typeof(ITokenizer),
            typeof(ITokenFilter),
            typeof(ISourceCollector),
            typeof(ITextRetriever),
            typeof(ITextMapper),
            typeof(ITextPicker),
            typeof(ITextRenderer)
        })
        {
            foreach (Type t in GetAssemblyConcreteTypes(assemblies, it))
            {
                services.AddTransient(it, t);
            }
        }
    }

    /// <summary>
    /// Overrides the options to supply connection string.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="section">The section.</param>
    protected override void OverrideOptions(object options,
           IConfigurationSection? section)
    {
        Type optionType = options.GetType();

        // if we have a default connection AND the options type
        // has a ConnectionString property, see if we should supply a value
        // for it
        PropertyInfo? property;
        if (ConnectionString != null &&
            (property = optionType.GetProperty(CONNECTION_STRING_NAME)) != null)
        {
            // here we can safely discard the returned object as it will
            // be equal to the input options, which is not null
            SupplyProperty(optionType, property, options, ConnectionString);
        }
    }

    /// <summary>
    /// Gets the optional query literal filters. These are the filters applied
    /// to user input in query pair literals (equals, not equals, starts
    /// with, ends with, contains).
    /// </summary>
    /// <returns>filters</returns>
    public IList<ILiteralFilter> GetLiteralFilters() =>
        GetRequiredComponents<ILiteralFilter>("LiteralFilters");

    /// <summary>
    /// Gets the text filters.
    /// </summary>
    /// <returns>filters</returns>
    public IList<ITextFilter> GetTextFilters() =>
        GetRequiredComponents<ITextFilter>("TextFilters");

    /// <summary>
    /// Gets the optional attribute parsers.
    /// </summary>
    /// <returns>parsers or null</returns>
    public IList<IAttributeParser> GetAttributeParsers() =>
        GetRequiredComponents<IAttributeParser>("AttributeParsers");

    /// <summary>
    /// Gets the document sort key builder.
    /// </summary>
    /// <returns>builder</returns>
    public IDocSortKeyBuilder GetDocSortKeyBuilder() =>
        GetComponent<IDocSortKeyBuilder>("DocSortKeyBuilder", true)!;

    /// <summary>
    /// Gets the document date value calculator.
    /// </summary>
    /// <returns>calculator</returns>
    public IDocDateValueCalculator GetDocDateValueCalculator() =>
        GetComponent<IDocDateValueCalculator>("DocDateValueCalculator", true)!;

    /// <summary>
    /// Gets the tokenizer with its filters.
    /// </summary>
    /// <returns>tokenizer with its filters</returns>
    public ITokenizer GetTokenizer(bool inner = false)
    {
        string path = inner ? "Tokenizer:InnerTokenizer" : "Tokenizer";

        ITokenizer tokenizer = GetComponent<ITokenizer>(path, true)!;

        IList<ComponentFactoryConfigEntry> entries =
            ComponentFactoryConfigEntry.ReadComponentEntries(
            Configuration, $"{path}:Options:TokenFilters");
        foreach (ITokenFilter filter in GetRequiredComponents<ITokenFilter>
            (entries))
        {
            tokenizer.Filters.Add(filter);
        }

        if (!inner && tokenizer is IHasInnerTokenizer it)
        {
            ITokenizer? innerTokenizer = GetTokenizer(true);
            if (innerTokenizer != null) it.SetInnerTokenizer(innerTokenizer);
        }

        return tokenizer;
    }

    private static string ReplaceLastPathStep(string path, string step)
    {
        int i = path.LastIndexOf(':');
        return i == -1 ? step : path[..(i + 1)] + step;
    }

    /// <summary>
    /// Gets the optional structure parser(s).
    /// </summary>
    /// <returns>parsers</returns>
    public IList<IStructureParser> GetStructureParsers()
    {
        IList<ComponentFactoryConfigEntry> entries =
            ComponentFactoryConfigEntry.ReadComponentEntries(
            Configuration, "StructureParsers");

        IList<IStructureParser> parsers =
            GetRequiredComponents<IStructureParser>(entries);

        for (int i = 0; i < parsers.Count; i++)
        {
            string filtersPath = ReplaceLastPathStep(entries[i].OptionsPath!,
                "Filters");
            IConfigurationSection section = Configuration.GetSection(filtersPath);
            if (section.Exists())
            {
                var filterEntries = ComponentFactoryConfigEntry.ReadComponentEntries(
                    Configuration, filtersPath);
                foreach (IStructureValueFilter filter in
                    GetRequiredComponents<IStructureValueFilter>(filterEntries))
                {
                    parsers[i].Filters.Add(filter);
                }
            }
        }
        return parsers;
    }

    /// <summary>
    /// Gets the source collector.
    /// </summary>
    /// <returns>collector</returns>
    public ISourceCollector GetSourceCollector() =>
        GetComponent<ISourceCollector>("SourceCollector", true)!;

    /// <summary>
    /// Gets the text retriever.
    /// </summary>
    /// <returns>retriever</returns>
    public ITextRetriever GetTextRetriever() =>
        GetComponent<ITextRetriever>("TextRetriever", true)!;

    /// <summary>
    /// Gets the text mapper.
    /// </summary>
    /// <returns>mapper</returns>
    public ITextMapper GetTextMapper() =>
        GetComponent<ITextMapper>("TextMapper", true)!;

    /// <summary>
    /// Gets the text picker.
    /// </summary>
    /// <returns>picker</returns>
    public ITextPicker GetTextPicker() =>
        GetComponent<ITextPicker>("TextPicker", true)!;

    /// <summary>
    /// Gets the text renderer.
    /// </summary>
    /// <returns>renderer</returns>
    public ITextRenderer GetTextRenderer() =>
        GetComponent<ITextRenderer>("TextRenderer", true)!;
}
