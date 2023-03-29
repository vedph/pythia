using Fusi.Tools;
using Fusi.Tools.Configuration;
using Pythia.Core.Analysis;
using System;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// A token filter which injects punctuation attributes for punctuation at
/// the left/right (<c>lp</c> and <c>rp</c>) of a token. All the punctuation
/// character(s) at the left up to the first non-punctuation character are
/// considered as <c>lp</c>; all the punctuation character(s) at the right
/// leftwards, up to the first non punctuation character and before <c>lp</c>
/// characters are considered as <c>rp</c>.
/// <para>Tag: <c>token-filter.punctuation</c>.</para>
/// </summary>
/// <seealso cref="ITokenFilter" />
[Tag("token-filter.punctuation")]
public sealed class PunctuationTokenFilter : ITokenFilter
{
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
    public void Apply(Token token, int position, IHasDataDictionary? context = null)
    {
        if (token == null) throw new ArgumentNullException(nameof(token));

        if (string.IsNullOrEmpty(token.Value)) return;

        int a = 0;
        while (a < token.Length && char.IsPunctuation(token.Value[a])) a++;

        int b = token.Length - 1;
        while (b > a && char.IsPunctuation(token.Value[b])) b--;

        if (a > 0)
            token.AddAttribute(new Corpus.Core.Attribute("lp", token.Value[..a]));

        if (b < token.Length - 1)
            token.AddAttribute(new Corpus.Core.Attribute("rp", token.Value[(b + 1)..]));
    }
}
