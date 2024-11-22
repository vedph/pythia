using Corpus.Core.Reading;
using Fusi.Tools.Configuration;
using Fusi.Xml.Extras.Render;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Corpus.Core.Plugin.Reading;

/// <summary>
/// A text renderer based on an XSLT script.
/// Tag: <c>text-renderer.xslt</c>.
/// </summary>
/// <seealso cref="ITextRenderer" />
[Tag("text-renderer.xslt")]
public sealed class XsltTextRenderer : ITextRenderer,
    IConfigurable<XsltTextRendererOptions>
{
    private XsltTransformer? _xslt;
    private XName? _rootElementName;
    private bool _indent;

    /// <summary>
    /// Configures this renderer with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(XsltTextRendererOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _rootElementName = options.ScriptRootElement;
        _indent = options.IsIndentEnabled;

        // load the script and prepare the transformation
        string? xslt = null;
        if (options.Script!.StartsWith('<'))
        {
            xslt = options.Script;
        }
        else
        {
            using StreamReader reader = new(
                new FileStream(options.Script, FileMode.Open,
                FileAccess.Read, FileShare.Read), Encoding.UTF8);
            xslt = reader.ReadToEnd();
        }
        _xslt = new XsltTransformer(xslt, options.ParseScriptArgs())
        {
            IsIndentEnabled = options.IsIndentEnabled
        };
    }

    /// <summary>
    /// Renders the specified text.
    /// </summary>
    /// <param name="document">The document the text belongs to. This can
    /// be used by renderers to change their behavior according to the
    /// document's metadata.</param>
    /// <param name="text">The input text.</param>
    /// <returns>rendered text</returns>
    /// <exception cref="ArgumentNullException">document or text</exception>
    public string Render(IDocument document, string text)
    {
        ArgumentNullException.ThrowIfNull(document);

        ArgumentNullException.ThrowIfNull(text);

        // wrap into root element when not found at root
        if (_rootElementName != null)
        {
            // prevent a malformed (root-less) document by wrapping it
            // when a parse error is thrown
            XDocument doc;
            try
            {
                doc = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
            }
            catch
            {
                StringBuilder sb = new();
                sb.Append('<').Append(_rootElementName.LocalName);
                if (!string.IsNullOrEmpty(_rootElementName.NamespaceName))
                {
                    sb.Append(" xmlns=\"")
                      .Append(_rootElementName.NamespaceName)
                      .Append('"');
                }
                sb.Append('>');
                sb.Append(text);
                sb.Append("</").Append(_rootElementName.LocalName).Append('>');
                text = sb.ToString();
                doc = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
            }
            if (doc.Root?.Name != _rootElementName)
            {
                XDocument newDoc = new(new XElement(_rootElementName, doc.Root));
                text = newDoc.ToString();
            }
        }

        try
        {
            StringWriter writer = new();
            _xslt!.Transform(new StringReader(text),
                writer, new XmlWriterSettings
                {
                    Indent = _indent,
                    ConformanceLevel = ConformanceLevel.Fragment
                });
            return writer.ToString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            return $"{ex.Message}\n{ex}";
        }
    }
}

/// <summary>
/// Options for <see cref="XsltTextRenderer"/>.
/// </summary>
public class XsltTextRendererOptions
{
    /// <summary>
    /// Gets or sets the XSLT script source (e.g. a file path), or the
    /// script directly (when it begins with <c>&lt;</c>).
    /// </summary>
    public string? Script { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether output indent is enabled.
    /// </summary>
    public bool IsIndentEnabled { get; set; }

    /// <summary>
    /// Gets or sets the script arguments. This is an optional list of
    /// name=value pairs, representing arguments to be passed to the XSLT
    /// script.
    /// </summary>
    public IList<string>? ScriptArgs { get; set; }

    /// <summary>
    /// Gets or sets the name of the root element in the script.
    /// If the XML fragment being rendered lacks this element, it will
    /// be wrapped in it before rendering. You can prefix to this name
    /// a namespace between braces.
    /// </summary>
    public string? ScriptRootElement { get; set; }

    /// <summary>
    /// Parses the script arguments.
    /// </summary>
    /// <returns>A dictionary with argument values keyed by their name.
    /// </returns>
    public IDictionary<string, object>? ParseScriptArgs()
    {
        Dictionary<string, object>? args = null;
        if (ScriptArgs?.Count > 0)
        {
            args = [];
            foreach (string arg in ScriptArgs)
            {
                Match m = Regex.Match(arg, "^([^=]+)=(.*)$");
                if (m.Success)
                    args[m.Groups[1].Value] = m.Groups[2].Value;
            }
        }
        return args;
    }
}
