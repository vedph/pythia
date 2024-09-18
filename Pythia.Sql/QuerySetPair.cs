using System.Globalization;
using System.Text.RegularExpressions;

namespace Pythia.Sql;

/// <summary>
/// A name-operator-value pair in a Pythia query set. This just holds
/// name, operator, and value and some derived metadata.
/// </summary>
public sealed class QuerySetPair
{
    // escape in pair's value &HHHH;
    private static readonly Regex _escRegex = new("&([0-9a-fA-F]{1,4});");
    private static readonly Regex _quoteRegex = new(@"^""([^""]*)""$");

    private string? _name;
    private string? _value;

    /// <summary>
    /// Gets or sets the pair number (1-N).
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// The subquery pair ID in the SQL query (sN).
    /// </summary>
    public string Id => "s" + Number;

    /// <summary>
    /// True if it's a structure pair.
    /// </summary>
    public bool IsStructure { get; private set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string? Name
    {
        get { return _name; }
        set
        {
            IsStructure = value?.StartsWith('$') ?? false;
            _name = value != null && IsStructure? value![1..] : value;
        }
    }

    /// <summary>
    /// Gets or sets the operator constant, as defined in the Pythia lexer.
    /// </summary>
    public int Operator { get; set; }

    /// <summary>
    /// Gets or sets the value. Any escape in the value being set is
    /// resolved in the corresponding character.
    /// </summary>
    public string? Value
    {
        get { return _value; }
        set
        {
            // unwrap from ""
            if (value != null)
            {
                Match m = _quoteRegex.Match(value);
                if (m.Success) value = m.Groups[1].Value;
            }

            // replace escapes if any
            _value = value?.IndexOf('&') > -1
                ? _escRegex.Replace(value, m =>
                {
                    return new string((char)
                        int.Parse(m.Groups[1].Value, NumberStyles.HexNumber), 1);
                })
                : value;
        }
    }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    /// <returns>New pair.</returns>
    public QuerySetPair Clone()
    {
        return new QuerySetPair
        {
            Number = Number,
            IsStructure = IsStructure,
            _name = Name,
            Operator = Operator,
            _value = Value
        };
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"#{Number} {(IsStructure ? "$" : "")}{Name}" +
            (Operator > 0 ? $"{Operator} {Value}" : "");
    }
}
