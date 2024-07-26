using System;
using System.Linq;
using Fusi.Tools;
using Fusi.Tools.Configuration;
using Pythia.Core.Analysis;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// A token filter which removes from <see cref="TextSpan.Value"/> any
/// non-letter/digit/' char and lowercases the letters.
/// <para>Tag: <c>token-filter.lo-alnum-apos</c>.</para>
/// </summary>
/// <seealso cref="ITokenFilter" />
[Tag("token-filter.lo-alnum-apos")]
public sealed class LoAlnumAposTokenFilter : ITokenFilter
{
    /// <summary>
    /// Filters the specified token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="position">The position which will be assigned to
    /// the resulting token, provided that it's not empty. Not used.
    /// </param>
    /// <param name="context">The optional context. Not used.</param>
    /// <returns>The input token (used for chaining)</returns>
    /// <exception cref="ArgumentNullException">token</exception>
    public void Apply(TextSpan token, int position,
        IHasDataDictionary? context = null)
    {
        ArgumentNullException.ThrowIfNull(token);

        token.Value = new string((from c in token.Value
            where char.IsLetterOrDigit(c) || c == '\''
            select char.ToLowerInvariant(c)).ToArray());
    }
}
