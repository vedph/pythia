using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Corpus.Core.Analysis;
using Fusi.Tools.Configuration;

namespace Corpus.Core.Plugin.Analysis;

/// <summary>
/// Standard document's date value calculator, which just copies it from a
/// specified document's attribute.
/// <para>Tag: <c>doc-datevalue-calculator.standard</c>.</para>
/// </summary>
/// <seealso cref="IDocDateValueCalculator" />
[Tag("doc-datevalue-calculator.standard")]
public sealed class StandardDocDateValueCalculator : IDocDateValueCalculator,
    IConfigurable<StandardDocDateValueCalculatorOptions>
{
    private StandardDocDateValueCalculatorOptions? _options;

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(StandardDocDateValueCalculatorOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Calculates the specified attributes.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    /// <returns>Result.</returns>
    /// <exception cref="T:System.ArgumentNullException">null document</exception>
    public double Calculate(IList<Attribute> attributes)
    {
        Attribute? attr = attributes?.FirstOrDefault
            (a => a.Name == _options?.Attribute);
        if (attr == null) return 0;

        return double.TryParse(attr.Value, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var n)
            ? n
            : 0;
    }
}

/// <summary>
/// Options for <see cref="StandardDocDateValueCalculator"/>.
/// </summary>
public sealed class StandardDocDateValueCalculatorOptions
{
    /// <summary>
    /// Gets or sets the name of the document's attribute to copy the date
    /// value from.
    /// </summary>
    public string? Attribute { get; set; }
}
