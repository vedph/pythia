using Pythia.Core;
using System;
using System.Text;

namespace Pythia.Sql;

/// <summary>
/// Base class for lemma or word SQL code builder.
/// </summary>
public abstract class SqlLemmaQueryBuilderBase(ISqlHelper sqlHelper)
{
    private static readonly char[] _wildcards = { '_', '%' };

    /// <summary>
    /// The SQL helper.
    /// </summary>
    protected readonly ISqlHelper SqlHelper = sqlHelper ??
            throw new ArgumentNullException(nameof(sqlHelper));

    private static void AppendClausePrefix(int clause, StringBuilder sb)
    {
        if (clause > 1) sb.Append("AND\n");
    }

    /// <summary>
    /// Gets the SQL code for the WHERE clauses.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>SQL code.</returns>
    protected string GetWhereSql(LemmaFilter filter)
    {
        // WHERE clauses
        StringBuilder sb = new();

        int clause = 0;

        // value
        if (!string.IsNullOrEmpty(filter.ValuePattern))
        {
            AppendClausePrefix(++clause, sb);
            bool hasWildcards = filter.ValuePattern.IndexOfAny(_wildcards) > -1;
            sb.Append("value ").Append(hasWildcards ? "LIKE " : "= ")
              .Append(SqlHelper.SqlEncode(filter.ValuePattern, hasWildcards, true))
              .Append('\n');
        }

        // value min len
        if (filter.MinValueLength > 0)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("LENGTH(value) >= ").Append(filter.MinValueLength)
                .Append('\n');
        }

        // value max len
        if (filter.MaxValueLength > 0)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("LENGTH(value) <= ").Append(filter.MaxValueLength)
                .Append('\n');
        }

        // min count
        if (filter.MinCount > 0)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("count >= ").Append(filter.MinCount).Append('\n');
        }

        // max count
        if (filter.MaxCount > 0)
        {
            AppendClausePrefix(clause, sb);
            sb.Append("count <= ").Append(filter.MaxCount).Append('\n');
        }

        return sb.ToString();
    }
}
