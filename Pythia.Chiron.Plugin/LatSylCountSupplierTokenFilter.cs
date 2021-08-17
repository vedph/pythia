using Chiron.Latin;
using Fusi.Tools.Config;
using System.Reflection;

namespace Pythia.Chiron.Plugin
{
    /// <summary>
    /// Syllables count supplier token filter for the Latin language.
    /// This uses the Chiron engine to provide the count of syllables
    /// of each filtered token, in a token attribute named <c>sylc</c>.
    /// Tag: <c>token-filter.sylc-supplier-lat</c>
    /// </summary>
    [Tag("token-filter.sylc-supplier-lat")]
    public sealed class LatSylCountSupplierTokenFilter :
        SylCountSupplierTokenFilterBase
    {
        /// <summary>
        /// Gets or sets the Chiron profile identifier.
        /// </summary>
        protected override string ProfileId { get => "Profile-lat"; }

        /// <summary>
        /// Gets the additional assemblies specialized in the language
        /// handled by the derived class.
        /// </summary>
        /// <returns>Assemblies.</returns>
        protected override Assembly[] GetAdditionalAssemblies()
        {
            return new[] { typeof(LatinPhonemizer).Assembly };
        }
    }
}
