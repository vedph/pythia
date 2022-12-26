using System;

namespace Pythia.Sql;

/// <summary>
/// SQL helper interface. This defines a set of helper methods for building
/// SQL code.
/// </summary>
public interface ISqlHelper
{
    /// <summary>
    /// Encodes the specified date (or date and time) value for SQL.
    /// </summary>
    /// <param name="dt">The value.</param>
    /// <param name="time">if set to <c>true</c>, include the time.</param>
    /// <returns>SQL-encoded value</returns>
    string SqlEncode(DateTime dt, bool time);

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
    /// option is meaningful only when <paramref name="wrapInQuotes"/> is
    /// true.</param>
    /// <returns>SQL-encoded value</returns>
    string SqlEncode(string text, bool hasWildcards = false,
        bool wrapInQuotes = false, bool unicode = true);

    /// <summary>
    /// Escapes the specified keyword.
    /// </summary>
    /// <param name="keyword">The keyword.</param>
    /// <returns>Escaped keyword</returns>
    string EscapeKeyword(string keyword);

    /// <summary>
    /// Builds the paging expression with the specified values.
    /// </summary>
    /// <param name="offset">The offset count.</param>
    /// <param name="limit">The limit count.</param>
    /// <returns>SQL code.</returns>
    string BuildPaging(int offset, int limit);

    /// <summary>
    /// Builds the SQL code required to represent the text field represented
    /// by <paramref name="name"/> as an integer number, when this cast
    /// is applicable.
    /// </summary>
    /// <param name="name">The expression name.</param>
    /// <returns>SQL code.</returns>
    string BuildTextAsNumber(string name);

    /// <summary>
    /// Builds the SQL expression representing a regular expression match
    /// for field <paramref name="name"/> with the specified
    /// <paramref name="pattern"/>.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="pattern">The pattern.</param>
    /// <returns>SQL code.</returns>
    string BuildRegexMatch(string name, string pattern);

    /// <summary>
    /// Builds the SQL expression representing a fuzzy match for field
    /// <paramref name="name"/> with the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value, eventually including a suffix
    /// introduced by <c>:</c> with the minimum treshold value.</param>
    /// <returns>SQL code.</returns>
    string BuildFuzzyMatch(string name, string value);

    /// <summary>
    /// Gets the name of the lexer function with the specified ID in the
    /// SQL database.
    /// </summary>
    /// <param name="op">The lexer operator ID.</param>
    /// <returns>Function name, or null if none.</returns>
    string? GetLexerFnName(int op);
}
