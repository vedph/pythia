using Corpus.Core.Analysis;
using Fusi.Tools.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// Unix-date modern date value calculator. This is based on an attribute
/// with some indication of year and eventually month and day, and calculates
/// the Unix time from it. Alternatively, it can also provide the result
/// as just the integer number resulting from concatenating YYYYMMDD,
/// like <c>20140420</c> from Y=2014, M=4, D=20. This is a more user-friendly
/// value for a simple date.
/// <para>Tag: <c>doc-datevalue-calculator.unix</c>.</para>
/// </summary>
/// <seealso cref="IDocDateValueCalculator" />
[Tag("doc-datevalue-calculator.unix")]
public sealed class UnixDateValueCalculator : IDocDateValueCalculator,
    IConfigurable<UnixDateValueCalculatorOptions>
{
    private string? _name;
    private bool _ymdAsInt;
    private Regex? _ymdRegex;

    /// <summary>
    /// Configures this calculator with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(UnixDateValueCalculatorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _name = options.Attribute;
        _ymdAsInt = options.YmdAsInt;
        _ymdRegex = new Regex(options.YmdPattern, RegexOptions.Compiled);
    }

    private static int GetGroupValue(Match match, string name, int defaultValue)
    {
        return match.Groups[name].Length > 0
            ? int.Parse(match.Groups[name].Value, CultureInfo.InvariantCulture)
            : defaultValue;
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
        if (attr == null || _ymdRegex == null) return 0;

        Match match = _ymdRegex.Match(attr.Value ?? "");
        if (!match.Success) return 0;
        int y = GetGroupValue(match, "y", 0);
        if (y == 0) return 0;

        int m = GetGroupValue(match, "m", 1);
        int d = 1;
        if (m > 0) d = GetGroupValue(match, "d", 1);

        if (_ymdAsInt) return int.Parse($"{y:0000}{m:00}{d:00}");

        DateTimeOffset dto = new DateTime(y, m, d, 0, 0, 0);
        return dto.ToUnixTimeSeconds();
    }
}

/// <summary>
/// Options for <see cref="UnixDateValueCalculator"/>.
/// </summary>
public class UnixDateValueCalculatorOptions
{
    /// <summary>
    /// Gets or sets the name of the document's attribute to read the date
    /// expression from.
    /// </summary>
    public string? Attribute { get; set; }

    /// <summary>
    /// Gets or sets the year-month-day pattern to match from the
    /// <see cref="Attribute"/>'s value. The pattern should provide groups
    /// named <c>y</c> for year, <c>m</c> for month, and <c>d</c> for day.
    /// At least the year group should be defined. For instance, from
    /// a value like <c>2010/04/20</c> we could get year=2004, month=04,
    /// and day=20 using pattern
    /// <c>(?&lt;y&gt;\d{4})/(?&lt;m&gt;\d{2})/(?&lt;d&gt;\d{2})</c>
    /// (which is the default).
    /// </summary>
    public string YmdPattern { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether instead of calculating
    /// the Unix time the value should just be an integer resulting from
    /// concatenating YYYYMMDD.
    /// </summary>
    public bool YmdAsInt { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnixDateValueCalculatorOptions"/>
    /// class.
    /// </summary>
    public UnixDateValueCalculatorOptions()
    {
        YmdPattern = "(?<y>\\d{4})/(?<m>\\d{2})/(<d>\\d{2})";
    }
}
