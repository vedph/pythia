using Chiron.Italian;
using Fusi.Tools.Configuration;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pythia.Chiron.Ita.Plugin;

/// <summary>
/// Italian syllable count (<c>sylc</c>) supplier token filter.
/// </summary>
/// <seealso cref="SylCountSupplierTokenFilterBase" />
[Tag("token-filter.syl-count-supplier.ita")]
public sealed class ItaSylCountSupplierTokenFilter :
    SylCountSupplierTokenFilterBase
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ItaSylCountSupplierTokenFilter"/> class.
    /// </summary>
    public ItaSylCountSupplierTokenFilter() : base(LoadProfile())
    {
    }

    private static string LoadProfile()
    {
        using StreamReader reader = new(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(
            "Pythia.Chiron.Ita.Plugin.Assets.Profile.json")!,
            Encoding.UTF8);
        return reader.ReadToEnd();
    }

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
