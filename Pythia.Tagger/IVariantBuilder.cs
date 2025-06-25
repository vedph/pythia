using System.Collections.Generic;
using Pythia.Tagger.Lookup;

namespace Pythia.Tagger;

/// <summary>
/// Variants builder.
/// </summary>
public interface IVariantBuilder
{
    /// <summary>
    /// Build variants for the specified word.
    /// </summary>
    /// <param name="word">The word.</param>
    /// <param name="pos">The optional part of speech for the word.</param>
    /// <param name="index">The lookup index.</param>
    /// <returns>Variants.</returns>
    IList<VariantForm> Build(string word, string? pos, ILookupIndex index);
}
