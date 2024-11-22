using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Corpus.Core.Analysis;
using Fusi.Tools;
using Fusi.Tools.Configuration;

namespace Corpus.Core.Plugin.Analysis;

/// <summary>
/// Filter for preprocessing TEI documents. This filter blank-fills the whole
/// TEI header (assuming it's coded as <c>&lt;teiHeader&gt;</c>), and each
/// tag in the document (unless instructed to keep the tags).
/// <para>Tag: <c>text-filter.tei</c>.</para>
/// </summary>
/// <seealso cref="ITextFilter" />
[Tag("text-filter.tei")]
public sealed class TeiTextFilter : ITextFilter,
    IConfigurable<TeiTextFilterOptions>
{
    private readonly Regex _headerRegex;
    private readonly Regex _tagRegex;
    private bool _keepTags;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeiTextFilter"/> class.
    /// </summary>
    public TeiTextFilter()
    {
        _headerRegex = new Regex("<teiHeader>.+</teiHeader>",
            RegexOptions.Singleline);
        _tagRegex = new Regex("<[^>]+>");
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(TeiTextFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _keepTags = options.KeepTags;
    }

    /// <summary>
    /// Applies this filter.
    /// </summary>
    /// <param name="reader">The input text reader.</param>
    /// <param name="context">The optional context. Not used.</param>
    /// <returns>The output text reader.</returns>
    /// <exception cref="ArgumentNullException">null reader</exception>
    public async Task<TextReader> ApplyAsync(TextReader reader,
        IHasDataDictionary? context = null)
    {
        ArgumentNullException.ThrowIfNull(reader);

        string text = await reader.ReadToEndAsync();

        text = _headerRegex.Replace(text, m => new string(' ', m.Length));
        if (!_keepTags)
            text = _tagRegex.Replace(text, m => new string(' ', m.Length));

        return new StringReader(text);
    }
}

/// <summary>
/// Options for <see cref="TeiTextFilter"/>.
/// </summary>
public sealed class TeiTextFilterOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to keep tags in the TEI's
    /// text. The default value is <c>false</c>. Even when <c>true</c>,
    /// the TEI header is cleared anyway.
    /// </summary>
    public bool KeepTags { get; set; }
}
