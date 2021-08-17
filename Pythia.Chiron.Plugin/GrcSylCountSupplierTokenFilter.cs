using Chiron.Greek;
using Fusi.Tools.Config;
using System.Reflection;

namespace Pythia.Chiron.Plugin
{
    /// <summary>
    /// Syllables count supplier token filter for the ancient Greek language.
    /// This uses the Chiron engine to provide the count of syllables
    /// of each filtered token, in a token attribute named <c>sylc</c>.
    /// Tag: <c>token-filter.sylc-supplier-grc</c>
    /// </summary>
    [Tag("token-filter.sylc-supplier-grc")]
    public sealed class GrcSylCountSupplierTokenFilter :
        SylCountSupplierTokenFilterBase
    {
        /// <summary>
        /// Gets or sets the Chiron profile identifier.
        /// </summary>
        protected override string ProfileId { get => "Profile-grc"; }

        /// <summary>
        /// Gets the additional assemblies specialized in the language
        /// handled by the derived class.
        /// </summary>
        /// <returns>Assemblies.</returns>
        protected override Assembly[] GetAdditionalAssemblies()
        {
            return new[] { typeof(GreekPhonemizer).Assembly };
        }
    }
}
