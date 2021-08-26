using Pythia.Core.Analysis;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Fusi.Xml;
using Pythia.Core.Config;

// XML reader API details:
// http://diranieh.com/NETXML/XmlReader

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// Base class for XML document tokenizers. Such tokenizers use a specific
    /// inner tokenizer for the value of each XML text/CDATA node.
    /// Tokenizers derived from this class can use the XML context information
    /// in <see cref="CurrentContexts"/> to apply further processing before
    /// the token is returned. This allows e.g. adding more attributes to it
    /// according to its context, or more involved processing, like e.g.
    /// collecting sentences tokens and POS-tag them whenever a sentence is
    /// closed.
    /// </summary>
    /// <seealso cref="TokenizerBase" />
    public abstract class XmlTokenizerBase : TokenizerBase,
        IHasInnerTokenizer
    {
        private XmlReader _xmlReader;
        private ITokenizer _innerTokenizer;
        private int _nodeBaseIndex;
        private int _position;
        private bool _textNodeRead;

        /// <summary>
        /// Gets the current XML contexts.
        /// </summary>
        public List<XmlTokenizerContext> CurrentContexts { get; }

        /// <summary>
        /// Gets the current XML (text/CDATA) node information.
        /// </summary>
        protected IXmlLineInfo CurrentXmlInfo { get; private set; }

        /// <summary>
        /// Gets the original full text of the document being tokenized.
        /// </summary>
        protected string FullText { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlTokenizerBase"/> class.
        /// </summary>
        protected XmlTokenizerBase()
        {
            CurrentContexts = new List<XmlTokenizerContext>();
            _innerTokenizer = new StandardTokenizer();
        }

        /// <summary>
        /// Sets the inner tokenizer.
        /// </summary>
        /// <param name="tokenizer">The tokenizer.</param>
        /// <exception cref="ArgumentNullException">tokenizer</exception>
        public void SetInnerTokenizer(ITokenizer tokenizer)
        {
            _innerTokenizer = tokenizer ??
                throw new ArgumentNullException(nameof(tokenizer));
        }

        /// <summary>
        /// Called after <see cref="TokenizerBase.Start" /> has been called.
        /// </summary>
        protected override void OnStarted()
        {
            base.OnStarted();

            FullText = Reader.ReadToEnd();
            Reader = new StringReader(FullText);
            _textNodeRead = false;
            _position = 0;

            _xmlReader = XmlReader.Create(Reader,
                new XmlReaderSettings
                {
                    IgnoreWhitespace = false,
                    DtdProcessing = DtdProcessing.Ignore
                });
            CurrentXmlInfo = (IXmlLineInfo)_xmlReader;
        }

        /// <summary>
        /// Called when the next relevant node has been read and processed
        /// by this class. Relevant XML node types are only element, end
        /// element, text, and CDATA. The default implementation does nothing.
        /// </summary>
        /// <param name="reader">The reader.</param>
        protected virtual void OnNextNode(XmlReader reader)
        {
        }

        private string ReadNextTextNode()
        {
            _textNodeRead = false;
            while (_xmlReader.Read())
            {
                switch (_xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (_xmlReader.IsEmptyElement)
                        {
                            OnNextNode(_xmlReader);
                            break;
                        }

                        // XElement e = XNode.ReadFrom(_xmlReader) as XElement;
                        XmlTokenizerContext ctx = new XmlTokenizerContext
                        {
                            TagName = _xmlReader.Name,
                            Depth = _xmlReader.Depth
                        };
                        if (_xmlReader.HasAttributes)
                        {
                            _xmlReader.MoveToFirstAttribute();
                            ctx.AddAttribute(_xmlReader.Name, _xmlReader.Value);
                            while (_xmlReader.MoveToNextAttribute())
                            {
                                ctx.AddAttribute(_xmlReader.Name, _xmlReader.Value);
                            }
                            _xmlReader.MoveToElement();
                        }
                        CurrentContexts.Add(ctx);
                        OnNextNode(_xmlReader);
                        break;

                    case XmlNodeType.EndElement:
                        CurrentContexts.RemoveAt(CurrentContexts.Count - 1);
                        OnNextNode(_xmlReader);
                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        _nodeBaseIndex = OffsetHelper.GetOffset(
                            FullText, CurrentXmlInfo.LineNumber, CurrentXmlInfo.LinePosition);
                        OnNextNode(_xmlReader);
                        _textNodeRead = true;
                        return _xmlReader.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Called when the token has been read, before returning it.
        /// The default implementation does nothing.
        /// Ovveride this method to add custom processing according to the token
        /// and its XML context.
        /// </summary>
        protected virtual void OnTokenRead() { }

        /// <summary>
        /// Called after <see cref="TokenizerBase.Next" /> has been invoked.
        /// </summary>
        /// <returns>false if end of text reached</returns>
        protected override bool OnNext()
        {
            if (!_textNodeRead)
            {
                string text = ReadNextTextNode();
                if (text == null) return false;
                _innerTokenizer.Start(new StringReader(text), DocumentId);
            }

            while (true)
            {
                if (_innerTokenizer.Next())
                {
                    _position++;
                    CurrentToken.CopyFrom(_innerTokenizer.CurrentToken);
                    CurrentToken.Position = _position;
                    CurrentToken.Index += _nodeBaseIndex;
                    OnTokenRead();
                    return true;
                }
                string text = ReadNextTextNode();
                if (text == null) return false;

                _innerTokenizer.Start(new StringReader(text), DocumentId);
            }
        }
    }

    /// <summary>
    /// XML context information used by <see cref="XmlTokenizerBase"/>-derived
    /// tokenizers.
    /// </summary>
    public class XmlTokenizerContext
    {
        private Dictionary<string, string> _attrs;

        /// <summary>
        /// Gets or sets the name of the tag.
        /// </summary>
        public string TagName { get; set; }

        /// <summary>
        /// Gets or sets the depth level.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Adds the attribute with the specified name and value.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">name</exception>
        public void AddAttribute(string name, string value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (_attrs == null) _attrs = new Dictionary<string, string>();
            _attrs[name] = value;
        }

        /// <summary>
        /// Gets all the attribute names.
        /// </summary>
        /// <returns>The names.</returns>
        public IEnumerable<string> GetAttributeNames()
        {
            if (_attrs == null) return new string[0];
            return _attrs.Keys;
        }

        /// <summary>
        /// Gets the attribute.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The attribute value or null if not found.</returns>
        /// <exception cref="ArgumentNullException">name</exception>
        public string GetAttribute(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (_attrs == null || _attrs.Count == 0) return null;
            return _attrs.ContainsKey(name) ? _attrs[name] : null;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{TagName}"
                + (_attrs != null?
                string.Join(", ",
                    from p in _attrs select $"{p.Key}={p.Value}") : "");
        }
    }
}
