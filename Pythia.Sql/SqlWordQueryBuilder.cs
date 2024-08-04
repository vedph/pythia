using System;
using System.Text;
using Pythia.Core;

namespace Pythia.Sql;

/// <summary>
/// SQL word query builder.
/// </summary>
/// <param name="sqlHelper">The SQL helper to use.</param>
public sealed class SqlWordQueryBuilder(ISqlHelper sqlHelper) :
    SqlLemmaQueryBuilderBase(sqlHelper), ISqlWordQueryBuilder
{
    private string BuildQuery(WordFilter filter, bool count, string clauses)
    {
        StringBuilder sb = new();

        if (count)
        {
            sb.Append("SELECT COUNT(word.id) FROM word\n");

            if (clauses.Length > 0) sb.Append("WHERE\n").Append(clauses);
        }
        else
        {
            sb.Append("SELECT word.id, word.lemma_id,\n" +
                "word.value, word.reversed_value,\n" +
                "word.language, word.pos, word.lemma, word.count\n" +
                "FROM word\n");

            if (clauses.Length > 0) sb.Append("WHERE\n").Append(clauses);

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

            sb.Append(SqlHelper.BuildPaging(
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

        StringBuilder clauses = new(GetWhereSql(filter));
        bool hasClauses = clauses.Length > 0;

        // add word-specific filters
        if (filter.LemmaId.HasValue)
        {
            if (hasClauses) clauses.Append("AND ");
            clauses.Append($"lemma_id={filter.LemmaId.Value}\n");
            hasClauses = true;
        }

        if (filter.Pos != null)
        {
            if (hasClauses) clauses.Append("AND ");
            clauses.Append($"pos='{SqlHelper.SqlEncode(filter.Pos)}'\n");
        }

        return Tuple.Create(
            BuildQuery(filter, false, clauses.ToString()),
            BuildQuery(filter, true, clauses.ToString()));
    }
}
