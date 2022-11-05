using Fusi.Text.Unicode;
using Fusi.Tools.Config;
using Pythia.Core.Analysis;
using System;
using System.Text;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// Standard structure value filter: this removes any character different
    /// from letter or digit or apostrophe or whitespace, removes any diacritics
    /// from each letter, and lowercases each letter. Whitespaces are all
    /// normalized to a single space, and the result is trimmed.
    /// <para>Tag: <c>struct-filter.standard</c>.</para>
    /// </summary>
    /// <seealso cref="IStructureValueFilter" />
    [Tag("struct-filter.standard")]
    public sealed class StandardStructureValueFilter : IStructureValueFilter
    {
        private readonly UniData _ud;

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardStructureValueFilter"/>
        /// class.
        /// </summary>
        /// <param name="ud">The Unicode data helper to be used.</param>
        /// <exception cref="ArgumentNullException">ud</exception>
        public StandardStructureValueFilter(UniData ud)
        {
            _ud = ud ?? throw new ArgumentNullException(nameof(ud));
        }

        /// <summary>
        /// Applies this filter to the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="structure">The structure being parsed.</param>
        /// <exception cref="ArgumentNullException">text</exception>
        public void Apply(StringBuilder text, Structure? structure)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            bool ws = false;
            int i = text.Length - 1;
            while (i >- 1)
            {
                bool ld;
                // remove any not-allowed character
                if (!(ld = char.IsLetterOrDigit(text[i]))
                    && !(ws = char.IsWhiteSpace(text[i]))
                    && text[i] != '\'')
                {
                    text.Remove(i--, 1);
                    continue;
                }

                // purge letters
                if (ld)
                {
                    if (char.IsLetter(text[i]))
                    {
                        text[i] = char.ToLowerInvariant(text[i] <= 0x03FF
                            ? UniHelper.GetSegment(text[i])
                            : _ud.GetSegment(text[i], true));
                    }
                    i--;
                }
                else if (ws)
                {
                    text[i] = ' ';
                    int j = i--;
                    while (i > -1 && char.IsWhiteSpace(text[i])) i--;
                    if (i < j) text.Remove(i + 1, j - i - 1);
                }
                else i--;
            }

            // trim left
            i = 0;
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (i > 0) text.Remove(0, i);

            // trim right
            i = text.Length - 1;
            while (i > -1 && char.IsWhiteSpace(text[i])) i--;
            if (i < text.Length - 1) text.Remove(i + 1, text.Length - i - 1);
        }
    }
}
