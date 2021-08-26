using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Corpus.Core;
using Corpus.Core.Reading;
using Fusi.Tools;
using Fusi.Tools.Config;
using Fusi.Xml;
using Pythia.Core.Analysis;
using Attribute = Corpus.Core.Attribute;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// A parser for structures in XML documents.
    /// Tag: <c>structure-parser.xml</c>.
    /// </summary>
    [Tag("structure-parser.xml")]
    public sealed class XmlStructureParser : StructureParserBase,
        IConfigurable<XmlStructureParserOptions>
    {
        private readonly List<Structure> _structures;
        private XmlPath _rootPath;
        private XmlStructureDefinition[] _definitions;
        private int _bufferSize;

        private XElement _rootElement;
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

            SetDocumentFilters(options.DocumentFilters);

            _rootPath = options.RootPath != null ?
                XmlPath.Parse(options.RootPath) : null;

            _bufferSize = options.BufferSize < 0? 100 : options.BufferSize;
            _definitions = options.ParseDefinitions();
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

        private void AddStructure(int documentId, string text,
            XmlStructureDefinition definition, XElement target)
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
            string value = definition.Path.GetValue(target);
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
                value = definition.TokenTargetValue ??
                    ApplyFilters(target.Value ?? "", structure);

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

        private void ParseChildren(XElement element, int documentId, string text)
        {
            foreach (XElement childElement in element.Elements())
            {
                if (_cancel.IsCancellationRequested) break;

                // walk up each definition from the tested element
                // Debug.WriteLine($"Parsing {element.Name}/{childElement.Name}");

                HashSet<string> found = new HashSet<string>();

                foreach (XmlStructureDefinition def in _definitions
                    .Where(d => d.Path.Steps.Length > 0))
                {
                    // if the structure was already matched, just skip
                    // (the first matched definition wins)
                    if (found.Contains(def.Name)) continue;

                    XElement topElement = def.Path.WalkUp(childElement);
                    if (topElement == null) continue;

                    // a definition is fully matched when either the target
                    // element's parent is equal to the root element (e.g.
                    // /TEI/div/p), or when the definition starts with an
                    // any -descendant XPath expression (e.g. just //p).
                    if (topElement.Parent == _rootElement
                        || def.Path.Steps[0].IsIndirect)
                    {
                        // Debug.WriteLine($"Matched definition {def}");
                        AddStructure(documentId, text, def, childElement);
                        found.Add(def.Name);
                    }
                }

                ParseChildren(childElement, documentId, text);
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
            if (_rootPath == null || _definitions == null) return;

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

                // locate the element corresponding to the root target:
                // we will start walking the tree down from that element.
                _rootElement = _rootPath.WalkDown(doc.Root);
                if (_rootElement == null) return;

                // walk down from the root element, looking for matching 
                // structure definitions
                Debug.WriteLine($"XML structure root located: {_rootElement.Name}");
                ParseChildren(_rootElement, document.Id, text);

                // empty the buffer
                if (_structures.Count > 0)
                    Repository?.AddStructures(_structures);
            }
            finally
            {
                _rootElement = null;
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
        /// Gets or sets the root path, in the format used by <see cref="XmlPath"/>.
        /// This is the path to the element to be used as the root node in a
        /// text map.
        /// </summary>
        public string RootPath { get; set; }

        /// <summary>
        /// Gets or sets the definitions, in the format required by
        /// <see cref="XmlStructureDefinition.Parse"/>.
        /// </summary>
        public string[] Definitions { get; set; }

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

        /// <summary>
        /// Parses the definitions from <see cref="Definitions"/>.
        /// </summary>
        /// <returns>definitions or null</returns>
        public XmlStructureDefinition[] ParseDefinitions()
        {
            return Definitions != null ?
                (from s in Definitions
                 select XmlStructureDefinition.Parse(s)).ToArray() :
                null;
        }
    }
    #endregion
}
