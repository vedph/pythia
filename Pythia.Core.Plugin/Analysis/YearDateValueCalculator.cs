using Corpus.Core.Analysis;
using Fusi.Tools.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// Year date value calculator. This is based on an attribute with some indication
/// of the year which is extracted using a regular expression pattern.
/// </summary>
/// <seealso cref="IDocDateValueCalculator" />
[Tag("doc-datevalue-calculator.year")]
public sealed class YearDateValueCalculator : IDocDateValueCalculator,
    IConfigurable<YearDateValueCalculatorOptions>
{
    private string? _name;
    private Regex? _yearRegex;

    /// <summary>
    /// Configures this calculator with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(YearDateValueCalculatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _name = options.Attribute;
        _yearRegex = new Regex(options.Pattern, RegexOptions.Compiled);
    }

    /// <summary>
    /// Calculates the date value from the specified document's attributes.
    /// </summary>
    /// <param name="attributes">The attributes parsed from a document.</param>
    /// <returns>Date value</returns>
    public double Calculate(IList<Corpus.Core.Attribute> attributes)
    {
        Corpus.Core.Attribute? attr =
            attributes?.FirstOrDefault(a => a.Name == _name);
        if (attr == null || _yearRegex == null) return 0;

        Match match = _yearRegex.Match(attr.Value ?? "");
        if (!match.Success) return 0;

        return int.TryParse(match.Groups["y"].Value, out int year) ? year : 0;
    }
}

/// <summary>
/// Options for <see cref="YearDateValueCalculator"/>.
/// </summary>
public class YearDateValueCalculatorOptions
{
    /// <summary>
    /// Gets or sets the name of the document's attribute to read the date
    /// expression from.
    /// </summary>
    public string? Attribute { get; set; }

    /// <summary>
    /// Gets or sets the year pattern to match from the <see cref="Attribute"/>'s
    /// value. The pattern should provide a group named <c>y</c> for year.
    /// </summary>
    public string Pattern { get; set; } = "^(?<y>\\d{4})";
}
