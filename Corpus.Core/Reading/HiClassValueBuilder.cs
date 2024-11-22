using System;
using System.Collections.Generic;
using System.Linq;

namespace Corpus.Core.Reading;

/// <summary>
/// HTML class value builder for XML TEI-like hi rendition values. This maps
/// each token in the hi value to a corresponding token of the HTML class name.
/// For instance, a hi value like "bold italic" can be mapped into class tokens
/// "i b". You could just avoid the mapping and emit the tokens from hi value
/// into HTML class value, separated by spaces, but often the HTML names are
/// shorter.
/// </summary>
public sealed class HiClassValueBuilder
{
    private readonly char[] _separators;
    private readonly Dictionary<string, string> _tokens;

    /// <summary>
    /// Creates a new nstance of the <see cref="HiClassValueBuilder"/> class.
    /// </summary>
    /// <param name="tokens">tokens mappings. For each token, 1=hi token and
    /// 2=HTML token.</param>
    public HiClassValueBuilder(IEnumerable<Tuple<string, string>> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        _separators = [' ', '\t'];
        _tokens = [];
        foreach (var t in tokens) _tokens[t.Item1] = t.Item2;
    }

    /// <summary>
    /// Build the class value from the specified hi value.
    /// </summary>
    /// <param name="hi">The hi value</param>
    /// <returns>The class value</returns>
    public string Build(string? hi)
    {
        if (string.IsNullOrWhiteSpace(hi)) return "";

        var tokens = from s in
            hi.Split(_separators, StringSplitOptions.RemoveEmptyEntries)
            select _tokens.ContainsKey(s) ? _tokens[s] : "";

        return string.Join(" ", tokens.Order());
    }
}
