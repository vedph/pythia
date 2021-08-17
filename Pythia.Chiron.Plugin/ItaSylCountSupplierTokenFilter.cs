using Chiron.Italian;
using Fusi.Tools.Config;
using System.Reflection;

namespace Pythia.Chiron.Plugin
{
    /// <summary>
    /// Syllables count supplier token filter for the Italian language.
    /// This uses the Chiron engine to provide the count of syllables
    /// of each filtered token, in a token attribute named <c>sylc</c>.
    /// Tag: <c>token-filter.sylc-supplier-ita</c>
    /// </summary>
    [Tag("token-filter.sylc-supplier-ita")]
    public sealed class ItaSylCountSupplierTokenFilter :
        SylCountSupplierTokenFilterBase
    {
        /// <summary>
        /// Gets or sets the Chiron profile identifier.
        /// </summary>
        protected override string ProfileId { get => "Profile-ita"; }

        /// <summary>
        /// Gets the additional assemblies specialized in the language
        /// handled by the derived class.
        /// </summary>
        /// <returns>Assemblies.</returns>
        protected override Assembly[] GetAdditionalAssemblies()
        {
            return new[] { typeof(ItalianPhonemizer).Assembly };
        }
    }
}
