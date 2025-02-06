using Pythia.Core;
using System;
using System.Linq;
using System.Text;

namespace Pythia.Sql;

/// <summary>
/// Base class for lemma or word SQL code builder.
/// </summary>
public abstract class SqlLemmaQueryBuilderBase(ISqlHelper sqlHelper)
{
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
            int wildCount = filter.ValuePattern.Count(c => c == '_' || c == '%');

            if (wildCount > 0)
            {
                // special case: when pattern is %... with a single wildcard,
                // we can use the reversed value like ...%
                if (wildCount == 1 && filter.ValuePattern.StartsWith('%'))
                {
                    char[] a = filter.ValuePattern[1..].ToCharArray();
                    Array.Reverse(a);
                    string reversed = new(a);

                    sb.Append("reversed_value ").Append("LIKE ")
                      .Append(SqlHelper.SqlEncode($"{reversed}%", true, true))
                      .Append('\n');
                }
                else
                {
                    sb.Append("value LIKE ")
                      .Append(SqlHelper.SqlEncode(filter.ValuePattern, true, true))
                      .Append('\n');
                }
            }
            else
            {
                sb.Append("value = ")
                  .Append(SqlHelper.SqlEncode(filter.ValuePattern, false, true))
                  .Append('\n');
            }
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
            AppendClausePrefix(++clause, sb);
            sb.Append("count <= ").Append(filter.MaxCount).Append('\n');
        }

        // pos
        if (!string.IsNullOrEmpty(filter.Pos))
        {
            AppendClausePrefix(clause, sb);
            sb.Append("pos = ")
              .Append(SqlHelper.SqlEncode(filter.Pos, false, true))
              .Append('\n');
        }

        return sb.ToString();
    }
}
