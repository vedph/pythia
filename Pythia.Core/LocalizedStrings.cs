using System;

namespace Pythia.Core;

/// <summary>
/// Localized strings.
/// </summary>
internal static class LocalizedStrings
{
    /// <summary>
    /// Format the specified text template filling it with the specified arguments.
    /// </summary>
    /// <remarks>This is just a wrapper for <c>String.Format</c> using the culture
    /// of this object resources.</remarks>
    /// <param name="template">template</param>
    /// <param name="args">arguments</param>
    /// <returns>formatted string</returns>
    /// <exception cref="ArgumentNullException">null template</exception>
    public static string Format(string template, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(template);
        return string.Format(Properties.Resources.Culture, template, args);
    }
}
