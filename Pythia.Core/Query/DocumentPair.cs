using System;
using System.Collections.Generic;

namespace Pythia.Core.Query;

/// <summary>
/// A document name=value pair used in building word-index queries.
/// </summary>
public class DocumentPair
{
    /// <summary>
    /// Gets a value indicating whether <see cref="Name"/> refers to a privileged
    /// document attribute.
    /// </summary>
    public bool IsPrivileged { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Name"/> refers to a numeric
    /// value.
    /// </summary>
    public bool IsNumeric { get; }

    /// <summary>
    /// Gets the attribute pair's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the attribute pair's value. This is null when the pair
    /// refers to a range using <see cref="MinValue"/> and <see cref="MaxValue"/>.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the minimum value (included). This is used when
    /// <see cref="IsNumeric"/> is true.
    /// </summary>
    public double MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value (excluded). This is used when
    /// <see cref="IsNumeric"/> is true.
    /// </summary>
    public double MaxValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentPair"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    /// <param name="privileged">if set to <c>true</c> this pair refers to a
    /// privileged attribute.</param>
    /// <exception cref="ArgumentNullException">name or value</exception>
    public DocumentPair(string name, string value, bool privileged)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        IsPrivileged = privileged;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentPair"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="privileged">if set to <c>true</c> this pair refers to a
    /// privileged attribute.</param>
    /// <exception cref="ArgumentNullException">name or value</exception>
    public DocumentPair(string name, double min, double max, bool privileged)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        MinValue = min;
        MaxValue = max;
        IsPrivileged = privileged;
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        string prSuffix = IsPrivileged ? "*" : "";
        return IsNumeric
            ? $"{Name}{prSuffix} {MinValue}:{MaxValue}"
            : $"{Name}{prSuffix}={Value}";
    }

    /// <summary>
    /// Generates all the binning pairs corresponding to the specified range.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="privileged">if set to <c>true</c> [privileged].</param>
    /// <param name="min">The minimum.</param>
    /// <param name="max">The maximum.</param>
    /// <param name="categoryCount">The category count.</param>
    /// <returns>Pairs.</returns>
    /// <exception cref="ArgumentNullException">name</exception>
    public static IList<DocumentPair> GenerateBinPairs(string name,
        bool privileged, double min, double max, int categoryCount)
    {
        ArgumentNullException.ThrowIfNull(nameof(name));

        double size = (max - min) / categoryCount;
        List<DocumentPair> pairs = [];
        double d = min;

        while (d < max)
        {
            pairs.Add(new DocumentPair(name, d, d + size, privileged));
            d += size;
        }

        return pairs;
    }
}
