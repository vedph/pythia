using System;
using Pythia.Tagger.Lookup;

namespace Pythia.Tagger;

/// <summary>
/// Variant word form.
/// </summary>
public sealed class VariantForm
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the source.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the part of speech.
    /// </summary>
    public string? Pos { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VariantForm"/> class.
    /// </summary>
    public VariantForm()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VariantForm"/> class.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <param name="type">The type.</param>
    /// <param name="source">The source.</param>
    /// <exception cref="ArgumentNullException">entry</exception>
    public VariantForm(LookupEntry entry, string type, string source)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Value = entry.Value;
        Type = type;
        Source = source;
        Pos = entry.Pos;
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"[{Type}] {Value} < {Source}";
    }
}
