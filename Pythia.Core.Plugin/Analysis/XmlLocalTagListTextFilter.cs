using Corpus.Core.Analysis;
using Fusi.Tools;
using Fusi.Tools.Configuration;
using Fusi.Tools.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// XML local (=no namespace) tags list text filter. This extracts a list
/// of <see cref="XmlTagListEntry"/> entries, one for each of the
/// tags found in the text and listed in the filter's options. Each entry
/// has the tag name and position. Entries are stored in context data
/// with key <see cref="XML_LOCAL_TAG_LIST_KEY"/>; the text is not changed
/// at all.
/// <para>Tag: <c>text-filter.xml-local-tag-list</c></para>
/// </summary>
[Tag("text-filter.xml-local-tag-list")]
public sealed class XmlLocalTagListTextFilter : ITextFilter,
    IConfigurable<XmlLocalTagListTextFilterOptions>
{
    /// <summary>
    /// The key used for the results of this filter stored in the filter's
    /// context.
    /// Value: <c>xml-local-tag-list</c>.
    /// </summary>
    public const string XML_LOCAL_TAG_LIST_KEY = "xml-local-tag-list";

    private XmlLocalTagListTextFilterOptions? _options;

    private static readonly Regex _tagRegex =
        new(@"<(?<c>/)?(?<n>[\p{L}_][-_.\p{L}0-9]*)[^/>]*(?<e>/)?>",
        RegexOptions.Compiled);

    /// <summary>
    /// Applies the filter to the specified reader asynchronously.
    /// </summary>
    /// <param name="reader">The input reader.</param>
    /// <param name="context">The context. If null, this filter will do
    /// nothing.</param>
    /// <returns>The output reader.</returns>
    /// <exception cref="ArgumentNullException">reader</exception>
    public Task<TextReader> ApplyAsync(TextReader reader,
        IHasDataDictionary? context = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (context is null) return Task.FromResult(reader);

        string text = reader.ReadToEnd();
        List<XmlTagListEntry> entries = new();
        Stack<XmlTagListEntry> stack = new();

        foreach (Match m in _tagRegex.Matches(text))
        {
            string name = m.Groups["n"].Value;
            if (_options?.Names == null || _options.Names.Contains(name))
            {
                // empty
                if (m.Groups["e"].Length > 0)
                {
                    entries.Add(new XmlTagListEntry(name,
                        new(m.Index, m.Length)));
                }
                // closing
                else if (m.Groups["c"].Length > 0)
                {
                    XmlTagListEntry opening = stack.Pop();
                    entries.Add(new XmlTagListEntry(name,
                        new TextRange(
                            opening.Range.Start,
                            m.Index + m.Length - opening.Range.Start)));
                }
                // opening
                else
                {
                    stack.Push(new XmlTagListEntry(name,
                        new(m.Index, m.Length)));
                }
            }
        }

        context.Data[XML_LOCAL_TAG_LIST_KEY] =
            entries.OrderBy(e => e.Range.Start).ToList();
        return Task.FromResult((TextReader)new StringReader(text));
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(XmlLocalTagListTextFilterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
}

/// <summary>
/// An entry in the list extracted by <see cref="XmlLocalTagListTextFilter"/>.
/// </summary>
public record XmlTagListEntry
{
    /// <summary>
    /// Gets the tag's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the element range, starting with the first character of the
    /// opening tag, and ending with the last character of the closing tag.
    /// For empty tags, this is equal to the tag's span.
    /// </summary>
    public TextRange Range { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlTagListEntry"/>
    /// class.
    /// </summary>
    /// <param name="name">The tag name.</param>
    /// <param name="range">The range.</param>
    public XmlTagListEntry(string name, TextRange range)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Range = range;
    }
}

/// <summary>
/// Options for <see cref="XmlLocalTagListTextFilter"/>.
/// </summary>
public class XmlLocalTagListTextFilterOptions
{
    /// <summary>
    /// Gets or sets the names of the tags to be listed. If not specified,
    /// all the tags will be listed.
    /// </summary>
    public HashSet<string>? Names { get; set; }
}
