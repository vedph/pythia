using Corpus.Core.Analysis;
using Corpus.Core.Plugin.Analysis;
using Fusi.Tools;
using Fusi.Tools.Configuration;
using Fusi.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// XML tags filler text filter. This blank-fills with spaces all the
/// matching tags with their content. For instance, say you have a TEI
/// document with choice including <c>abbr</c> and <c>expan</c>, and you
/// want to blank-fill all the <c>expan</c>'s to avoid indexing them:
/// you can use this text filter to replace <c>expan</c> elements and all
/// their content with spaces, thus effectively removing them from indexed
/// text, while keeping offsets and document's length unchanged.
/// <para>Tag: <c>text-filter.xml-tag-filler</c>.</para>
/// </summary>
/// <seealso cref="ITextFilter" />
[Tag("text-filter.xml-tag-filler")]
public sealed class XmlTagFillerTextFilter : ITextFilter,
    IConfigurable<XmlTagFillerTextFilterOptions>
{
    private readonly HashSet<XName> _tags;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlTagFillerTextFilter"/>
    /// class.
    /// </summary>
    public XmlTagFillerTextFilter()
    {
        _tags = new HashSet<XName>();
    }

    private static string ResolveTagName(string name,
        IDictionary<string, string> namespaces)
    {
        string? resolved = XmlNsOptionHelper.ResolveTagName(name, namespaces);
        if (resolved == null)
        {
            throw new InvalidOperationException($"Tag name \"{name}\" " +
                "has unknown namespace prefix");
        }
        return resolved;
    }

    /// <summary>
    /// Configures the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(XmlTagFillerTextFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // read prefix=namespace pairs if any
        Dictionary<string, string>? namespaces =
            XmlNsOptionHelper.ParseNamespaces(options.Namespaces);

        // stop tags
        _tags.Clear();
        if (options.Tags != null)
        {
            foreach (string s in options.Tags)
            {
                _tags.Add(ResolveTagName(s,
                    namespaces ?? new Dictionary<string, string>()));
            }
        }
    }

    /// <summary>
    /// Skips the outer XML string matching it to the specified XML context
    /// while ignoring xmlns attributes added either in XML or in context.
    /// This is used to determine the length of the XML code portion to fill
    /// once a target tag has been found by this filter.
    /// </summary>
    /// <param name="xml">The XML to skip.</param>
    /// <param name="context">The context.</param>
    /// <param name="start">The start.</param>
    /// <returns>Index to the first character of the context string past the
    /// outer XML string as matched, or -1 if match error.</returns>
    public static int SkipOuterXml(string xml, string context, int start)
    {
        Regex r = new(@" xmlns(?::[^=]+)?=""[^""]*""", RegexOptions.Compiled);

        int xi = 0, ci = start;
        while (xi < xml.Length)
        {
            // keep advancing both until equal
            while (xi < xml.Length && xml[xi] == context[ci])
            {
                xi++;
                ci++;
            }
            if (xi == xml.Length) break;

            // not equal: if there is an xmlns in xml, skip it
            Match m = r.Match(xml, xi);
            if (m.Success && m.Index == xi)
            {
                xi += m.Length;
                continue;
            }
            // if there is an xmlns in context, skip it
            m = r.Match(context, ci);
            if (m.Success && m.Index == ci)
            {
                ci += m.Length;
                continue;
            }
            // else not equal
            return -1;
        }
        return ci;
    }

    /// <summary>
    /// Applies the filter to the specified reader.
    /// </summary>
    /// <param name="reader">The input reader.</param>
    /// <param name="context">The optional context. Not used.</param>
    /// <returns>The output reader.</returns>
    /// <exception cref="ArgumentNullException">reader</exception>
    /// <exception cref="FormatException">Mismatched XML fragment.</exception>
    public async Task<TextReader> ApplyAsync(TextReader reader,
        IHasDataDictionary? context = null)
    {
        ArgumentNullException.ThrowIfNull(reader);

        string xml = reader.ReadToEnd();

        // if no tags defined, just fill all the tags
        if (_tags.Count == 0)
        {
            StringBuilder sb = new(xml);
            XmlFiller.FillTags(sb);
            return new StringReader(sb.ToString());
        }

        // else fill only the tags defined
        XDocument doc = XDocument.Parse(xml,
            LoadOptions.PreserveWhitespace |
            LoadOptions.SetLineInfo);
        StringBuilder filled = new(xml);

        foreach (XName tag in _tags)
        {
            foreach (XElement element in doc.Descendants(tag))
            {
                IXmlLineInfo info = element;

                int offset = OffsetHelper.GetOffset(xml,
                    info.LineNumber,
                    info.LinePosition - 1);

                string outerXml = element.OuterXml();

                // outer XML contains also the default XML namespace attribute,
                // supplied by the XML element when not found in markup
                int end = SkipOuterXml(outerXml, xml, offset);
                if (end == -1)
                {
                    throw new FormatException("Mismatched XML fragment while filling XML tag: "
                        + xml[offset..]);
                }

                for (int i = offset; i < end; i++) filled[i] = ' ';
            }
        }

        return new StringReader(filled.ToString());
    }
}

/// <summary>
/// Options for <see cref="XmlTagFillerTextFilter"/>.
/// </summary>
public class XmlTagFillerTextFilterOptions
{
    /// <summary>
    /// Gets or sets the list of tag names to be blank-filled with all
    /// their content.
    /// When using namespaces, add a prefix (like <c>tei:expan</c>) and
    /// ensure it is defined in <see cref="Namespaces"/>.
    /// If this is empty, all the tags (but not their content) will be
    /// blank-filled.
    /// </summary>
    public IList<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets a set of optional key=namespace URI pairs. Each string
    /// has format <c>prefix=namespace</c>. When dealing with documents with
    /// namespaces, add all the prefixes you will use in <see cref="Tags"/>
    /// here, so that they will be expanded before processing.
    /// </summary>
    public IList<string>? Namespaces { get; set; }
}
