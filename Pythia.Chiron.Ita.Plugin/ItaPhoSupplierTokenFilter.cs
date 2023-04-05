using Chiron.Italian;
using Fusi.Tools.Configuration;
using Pythia.Chiron.Plugin;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pythia.Chiron.Ita.Plugin;

/// <summary>
/// Italian phonology attributes supplier token filter.
/// </summary>
/// <seealso cref="PhoSupplierTokenFilterBase" />
[Tag("token-filter.pho-supplier.ita")]
public sealed class ItaPhoSupplierTokenFilter : PhoSupplierTokenFilterBase
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ItaPhoSupplierTokenFilter"/> class.
    /// </summary>
    public ItaPhoSupplierTokenFilter() : base(LoadProfile())
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
