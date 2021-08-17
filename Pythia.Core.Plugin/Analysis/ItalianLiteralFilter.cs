using Fusi.Tools.Config;
using Pythia.Core.Analysis;
using System.Text;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// Italian literal filter. This filter removes all the characters which
    /// are not letters or apostrophe, strips from them all diacritics (in
    /// Unicode range 0000-03FF), and lowercases all the letters.
    /// Tag: <c>literal-filter.ita</c>.
    /// </summary>
    /// <seealso cref="ILiteralFilter" />
    [Tag("literal-filter.ita")]
    public sealed class ItalianLiteralFilter : ILiteralFilter
    {
        public void Apply(StringBuilder text)
        {
            // keep only letters/apostrophe, removing diacritics and lowercase
            int aposCount = 0;
            for (int i = text.Length - 1; i > -1; i--)
            {
                char c = text[i];
                if (!char.IsLetter(c) && (c != '\''))
                {
                    text.Remove(i, 1);
                    continue;
                }

                if (c == '\'') aposCount++;
                else
                {
                    char filtered = UniHelper.GetSegment(c);
                    text[i] = char.ToLowerInvariant(filtered);
                }
            }

            // corner case: if the token has only ', purge it
            if (aposCount == text.Length) text.Clear();
        }
    }
}
