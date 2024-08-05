using Fusi.Tools;
using Fusi.Tools.Configuration;
using Pythia.Core.Analysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// A token filter which injects punctuation attributes for punctuation at
/// the left/right (<c>lp</c> and <c>rp</c>) of a token. All the punctuation
/// character(s) at the left up to the first non-punctuation character are
/// considered as <c>lp</c>; all the punctuation character(s) at the right
/// leftwards, up to the first non punctuation character and before <c>lp</c>
/// characters are considered as <c>rp</c>.
/// <para>Punctuation characters are any Unicode punctuation unless you
/// specify a whitelist or a blacklist.</para>
/// <para>Tag: <c>token-filter.punctuation</c>.</para>
/// </summary>
/// <seealso cref="ITokenFilter" />
[Tag("token-filter.punctuation")]
public sealed class PunctuationTokenFilter : ITokenFilter,
    IConfigurable<PunctuationTokenFilterOptions>
{
    private readonly HashSet<char> _puncts;
    private int _listType;

    /// <summary>
    /// Initializes a new instance of the <see cref="PunctuationTokenFilter"/>
    /// class.
    /// </summary>
    public PunctuationTokenFilter()
    {
        _puncts = [];
    }

    /// <summary>
    /// Configures the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(PunctuationTokenFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _puncts.Clear();
        if (!string.IsNullOrEmpty(options.Punctuations))
        {
            foreach (char c in options.Punctuations) _puncts.Add(c);
        }
        _listType = options.ListType;
    }

    private bool IsAllowedPunctuation(char c)
    {
        return _listType switch
        {
            -1 => !_puncts.Contains(c),
            +1 => _puncts.Contains(c),
            _ => true
        };
    }

    /// <summary>
    /// Apply the filter to the specified token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="position">The position which will be assigned to
    /// the resulting token, provided that it's not empty. Some filters
    /// may use this value, e.g. to identify tokens like in deferred
    /// POS tagging.</param>
    /// <param name="context">The optional context.</param>
    /// <exception cref="ArgumentNullException">token</exception>
    public Task ApplyAsync(TextSpan token, int position,
        IHasDataDictionary? context = null)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (string.IsNullOrEmpty(token.Value)) return Task.CompletedTask;

        // left
        StringBuilder sa = new();
        int a = 0;
        while (a < token.Length && char.IsPunctuation(token.Value[a]))
        {
            if (IsAllowedPunctuation(token.Value[a])) sa.Append(token.Value[a]);
            a++;
        }

        // right
        StringBuilder sb = new();
        int b = token.Length - 1;
        while (b > a && char.IsPunctuation(token.Value[b]))
        {
            if (IsAllowedPunctuation(token.Value[b])) sb.Insert(0, token.Value[b]);
            b--;
        }

        if (sa.Length > 0)
            token.AddAttribute(new Corpus.Core.Attribute("lp", sa.ToString()));

        if (sb.Length > 0)
            token.AddAttribute(new Corpus.Core.Attribute("rp", sb.ToString()));

        return Task.CompletedTask;
    }
}

/// <summary>
/// Options for <see cref="PunctuationTokenFilter"/>.
/// </summary>
public class PunctuationTokenFilterOptions
{
    /// <summary>
    /// Gets or sets the whitelist/blacklist of punctuation characters.
    /// When not specified, any Unicode punctuation will be included.
    /// </summary>
    public string? Punctuations { get; set; }

    /// <summary>
    /// Gets or sets the type of the <see cref="Punctuations"/> list: 0=none,
    /// 1=whitelist, -1=blacklist.
    /// </summary>
    public int ListType { get; set; }
}
