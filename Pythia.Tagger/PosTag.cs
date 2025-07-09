using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pythia.Tagger;

/// <summary>
/// A full POS tag, which consists of a part of speech and a set of features.
/// </summary>
public class PosTag
{
    /// <summary>
    /// The part of speech tag to build the full tag from.
    /// </summary>
    public string Pos { get; set; } = "";

    /// <summary>
    /// The features to include in the full tag.
    /// </summary>
    public Dictionary<string, string> Features { get; } = [];

    /// <summary>
    /// Create a new POS tag with no part of speech or features.
    /// </summary>
    public PosTag()
    {
    }

    /// <summary>
    /// Create a new POS tag with the specified part of speech and features.
    /// </summary>
    /// <param name="pos">POS tag.</param>
    /// <param name="features"><Optional features./param>
    /// <exception cref="ArgumentNullException">pos</exception>
    public PosTag(string pos, IDictionary<string, string>? features = null)
    {
        Pos = pos ?? throw new ArgumentNullException(nameof(pos));

        if (features is not null)
        {
            foreach (KeyValuePair<string, string> kvp in features)
            {
                Features[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// True if the current POS tag is a subset of the specified POS tag,
    /// i.e. it has the same part of speech and all its features are
    /// present in the specified tag with the same values.
    /// </summary>
    /// <param name="tag">Superset tag.</param>
    /// <returns>True if subset.</returns>
    public bool IsSubsetOf(PosTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return Pos == tag.Pos &&
               Features.All(kvp =>
                tag.Features.TryGetValue(kvp.Key, out string? value) &&
                  value == kvp.Value);
    }

    /// <summary>
    /// True if the current POS tag matches the specified part of speech and
    /// all the specified features.
    /// </summary>
    /// <param name="pos">The POS tag.</param>
    /// <param name="features">Array of feature key/value pairs. The item at
    /// index 0 is key, at 1 its value, at 2 the second key, at 3 its value,
    /// and so forth.</param>
    /// <returns>True on match.</returns>
    /// <exception cref="ArgumentException">uneven features</exception>
    public bool IsMatch(string pos, params string[] features)
    {
        if (Pos != pos) return false;
        if (features is null || features.Length == 0) return true;
        if (features.Length % 2 != 0)
        {
            throw new ArgumentException("Features must be in key-value pairs.",
                nameof(features));
        }

        for (int i = 0; i < features.Length; i += 2)
        {
            if (!Features.TryGetValue(features[i], out string? value) ||
                value != features[i + 1])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines if this POS tag matches the specified part of speech and
    /// the features query string.
    /// </summary>
    /// <param name="pos">The part of speech to match.</param>
    /// <param name="featuresQuery">A query string for matching features with
    /// syntax: "key=value" (equality) or "key!=value" (non-equality), connected
    /// by logical operators AND OR and grouped using brackets.
    /// Example: "Number=Plural AND (Gender=Masculine OR Gender=Feminine)"</param>
    /// <returns>True if the tag matches the part of speech and satisfies
    /// the features query.</returns>
    public bool IsMatch(string pos, string? featuresQuery)
    {
        // first check if the part of speech matches
        if (Pos != pos) return false;

        // if no features query, then it's a match
        if (string.IsNullOrWhiteSpace(featuresQuery)) return true;

        return EvaluateFeaturesQuery(featuresQuery);
    }

    private bool EvaluateFeaturesQuery(string query)
    {
        int position = 0;
        return EvaluateExpression(query, ref position);
    }

    private bool EvaluateExpression(string query, ref int position)
    {
        bool result = EvaluateTerm(query, ref position);

        while (position < query.Length)
        {
            // skip whitespace
            SkipWhitespace(query, ref position);

            if (position >= query.Length) break;

            // check for logical operators
            if (position + 2 < query.Length && query.Substring(position, 3)
                == "AND")
            {
                position += 3;
                SkipWhitespace(query, ref position);
                result &= EvaluateTerm(query, ref position);
            }
            else if (position + 1 < query.Length && query.Substring(position, 2)
                == "OR")
            {
                position += 2;
                SkipWhitespace(query, ref position);
                result |= EvaluateTerm(query, ref position);
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private bool EvaluateTerm(string query, ref int position)
    {
        SkipWhitespace(query, ref position);

        if (position >= query.Length) return false;

        // check for opening bracket (subexpression)
        if (query[position] == '(')
        {
            position++;
            bool result = EvaluateExpression(query, ref position);

            SkipWhitespace(query, ref position);

            // ensure closing bracket
            if (position < query.Length && query[position] == ')')
            {
                position++;
                return result;
            }

            throw new FormatException($"Missing closing bracket in features " +
                $"query: {query}");
        }

        // otherwise, evaluate a key=value or key!=value expression
        return EvaluateCondition(query, ref position);
    }

    private bool EvaluateCondition(string query, ref int position)
    {
        // find the key
        int start = position;
        while (position < query.Length &&
               !char.IsWhiteSpace(query[position]) &&
               query[position] != '=' &&
               !(query[position] == '!' && position + 1 < query.Length &&
                 query[position + 1] == '='))
        {
            position++;
        }

        if (position >= query.Length)
        {
            throw new FormatException(
                $"Incomplete condition in features query: {query}");
        }

        string key = query.Substring(start, position - start);

        // skip whitespace before operator
        SkipWhitespace(query, ref position);

        if (position >= query.Length)
        {
            throw new FormatException(
                $"Incomplete condition in features query: {query}");
        }

        // check for equality or inequality operator
        bool isEquality = true;
        if (query[position] == '!')
        {
            isEquality = false;
            position++; // skip !

            if (position >= query.Length || query[position] != '=')
            {
                throw new FormatException(
                    $"Invalid operator in features query, expected != " +
                    $"at position {position}: {query}");
            }
        }

        position++; // skip =

        // skip whitespace after operator
        SkipWhitespace(query, ref position);

        // find the value
        start = position;
        while (position < query.Length &&
               !char.IsWhiteSpace(query[position]) &&
               query[position] != ')' &&
               query[position] != '(')
        {
            position++;
        }

        string value = query.Substring(start, position - start);

        // evaluate the condition
        if (Features.TryGetValue(key, out string? actualValue))
            return isEquality ? actualValue == value : actualValue != value;

        // if key doesn't exist in Features, then equality check is false
        // and inequality check is true
        return !isEquality;
    }

    private static void SkipWhitespace(string query, ref int position)
    {
        while (position < query.Length && char.IsWhiteSpace(query[position]))
            position++;
    }

    /// <summary>
    /// Returns a string representation of the current object.
    /// </summary>
    /// <returns>String.</returns>
    public override string ToString()
    {
        StringBuilder sb = new(Pos);
        if (Features.Count > 0)
        {
            sb.Append(':');
            int count = 0;
            foreach (KeyValuePair<string, string> kvp in
                Features.OrderBy(f => f.Key))
            {
                if (count++ > 0) sb.Append('|');
                sb.Append(kvp.Key).Append('=').Append(kvp.Value);
            }
        }
        return sb.ToString();
    }
}
