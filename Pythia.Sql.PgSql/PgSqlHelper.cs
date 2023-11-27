using Pythia.Core.Query;
using System;
using System.Globalization;
using System.Text;

namespace Pythia.Sql.PgSql;

/// <summary>
/// PostgreSQL SLQ helper.
/// </summary>
/// <seealso cref="SqlHelper" />
/// <seealso cref="ISqlHelper" />
public sealed class PgSqlHelper : SqlHelper, ISqlHelper
{
    /// <summary>
    /// Encodes the specified literal text value for SQL.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="hasWildcards">if set to <c>true</c>, the text value
    /// has wildcards.</param>
    /// <param name="wrapInQuotes">if set to <c>true</c>, wrap in quotes
    /// the SQL literal.</param>
    /// <param name="unicode">if set to <c>true</c>, add the Unicode
    /// prefix <c>N</c> before a string literal. This is required in SQL
    /// Server for Unicode strings, while it's harmless in MySql. The
    /// option is meaningful only when <paramref name="wrapInQuotes" /> is
    /// true.</param>
    /// <returns>SQL-encoded value</returns>
    public string SqlEncode(string text, bool hasWildcards = false,
        bool wrapInQuotes = false, bool unicode = true)
    {
        StringBuilder sb = new();

        foreach (char c in text) EncodeSqlAnsiChar(c, hasWildcards, sb);
        if (wrapInQuotes)
        {
            sb.Insert(0, '\'');
            sb.Append('\'');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes the specified keyword.
    /// </summary>
    /// <param name="keyword">The keyword.</param>
    /// <returns>Escaped keyword</returns>
    public string EscapeKeyword(string keyword)
    {
        return string.IsNullOrEmpty(keyword) ? "" : keyword;
    }

    /// <summary>
    /// Builds the paging expression with the specified values.
    /// </summary>
    /// <param name="offset">The offset count.</param>
    /// <param name="limit">The limit count.</param>
    /// <returns>SQL code.</returns>
    public string BuildPaging(int offset, int limit)
    {
        return $"LIMIT {limit} OFFSET {offset}";
    }

    /// <summary>
    /// Builds the SQL code required to represent the text field represented
    /// by <paramref name="name" /> as an integer number, when this cast
    /// is applicable.
    /// </summary>
    /// <param name="name">The expression name.</param>
    /// <returns>SQL code.</returns>
    /// <exception cref="ArgumentNullException">name</exception>
    public string BuildTextAsNumber(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        // we need to cast to varchar or we would have type mismatch
        // for the function's input argument
        return $"(SELECT (CASE pyt_is_numeric({name}::varchar) " +
            $"WHEN true THEN {name}::double precision " +
            "ELSE NULL END))";
    }

    /// <summary>
    /// Builds the SQL expression representing a regular expression match
    /// for field <paramref name="name"/> with the specified
    /// <paramref name="pattern"/>.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="pattern">The pattern.</param>
    /// <returns>SQL code.</returns>
    /// <exception cref="ArgumentNullException">name or pattern</exception>
    public string BuildRegexMatch(string name, string pattern)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(pattern);

        return $"{name} ~ {SqlEncode(pattern, false, true)}";
    }

    /// <summary>
    /// Builds the SQL expression representing a fuzzy match for field
    /// <paramref name="name"/> with the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value, eventually including a suffix
    /// introduced by <c>:</c> with the minimum treshold value.</param>
    /// <returns>SQL code.</returns>
    public string BuildFuzzyMatch(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        var t = ParseFuzzyValue(value);
        string ev = SqlEncode(t.Item1);
        return $"similarity({name},'{ev}') >= " +
            t.Item2.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the name of the lexer function with the specified ID in the
    /// SQL database.
    /// </summary>
    /// <param name="op">The lexer operator ID.</param>
    /// <returns>Function name, or null if none.</returns>
    public string? GetLexerFnName(int op)
    {
        return op switch
        {
            pythiaLexer.NEAR or pythiaLexer.NOTNEAR => "pyt_is_near_within",
            pythiaLexer.BEFORE or pythiaLexer.NOTBEFORE => "pyt_is_before_within",
            pythiaLexer.AFTER or pythiaLexer.NOTAFTER => "pyt_is_after_within",
            pythiaLexer.OVERLAPS or pythiaLexer.NOTOVERLAPS => "pyt_is_overlap_within",
            pythiaLexer.INSIDE or pythiaLexer.NOTINSIDE => "pyt_is_inside_within",
            pythiaLexer.LALIGN or pythiaLexer.NOTLALIGN => "pyt_is_left_aligned",
            pythiaLexer.RALIGN or pythiaLexer.NOTRALIGN => "pyt_is_right_aligned",
            _ => null,
        };
    }
}
