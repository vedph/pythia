using Fusi.Text.Unicode;
using Fusi.Tools;
using Fusi.Tools.Configuration;
using Pythia.Core.Analysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// Italian tagged token filter. This filter normally removes all the characters
/// which are not letters or apostrophe, strips from them all diacritics, and
/// lowercases all the letters. Yet, for those tokens included in the specified
/// list of tags, it will just lowercase them and trim initial and final
/// punctuation-like characters, as specified by options. This is useful for
/// tokens representing numbers, dates, email addresses, etc. The filter relies
/// on <see cref="XmlLocalTagListTextFilter"/> to determine whether a token is
/// inside a tag or not.
/// <para>Tag: <c>token-filter.ita-tagged</c>.</para>
/// </summary>
[Tag("token-filter.ita-tagged")]
public sealed class ItalianTaggedTokenFilter :
    IConfigurable<ItalianTaggedTokenFilterOptions>
{
    private ItalianTaggedTokenFilterOptions _options;
    private static UniData? _ud;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItalianTaggedTokenFilter"/>
    /// class.
    /// </summary>
    public ItalianTaggedTokenFilter()
    {
        _options = new ItalianTaggedTokenFilterOptions();
    }

    /// <summary>
    /// Configures the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="System.ArgumentNullException">options</exception>
    public void Configure(ItalianTaggedTokenFilterOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        _options = options;
    }

    private static char GetSegment(char c)
    {
        if (UniHelper.IsInRange(c)) return UniHelper.GetSegment(c);

        _ud ??= new UniData();
        return _ud.GetSegment(c, true);
    }

    private XmlTagListEntry? FindEnclosingTag(int index,
        IList<XmlTagListEntry> entries)
    {
        // entries are sorted by range start
        foreach (XmlTagListEntry entry in entries)
        {
            if (index < entry.Range.Start) break;
            if (_options.Tags.Contains(entry.Name) && entry.Range.Contains(index))
                return entry;
        }
        return null;
    }

    private string TrimEdges(string text)
    {
        int start = 0;
        while (start < text.Length &&
            _options.TrimmedEdges!.Contains(text[start]))
        {
            start++;
        }

        int end = text.Length - 1;
        while (end >= 0 && _options.TrimmedEdges!.Contains(text[end]))
        {
            end--;
        }

        if (start == 0 && end == text.Length - 1) return text;
        return text.Substring(start, end - start + 1);
    }

    /// <summary>
    /// Apply the filter to the specified token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="position">The position which will be assigned to
    /// the resulting token, provided that it's not empty. Not used.
    /// </param>
    /// <param name="context">The optional context. Not used.</param>
    /// <exception cref="ArgumentNullException">null token</exception>
    public void Apply(Token token, int position,
        IHasDataDictionary? context = null)
    {
        if (token == null) throw new ArgumentNullException(nameof(token));
        if (string.IsNullOrEmpty(token.Value)) return;

        // special behavior for tags
        if (_options.Tags.Count > 0 && context?.Data.TryGetValue(
            XmlLocalTagListTextFilter.XML_LOCAL_TAG_LIST_KEY,
            out object? list) == true)
        {
            IList<XmlTagListEntry>? entries = list as IList<XmlTagListEntry>;
            if (entries != null)
            {
                XmlTagListEntry? entry = FindEnclosingTag(token.Index, entries);
                if (entry != null)
                {
                    token.Value = (string.IsNullOrEmpty(_options.TrimmedEdges)
                        ? token.Value
                        : TrimEdges(token.Value)).ToLowerInvariant();
                    return;
                }
            }
        }

        // keep only letters/apostrophe, removing diacritics and lowercase
        StringBuilder sb = new();
        int aposCount = 0;
        foreach (char c in token.Value)
        {
            if (!char.IsLetter(c) && (c != '\'')) continue;

            char filtered = GetSegment(c);
            sb.Append(char.ToLowerInvariant(filtered));
            if (c == '\'') aposCount++;
        }

        // corner case: if the token has only ', purge it
        token.Value = aposCount == sb.Length ? "" : sb.ToString();
    }
}

/// <summary>
/// Options for <see cref="ItalianTaggedTokenFilter"/>.
/// </summary>
public class ItalianTaggedTokenFilterOptions
{
    /// <summary>
    /// Gets or sets the tags to be treated as containers for tokens to be
    /// filtered in a special way like numbers, dates, or email addresses.
    /// The default tags are: <c>date</c>, <c>email</c>, <c>num</c>.
    /// </summary>
    public HashSet<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets the trimmed edges string; this includes any character
    /// which should be removed from the start or the end of the special
    /// token. If null, no trimming is performed. The default is a set of
    /// common punctuation characters like brackets, punctuation, quotes,
    /// etc.
    /// </summary>
    public string? TrimmedEdges { get; set; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ItalianTaggedTokenFilterOptions"/> class.
    /// </summary>
    public ItalianTaggedTokenFilterOptions()
    {
        Tags = new HashSet<string> { "date", "email", "num" };
        TrimmedEdges = "()[]{},;:.!?'/\"«»\u2018\u2019\u201c\u201d";
    }
}
