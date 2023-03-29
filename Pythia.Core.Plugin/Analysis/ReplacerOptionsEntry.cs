namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// A general purpose set of options for replacing text. This is used by
/// several filters relying on <see cref="Fusi.Tools.Text.TextReplacer"/>.
/// </summary>
public class ReplacerOptionsEntry
{
    /// <summary>
    /// Gets or sets the source text or pattern.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the target text.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the max repetitions count, or 0 for no limit
    /// (=keep replacing until no more changes).
    /// </summary>
    public int Repetitions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Source"/> is a
    /// regular expression pattern.
    /// </summary>
    public bool IsPattern { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{(IsPattern ? "*" : "")} {Source} => {Target} (×{Repetitions})";
    }
}