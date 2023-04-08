using Chiron.Latin;
using Fusi.Tools.Configuration;
using Pythia.Chiron.Plugin;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pythia.Chiron.Lat.Plugin;

/// <summary>
/// Latin phonology attributes supplier token filter.
/// Tag: <c>token-filter.pho-supplier.lat</c>.
/// </summary>
[Tag("token-filter.pho-supplier.lat")]
public sealed class LatPhoSupplierTokenFilter : PhoSupplierTokenFilterBase
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ItaPhoSupplierTokenFilter"/> class.
    /// </summary>
    public LatPhoSupplierTokenFilter() : base(LoadProfile())
    {
    }

    private static string LoadProfile()
    {
        using StreamReader reader = new(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(
            "Pythia.Chiron.Lat.Plugin.Assets.Profile.json")!,
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
        return new[] { typeof(LatinPhonemizer).Assembly };
    }
}
