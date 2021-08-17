using System.Collections.Generic;
using Pythia.Tagger.Lookup;

namespace Pythia.Tagger
{
    /// <summary>
    /// Variants builder.
    /// </summary>
    public interface IVariantBuilder
    {
        /// <summary>
        /// Build variants for the specified word.
        /// </summary>
        /// <param name="word">The word.</param>
        /// <param name="index">The lookup index.</param>
        /// <returns>Variants.</returns>
        IList<Variant> Build(string word, ILookupIndex index);
    }
}
