using System;
using System.Text;
using Pythia.Core;

namespace Pythia.Sql;

/// <summary>
/// SQL word query builder.
/// </summary>
/// <param name="sqlHelper">The SQL helper to use.</param>
public sealed class SqlWordQueryBuilder(ISqlHelper sqlHelper) :
    ISqlWordQueryBuilder
{
    private static readonly char[] _wildcards = { '_', '%' };

    private readonly ISqlHelper _sqlHelper = sqlHelper ??
            throw new ArgumentNullException(nameof(sqlHelper));

    private static void AppendClausePrefix(int clause, StringBuilder sb)
    {
        sb.Append(clause == 1 ? "WHERE\n" : "AND\n");
    }

    private string GetWhereSql(WordFilter filter)
    {
        // WHERE clauses
        StringBuilder sb = new();

        int clause = 0;

        // span value
        if (!string.IsNullOrEmpty(filter.ValuePattern))
        {
            AppendClausePrefix(++clause, sb);
            bool hasWildcards = filter.ValuePattern.IndexOfAny(_wildcards) > -1;
            sb.Append("word.value ").Append(hasWildcards ? "LIKE " : "= ")
              .Append(_sqlHelper.SqlEncode(filter.ValuePattern, hasWildcards, true))
              .Append('\n');
        }

        // value min len
        if (filter.MinValueLength > 0)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("LENGTH(word.value) >= ").Append(filter.MinValueLength)
                .Append('\n');
        }

        // value max len
        if (filter.MaxValueLength > 0)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("LENGTH(word.value) <= ").Append(filter.MaxValueLength)
                .Append('\n');
        }

        // min count
        if (filter.MinCount > 0)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("word.count >= ").Append(filter.MinCount).Append('\n');
        }

        // max count
        if (filter.MaxCount > 0)
        {
            AppendClausePrefix(clause, sb);
            sb.Append("word.count <= ").Append(filter.MaxCount).Append('\n');
        }

        return sb.ToString();
    }

    private string BuildQuery(WordFilter filter, bool count, string clauses)
    {
        StringBuilder sb = new();

        if (count)
        {
            sb.Append("SELECT COUNT(word.id) FROM word\n");

            if (clauses.Length > 0) sb.Append(clauses);
        }
        else
        {
            sb.Append("SELECT word.id, word.lemma_id,\n" +
                "word.value, word.reversed_value,\n" +
                "word.language, word.pos, word.lemma, word.count\n" +
                "FROM word\n");

            if (clauses.Length > 0) sb.Append(clauses);

            sb.Append("ORDER BY ");
            switch (filter.SortOrder)
            {
                case WordSortOrder.ByCount:
                    sb.Append("word.count");
                    break;
                case WordSortOrder.ByReversedValue:
                    sb.Append("word.reversed_value");
                    break;
                default:
                    sb.Append("word.value");
                    break;
            }
            if (filter.IsSortDescending) sb.Append(" DESC");
            sb.AppendLine();

            sb.Append(_sqlHelper.BuildPaging(
                filter.GetSkipCount(), filter.PageSize));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the SQL queries corresponding to the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>
    /// SQL queries for data and their total count.
    /// </returns>
    /// <exception cref="ArgumentNullException">filter</exception>
    public Tuple<string, string> Build(WordFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        string clauses = GetWhereSql(filter);

        return Tuple.Create(
            BuildQuery(filter, false, clauses),
            BuildQuery(filter, true, clauses));
    }
}
