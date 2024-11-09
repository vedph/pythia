using Corpus.Core.Analysis;
using Corpus.Core.Plugin.Analysis;
using Fusi.Tools;
using Fusi.Tools.Configuration;
using Fusi.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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
        _tags = [];
    }

    private static string ResolveTagName(string name,
        IDictionary<string, string> namespaces)
    {
        return XmlNsOptionHelper.ResolveTagName(name, namespaces)
            ?? throw new InvalidOperationException($"Tag name \"{name}\" " +
                "has unknown namespace prefix");
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
                _tags.Add(ResolveTagName(s, namespaces ?? []));
        }
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

        string xml = await reader.ReadToEndAsync();

        // if no tags defined, just fill all the tags
        if (_tags.Count == 0)
        {
            StringBuilder sb = new(xml);
            XmlFiller.FillTags(sb);
            return new StringReader(sb.ToString());
        }

        // else fill only the tags defined
        StringBuilder filled = new(xml);
        XmlTagRangeSet set = new(xml, _tags);
        foreach (XmlTagRange range in set.GetTagRanges())
        {
            for (int i = range.StartIndex;
                     i < range.StartIndex + range.Length; i++)
            {
                filled[i] = ' ';
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
