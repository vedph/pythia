using System;
using System.Text;
using Fusi.Text.Unicode;
using Fusi.Tools.Config;
using Pythia.Core.Analysis;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// Italian token filter. This filter removes all the characters which
    /// are not letters or apostrophe, strips from them all diacritics, and
    /// lowercases all the letters.
    /// <para>Tag: <c>token-filter.ita</c>.</para>
    /// </summary>
    [Tag("token-filter.ita")]
    public sealed class ItalianTokenFilter : ITokenFilter
    {
        private static UniData _ud;

        private static char GetSegment(char c)
        {
            if (UniHelper.IsInRange(c)) return UniHelper.GetSegment(c);

            if (_ud == null) _ud = new UniData();
            return _ud.GetSegment(c, true);
        }

        /// <summary>
        /// Apply the filter to the specified token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="position">The position which will be assigned to
        /// the resulting token, provided that it's not empty. Not used.
        /// </param>
        /// <exception cref="ArgumentNullException">null token</exception>
        public void Apply(Token token, int position)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            // keep only letters/apostrophe, removing diacritics and lowercase
            StringBuilder sb = new StringBuilder();
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
}
