using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Corpus.Core.Analysis;
using Fusi.Tools;
using Fusi.Tools.Configuration;

namespace Corpus.Core.Plugin.Analysis;

/// <summary>
/// This filter just replaces U+2019 (right single quotation mark) with an
/// apostrophe (U+0027) when it is included between two letters.
/// <para>Tag: <c>text-filter.quotation-mark</c>.</para>
/// </summary>
[Tag("text-filter.quotation-mark")]
public sealed class QuotationMarkTextFilter : ITextFilter
{
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

        StringBuilder sb = new(await reader.ReadToEndAsync());
        for (int i = 1; i < sb.Length - 1; i++)
        {
            if (sb[i] == '\u2019' && char.IsLetter(sb[i - 1]) &&
                char.IsLetter(sb[i + 1]))
            {
                sb[i] = '\'';
            }
        }

        return new StringReader(sb.ToString());
    }
}
