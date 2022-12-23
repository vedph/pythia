using System;
using System.Collections.Generic;

namespace Pythia.Core;

/// <summary>
/// A single term distribution result.
/// </summary>
public class TermDistribution
{
    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public string Attribute { get; }

    /// <summary>
    /// Gets the frequencies.
    /// </summary>
    public IDictionary<string, long> Frequencies { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TermDistribution"/> class.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    /// <exception cref="ArgumentNullException">attribute</exception>
    public TermDistribution(string attribute)
    {
        Attribute = attribute ??
            throw new ArgumentNullException(nameof(attribute));
        Frequencies = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
    }
}
