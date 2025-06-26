using System.IO;
using System.Text;

namespace Pythia.Tagger.Ita.Plugin;

/// <summary>
/// Italian part of speech full tag builder.
/// </summary>
public sealed class ItalianPosTagBuilder : PosTagBuilder
{
    /// <summary>
    /// Creates a new instance of the <see cref="ItalianPosTagBuilder"/> class
    /// </summary>
    public ItalianPosTagBuilder()
    {
        LoadProfile(new StreamReader(
            GetType().Assembly.GetManifestResourceStream(
                "Pythia.Tagger.Ita.Plugin.Assets.Pos.csv")!,
            Encoding.UTF8));
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PosTagBuilder"/> class with
    /// the specified part of speech and features.
    /// </summary>
    /// <param name="pos">The POS tag.</param>
    /// <param name="features">Array of feature key/value pairs. The item at
    /// index 0 is key, at 1 its value, at 2 the second key, at 3 its value,
    /// and so forth.</param>
    /// <exception cref="ArgumentNullException">pos</exception>
    /// <exception cref="ArgumentException">uneven features</exception>
    public ItalianPosTagBuilder(string pos, params string[] features) :
        base(pos, features)
    {
        LoadProfile(new StreamReader(
            GetType().Assembly.GetManifestResourceStream(
                "Pythia.Tagger.Ita.Plugin.Assets.Pos.csv")!,
            Encoding.UTF8));
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PosTagBuilder"/> class
    /// copying data from the specified <see cref="PosTag"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">tag</exception>
    public ItalianPosTagBuilder(PosTag tag) : base(tag)
    {
        LoadProfile(new StreamReader(
            GetType().Assembly.GetManifestResourceStream(
                "Pythia.Tagger.Ita.Plugin.Assets.Pos.csv")!,
            Encoding.UTF8));
    }
}
