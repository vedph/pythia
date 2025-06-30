using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pythia.Tools;

/// <summary>
/// The result of a word check operation.
/// </summary>
public class WordCheckResult
{
    /// <summary>
    /// The word that was checked.
    /// </summary>
    public WordToCheck Source { get; }

    /// <summary>
    /// The action to be taken based on the check result.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// The action's arguments, if any.
    /// </summary>
    public Dictionary<string, object> Arguments { get; } = [];

    /// <summary>
    /// The data associated with the check result.
    /// </summary>
    public Dictionary<string, string> Data { get; } = [];

    /// <summary>
    /// Creates a new instance of <see cref="WordCheckResult"/> with the
    /// specified source word and action.
    /// </summary>
    /// <param name="source">The source word.</param>
    /// <param name="action">The action to be taken.</param>
    /// <exception cref="ArgumentNullException">source or action</exception>
    public WordCheckResult(WordToCheck source, string action)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// Converts the result to a string representation.
    /// </summary>
    /// <returns>String.</returns>
    public override string ToString()
    {
        StringBuilder sb = new(Action);
        if (Arguments.Count > 0)
        {
            sb.Append('(');
            sb.Append(string.Join(", ",
                Arguments.Select(kv => $"{kv.Key}={kv.Value}")));
            sb.Append(')');
        }
        sb.Append(": ").Append(Source);
        return sb.ToString();
    }
}
