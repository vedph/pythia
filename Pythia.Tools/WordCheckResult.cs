using System;
using System.Collections.Generic;
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
    /// The type of the check result.
    /// </summary>
    public WordCheckResultType Type { get; }

    /// <summary>
    /// Message describing the result of the check.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The action to be taken based on the check result.
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// The data associated with the check result.
    /// </summary>
    public Dictionary<string, string>? Data { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="WordCheckResult"/> with the
    /// specified source word.
    /// </summary>
    /// <param name="source">The source word.</param>
    /// <param name="level">The severity level of the result.</param>
    /// <exception cref="ArgumentNullException">source or action</exception>
    public WordCheckResult(WordToCheck source, WordCheckResultType level)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Type = level;
    }

    /// <summary>
    /// Converts the result to a string representation.
    /// </summary>
    /// <returns>String.</returns>
    public override string ToString()
    {
        StringBuilder sb = new();
        switch (Type)
        {
            case WordCheckResultType.Warning:
                sb.Append("[WRN]");
                break;
            case WordCheckResultType.ErrorWithHint:
                sb.Append("[ERH]");
                break;
            case WordCheckResultType.Error:
                sb.Append("[ERR]");
                break;
            default:
                sb.Append("[INF]");
                break;
        }

        if (!string.IsNullOrEmpty(Action)) sb.Append(Action);

        sb.Append(": ").Append(Source);
        
        return sb.ToString();
    }
}

/// <summary>
/// The type of a word check result.
/// </summary>
public enum WordCheckResultType
{
    /// <summary>
    /// Information.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error with a suggestion for correction.
    /// </summary>
    ErrorWithHint = 2,

    /// <summary>
    /// Error without a suggestion for correction.
    /// </summary>
    Error = 3
}
