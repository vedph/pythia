namespace Corpus.Core;

/// <summary>
/// String extensions.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Truncates the specified string to a maximum length of value
    /// <paramref name="maxLength"/>.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>The truncated string, or null if input was null</returns>
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ?
            value : value[..maxLength];
    }
}
