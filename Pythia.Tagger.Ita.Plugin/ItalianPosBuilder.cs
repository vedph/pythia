using System.IO;
using System.Text;

namespace Pythia.Tagger.Ita.Plugin;

/// <summary>
/// Italian part of speech full tag builder.
/// </summary>
public sealed class ItalianPosBuilder : PosBuilder
{
    /// <summary>
    /// Creates a new instance of the <see cref="ItalianPosBuilder"/> class
    /// </summary>
    public ItalianPosBuilder()
    {
        Load(new StreamReader(
            GetType().Assembly.GetManifestResourceStream(
                "Pythia.Tagger.Ita.Plugin.Assets.Pos.csv")!,
            Encoding.UTF8));
    }
}
