using Fusi.Tools;
using System.IO;
using System.Threading.Tasks;

namespace Corpus.Core.Analysis;

/// <summary>
/// A filter to be applied to the source text before processing it.
/// </summary>
public interface ITextFilter
{
    /// <summary>
    /// Applies the filter to the specified reader asynchronously.
    /// </summary>
    /// <param name="reader">The input reader.</param>
    /// <param name="context">The optional context.</param>
    /// <returns>The output reader.</returns>
    Task<TextReader> ApplyAsync(TextReader reader,
        IHasDataDictionary? context = null);
}
