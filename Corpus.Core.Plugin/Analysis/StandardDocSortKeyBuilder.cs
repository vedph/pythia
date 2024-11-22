using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Corpus.Core.Analysis;
using Fusi.Text.Unicode;
using Fusi.Tools.Configuration;

namespace Corpus.Core.Plugin.Analysis;

/// <summary>
/// Standard sort key builder. This builder uses author, title and year
/// attributes to build a sort key.
/// <para>Tag: <c>doc-sortkey-builder.standard</c>.</para>
/// </summary>
/// <seealso cref="IDocSortKeyBuilder" />
[Tag("doc-sortkey-builder.standard")]
public sealed class StandardDocSortKeyBuilder : IDocSortKeyBuilder
{
    private static readonly UniData _ud = new();
    private readonly char[] _nameSeparators;
    private readonly Regex _nameRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardDocSortKeyBuilder"/>
    /// class.
    /// </summary>
    public StandardDocSortKeyBuilder()
    {
        _nameSeparators = new[] {';', ','};
        _nameRegex = new Regex(@"(?<l>[^;,]+)(?:\s*,\s*(?<f>[^;]+))?");
    }

    private static string Filter(string s)
    {
        return string.Join("", from c in s
            where char.IsLetterOrDigit(c)
            select char.ToLowerInvariant(_ud.GetSegment(c, true)));
    }

    private static void AppendFiltered(string text, StringBuilder sb)
    {
        foreach (char c in text.Where(char.IsLetterOrDigit))
            sb.Append(char.ToLowerInvariant(_ud.GetSegment(c, true)));
    }

    /// <summary>
    /// Builds the sort key for the specified document.
    /// </summary>
    /// <param name="document">source document</param>
    /// <returns>key</returns>
    /// <exception cref="ArgumentNullException">null document</exception>
    public string Build(IDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // author
        StringBuilder sb = new();
        if (!string.IsNullOrEmpty(document.Author))
        {
            // an author field may include several names with pattern
            // "last, first; last, first..."; in this case, pick the last name
            // which comes first in alphabetical order
            if (document.Author.IndexOfAny(_nameSeparators) == -1)
            {
                AppendFiltered(document.Author, sb);
            }
            else
            {
                List<string> lastNames = [];

                foreach (Match m in _nameRegex.Matches(document.Author))
                    lastNames.Add(Filter(m.Groups["l"].Value));
                string? last = lastNames.Order().FirstOrDefault();

                if (last != null) sb.Append(last);
                else AppendFiltered(document.Author, sb);
            }
        }

        // title
        sb.Append('-');
        if (!string.IsNullOrEmpty(document.Title))
            AppendFiltered(document.Title, sb);

        // date value with format A or P plus NNNN.NN
        sb.Append('-');
        sb.Append(document.DateValue < 0 ? 'A' : 'P');
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:0000.00}",
            document.DateValue);

        return sb.ToString();
    }
}
