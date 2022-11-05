using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Corpus.Core.Analysis;
using Corpus.Core.Reading;
using Fusi.Text.Unicode;
using Fusi.Tools.Config;
using Microsoft.Extensions.Configuration;
using Pythia.Core.Analysis;
using SimpleInjector;

namespace Pythia.Core.Config
{
    /// <summary>
    /// A factory for Pythia plugin components.
    /// </summary>
    public sealed class PythiaFactory : ComponentFactoryBase
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
        /// <param name="container">The container.</param>
        /// <param name="configuration">The configuration.</param>
        public PythiaFactory(Container container, IConfiguration configuration)
            : base(container, configuration)
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
        /// <param name="container">The container.</param>
        /// <param name="additionalAssemblies">The optional additional
        /// assemblies.</param>
        /// <exception cref="ArgumentNullException">container</exception>
        public static void ConfigureServices(Container container,
            params Assembly[] additionalAssemblies)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            // https://simpleinjector.readthedocs.io/en/latest/advanced.html?highlight=batch#batch-registration
            Assembly[] assemblies = new[]
            {
                // Pythia.Core
                typeof(PythiaFactory).Assembly,
            };
            if (additionalAssemblies?.Length > 0)
                assemblies = assemblies.Concat(additionalAssemblies).ToArray();

            container.Collection.Register<IAttributeParser>(assemblies);
            container.Collection.Register<IDocSortKeyBuilder>(assemblies);
            container.Collection.Register<IDocDateValueCalculator>(assemblies);
            container.Collection.Register<IStructureValueFilter>(assemblies);
            container.Collection.Register<IStructureParser>(assemblies);
            container.Collection.Register<ILiteralFilter>(assemblies);
            container.Collection.Register<ITextFilter>(assemblies);
            container.Collection.Register<ITokenizer>(assemblies);
            container.Collection.Register<ITokenFilter>(assemblies);
            container.Collection.Register<ISourceCollector>(assemblies);
            container.Collection.Register<ITextRetriever>(assemblies);
            container.Collection.Register<ITextMapper>(assemblies);
            container.Collection.Register<ITextPicker>(assemblies);
            container.Collection.Register<ITextRenderer>(assemblies);

            // required for injection
            container.RegisterInstance(new UniData());
        }

        private static object SupplyProperty(Type optionType,
            PropertyInfo property, object? options, object defaultValue)
        {
            // if options have been loaded, supply if not specified
            if (options != null)
            {
                string? value = (string?)property.GetValue(options);
                if (string.IsNullOrEmpty(value))
                    property.SetValue(options, defaultValue);
            }
            // else create empty options and supply it
            else
            {
                options = Activator.CreateInstance(optionType)!;
                property.SetValue(options, defaultValue);
            }

            return options;
        }

        /// <summary>
        /// Does the custom configuration.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="component">The component.</param>
        /// <param name="section">The section.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="optionType">Type of the option.</param>
        /// <returns>True if custom configuration logic applied.</returns>
        protected override bool DoCustomConfiguration<T>(T component,
            IConfigurationSection section, TypeInfo targetType, Type optionType)
        {
            // get the options if specified
            object? options = section?.Get(optionType);

            // if we have a default connection AND the options type
            // has a ConnectionString property, see if we should supply a value
            // for it
            PropertyInfo? property;
            if (ConnectionString != null
                && (property = optionType.GetProperty(CONNECTION_STRING_NAME)) != null)
            {
                options = SupplyProperty(optionType, property, options, ConnectionString);
            } // conn

            // apply options if any
            if (options != null)
            {
                targetType.GetMethod("Configure")?.Invoke(component,
                    new[] { options });
            }

            return true;
        }

        /// <summary>
        /// Gets the optional query literal filters. These are the filters applied
        /// to user input in query pair literals (equals, not equals, starts
        /// with, ends with, contains).
        /// </summary>
        /// <returns>filters</returns>
        public IList<ILiteralFilter> GetLiteralFilters()
        {
            IList<ComponentFactoryConfigEntry> entries =
                ComponentFactoryConfigEntry.ReadComponentEntries(
                Configuration, "LiteralFilters");
            return GetComponents<ILiteralFilter>(entries);
        }

        /// <summary>
        /// Gets the text filters.
        /// </summary>
        /// <returns>filters</returns>
        public IList<ITextFilter> GetTextFilters()
        {
            IList<ComponentFactoryConfigEntry> entries =
                ComponentFactoryConfigEntry.ReadComponentEntries(
                Configuration, "TextFilters");
            return GetComponents<ITextFilter>(entries);
        }

        /// <summary>
        /// Gets the optional attribute parsers.
        /// </summary>
        /// <returns>parsers or null</returns>
        public IList<IAttributeParser> GetAttributeParsers()
        {
            IList<ComponentFactoryConfigEntry> entries =
                ComponentFactoryConfigEntry.ReadComponentEntries(
                Configuration, "AttributeParsers");
            return GetComponents<IAttributeParser>(entries);
        }

        /// <summary>
        /// Gets the document sort key builder.
        /// </summary>
        /// <returns>builder</returns>
        public IDocSortKeyBuilder? GetDocSortKeyBuilder()
        {
            return GetComponent<IDocSortKeyBuilder>(
                Configuration["DocSortKeyBuilder:Id"],
                "DocSortKeyBuilder:Options",
                true);
        }

        /// <summary>
        /// Gets the document date value calculator.
        /// </summary>
        /// <returns>calculator</returns>
        public IDocDateValueCalculator? GetDocDateValueCalculator()
        {
            return GetComponent<IDocDateValueCalculator>(
                Configuration["DocDateValueCalculator:Id"],
                "DocDateValueCalculator:Options",
                true);
        }

        /// <summary>
        /// Gets the tokenizer with its filters.
        /// </summary>
        /// <returns>tokenizer with its filters</returns>
        public ITokenizer? GetTokenizer(bool inner = false)
        {
            string path = inner ? "Tokenizer:InnerTokenizer" : "Tokenizer";

            ITokenizer? tokenizer = GetComponent<ITokenizer>(
                Configuration[$"{path}:Id"],
                $"{path}:Options",
                true);
            if (tokenizer == null) return null;

            IList<ComponentFactoryConfigEntry> entries =
                ComponentFactoryConfigEntry.ReadComponentEntries(
                Configuration, $"{path}:Options:TokenFilters");
            foreach (ITokenFilter filter in GetComponents<ITokenFilter>(entries))
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
                GetComponents<IStructureParser>(entries);

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
                        GetComponents<IStructureValueFilter>(filterEntries))
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
        public ISourceCollector? GetSourceCollector()
        {
            return GetComponent<ISourceCollector>(
                Configuration["SourceCollector:Id"],
                "SourceCollector:Options",
                true);
        }

        /// <summary>
        /// Gets the text retriever.
        /// </summary>
        /// <returns>retriever</returns>
        public ITextRetriever? GetTextRetriever()
        {
            return GetComponent<ITextRetriever>(
                Configuration["TextRetriever:Id"],
                "TextRetriever:Options",
                true);
        }

        /// <summary>
        /// Gets the text mapper.
        /// </summary>
        /// <returns>mapper</returns>
        /// <exception cref="ArgumentNullException">null profile</exception>
        public ITextMapper? GetTextMapper()
        {
            return GetComponent<ITextMapper>(
                Configuration["TextMapper:Id"],
                "TextMapper:Options",
                true);
        }

        /// <summary>
        /// Gets the text picker.
        /// </summary>
        /// <returns>picker</returns>
        public ITextPicker? GetTextPicker()
        {
            return GetComponent<ITextPicker>(
                Configuration["TextPicker:Id"],
                "TextPicker:Options",
                true);
        }

        /// <summary>
        /// Gets the text renderer.
        /// </summary>
        /// <returns>renderer</returns>
        public ITextRenderer? GetTextRenderer()
        {
            return GetComponent<ITextRenderer>(
                Configuration["TextRenderer:Id"],
                "TextRenderer:Options",
                true);
        }
    }
}
