using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Corpus.Core;
using Corpus.Core.Plugin.Analysis;
using Fusi.Tools;
using Fusi.Tools.Config;
using Fusi.Xml;
using Pythia.Core.Analysis;
using Attribute = Corpus.Core.Attribute;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// A parser for element-based structures in XML documents.
    /// <para>Tag: <c>structure-parser.xml</c>.</para>
    /// </summary>
    [Tag("structure-parser.xml")]
    public sealed class XmlStructureParser : StructureParserBase,
        IConfigurable<XmlStructureParserOptions>
    {
        private readonly List<Structure> _structures;
        private XmlStructureDefinition[] _definitions;
        private IDictionary<string, string> _namespaces;
        private int _bufferSize;

        private IProgress<ProgressReport> _progress;
        private int _count;
        private ProgressReport _report;
        private CancellationToken _cancel;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlStructureParser" />
        /// class.
        /// </summary>
        public XmlStructureParser()
        {
            _structures = new List<Structure>();
            _bufferSize = 100;
        }

        /// <summary>
        /// Configures the object with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">options</exception>
        public void Configure(XmlStructureParserOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            // document filters
            SetDocumentFilters(options.DocumentFilters);

            // read prefix=namespace pairs if any
            _namespaces = XmlNsOptionHelper.ParseNamespaces(options.Namespaces);

            // buffer size
            _bufferSize = options.BufferSize < 0? 100 : options.BufferSize;

            // definitions
            _definitions = options.Definitions;
        }

        private string ApplyFilters(string text, Structure structure)
        {
            StringBuilder sb = new StringBuilder(text);
            foreach (IStructureValueFilter filter in Filters)
            {
                filter.Apply(sb, structure);
            }
            return sb.ToString();
        }

        private static string ResolveArgMacros(string value)
        {
            // the unique macro is currently {$_}
            int i = value.LastIndexOf("{$_}");
            if (i == -1) return value;

            StringBuilder sb = new StringBuilder(value);
            do
            {
                // corner case: initial is just removed
                if (i == 0)
                {
                    sb.Remove(0, 4);
                    break;
                }
                else
                {
                    sb.Remove(i, 4);

                    // replace with space only if not preceded by space,
                    // and not final
                    if (!char.IsWhiteSpace(value[i - 1]) && i < sb.Length)
                        sb.Insert(i, ' ');
                    i = value.LastIndexOf("{$_}", i - 1);
                }
            } while (i > -1);

            return sb.ToString();
        }

        private string GetStructureValue(XmlStructureDefinition definition,
            XElement target, XmlNamespaceManager nsmgr)
        {
            // use the target name if no value template defined
            if (string.IsNullOrEmpty(definition.ValueTemplate))
                return definition.Name;

            // if no placeholder this is a constant value, just ret it
            if (definition.ValueTemplate.IndexOf('{') == -1)
                return definition.ValueTemplate;

            // else we must lookup all the placeholders used in the template:
            // first, get all the used arguments names
            IList<string> argNames = definition.GetUsedArgNames();
            if (argNames == null) return definition.ValueTemplate;

            // then, collect the value of each used argument
            XPathNavigator nav = target.CreateNavigator();
            Dictionary<string, string> argValues = new Dictionary<string, string>();

            foreach (string argName in argNames)
            {
                string argXPath = definition.GetArgXPath(argName);
                if (argXPath == null) continue;

                var iterator = nav.Select(argXPath, nsmgr);
                if (iterator.MoveNext())
                    argValues[argName] = iterator.Current.Value;
            }

            // finally, build the value from the template (excluding {$...})
            string value = Regex.Replace(definition.ValueTemplate, @"\{([^$}][^}]*)\}",
                (Match m) =>
            {
                string argName = m.Groups[1].Value;
                return argValues.ContainsKey(argName) ?
                    argValues[argName] : "";
            });

            // resolve eventual {$...} commands
            return ResolveArgMacros(value);
        }

        private void AddStructure(int documentId, string text,
            XmlStructureDefinition definition, XElement target,
            XmlNamespaceManager nsmgr)
        {
            _count++;
            if (_progress != null && _count % 10 == 0)
            {
                _report.Count = _count;
                _progress?.Report(_report);
            }

            // get the structure's range
            IXmlLineInfo info = target;
            // line position refers to 1st char past <, so subtract 1 from it
            int index = IndexCalculator.GetIndex(
                info.LineNumber, info.LinePosition - 1);

            Tuple<int, int> range = null;
            if (Repository != null)
            {
                range = Repository.GetTokenPositionRange(documentId,
                    index,
                    OffsetHelper.GetElementEndOffset(text, index) - 1);
            }
            if (range == null) return;

            // create the structure
            Structure structure = new Structure
            {
                StartPosition = range.Item1,
                EndPosition = range.Item2,
                DocumentId = documentId,
                Name = definition.Name
            };

            // get the structure's value if any
            string value = GetStructureValue(definition, target, nsmgr);
            if (!string.IsNullOrEmpty(value))
            {
                value = ApplyFilters(value, structure);

                structure.Attributes.Add(new Attribute(definition.Name, value)
                {
                    TargetId = documentId,
                    Type = definition.Type
                });
            }

            // special case: if the structure targets a token, add the attribute
            // to all the tokens inside it, and discard the structure itself
            if (definition.TokenTargetName != null)
            {
                Repository?.AddTokenAttributes(documentId,
                    structure.StartPosition,
                    structure.EndPosition,
                    definition.TokenTargetName,
                    value,
                    definition.Type);
                return;
            }

            // add to buffer and flush it to database if full
            _structures.Add(structure);
            if (_structures.Count >= _bufferSize)
            {
                Repository?.AddStructures(_structures);
                _structures.Clear();
            }
        }

        /// <summary>
        /// Parses the specified document content.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="reader">The document's text reader.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <param name="cancel">The optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">null reader or
        /// calculator</exception>
        protected override void DoParse(Document document, TextReader reader,
            IProgress<ProgressReport> progress = null,
            CancellationToken? cancel = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            // nothing to do if no root or paths
            _count = 0;
            _structures.Clear();
            if (_definitions == null) return;

            try
            {
                _progress = progress;
                if (_progress != null) _report = new ProgressReport();
                _cancel = cancel ?? CancellationToken.None;

                // parse XML from the received text
                string text = reader.ReadToEnd();
                XDocument doc = XDocument.Parse(text,
                    LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
                if (doc.Root == null) return;

                // load namespaces from both document and options
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(
                    doc.CreateReader().NameTable);
                if (_namespaces?.Count > 0)
                {
                    foreach (var ns in _namespaces)
                        nsmgr.AddNamespace(ns.Key, ns.Value);
                }

                // search each defined structure in the document
                foreach (XmlStructureDefinition def in _definitions)
                {
                    foreach (XElement target in
                        doc.XPathSelectElements(def.XPath, nsmgr))
                    {
                        AddStructure(document.Id, text, def, target, nsmgr);
                    }
                }

                // empty the buffer
                if (_structures.Count > 0)
                    Repository?.AddStructures(_structures);
            }
            finally
            {
                _progress = null;
            }
        }
    }

    #region XmlStructureParserOptions
    /// <summary>
    /// Options for <see cref="XmlStructureParser"/>.
    /// </summary>
    public class XmlStructureParserOptions : StructureParserOptions
    {
        /// <summary>
        /// Gets or sets the definitions.
        /// </summary>
        public XmlStructureDefinition[] Definitions { get; set; }

        /// <summary>
        /// Gets or sets a set of optional key=namespace URI pairs. Each string
        /// has format <c>prefix=namespace</c>. When dealing with documents with
        /// namespaces, add all the prefixes you will use in
        /// <see cref="Definitions"/> here, so that they will be expanded
        /// before processing.
        /// </summary>
        public string[] Namespaces { get; set; }

        /// <summary>
        /// Gets or sets the size of the structures buffer. Structures
        /// are flushed to the database only when the buffer is filled,
        /// thus avoiding excessive pressure on the database. The default
        /// value is 100.
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Create a new instance of the <see cref="XmlStructureParserOptions"/>
        /// class.
        /// </summary>
        public XmlStructureParserOptions()
        {
            BufferSize = 100;
        }
    }
    #endregion
}
