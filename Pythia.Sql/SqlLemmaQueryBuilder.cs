using Pythia.Core;
using System;
using System.Text;

namespace Pythia.Sql;

/// <summary>
/// SQL-based lemma query builder.
/// </summary>
/// <seealso cref="ISqlLemmaQueryBuilder" />
public sealed class SqlLemmaQueryBuilder(ISqlHelper sqlHelper) :
    SqlLemmaQueryBuilderBase(sqlHelper), ISqlLemmaQueryBuilder
{
    private string BuildQuery(LemmaFilter filter, bool count, string clauses)
    {
        StringBuilder sb = new();

        if (count)
        {
            sb.Append("SELECT COUNT(id) FROM lemma\n");

            if (clauses.Length > 0) sb.Append("WHERE\n").Append(clauses);
        }
        else
        {
            sb.Append("SELECT id, value, reversed_value,\n" +
                "language, count\n" +
                "FROM lemma\n");

            if (clauses.Length > 0) sb.Append("WHERE\n").Append(clauses);

            sb.Append("ORDER BY ");
            switch (filter.SortOrder)
            {
                case WordSortOrder.ByCount:
                    sb.Append("count");
                    break;
                case WordSortOrder.ByReversedValue:
                    sb.Append("reversed_value");
                    break;
                default:
                    sb.Append("value");
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
    public Tuple<string, string> Build(LemmaFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        string clauses = GetWhereSql(filter);

        return Tuple.Create(
            BuildQuery(filter, false, clauses),
            BuildQuery(filter, true, clauses));
    }
}
