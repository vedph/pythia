using Fusi.Text.Unicode;
using Fusi.Tools.Configuration;
using Pythia.Core.Analysis;
using System.Globalization;
using System.Text;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// Italian literal filter. This filter removes all the characters which
/// are not letters, digit, apostrophe, or currency symbols; strips from them
/// all diacritics, and lowercases all the letters.
/// <para>Tag: <c>literal-filter.ita</c>.</para>
/// </summary>
/// <seealso cref="ILiteralFilter" />
[Tag("literal-filter.ita")]
public sealed class ItalianLiteralFilter : ILiteralFilter
{
    private static UniData? _ud;

    private static char GetSegment(char c)
    {
        if (UniHelper.IsInRange(c)) return UniHelper.GetSegment(c);

        _ud ??= new UniData();
        return _ud.GetSegment(c, true);
    }

    /// <summary>
    /// Applies the filter to the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    public void Apply(StringBuilder text)
    {
        // keep only letters/apostrophe/digits/currency, removing diacritics
        // and lowercase
        int aposCount = 0;
        for (int i = text.Length - 1; i > -1; i--)
        {
            char c = text[i];
            if (!char.IsLetterOrDigit(c) && (c != '\'') &&
                char.GetUnicodeCategory(c) != UnicodeCategory.CurrencySymbol)
            {
                text.Remove(i, 1);
                continue;
            }

            if (c == '\'')
            {
                aposCount++;
            }
            else
            {
                char filtered = char.IsLetter(c) ? GetSegment(c) : c;
                text[i] = char.ToLowerInvariant(filtered);
            }
        }

        // corner case: if the token has only ', purge it
        if (aposCount == text.Length) text.Clear();
    }
}
