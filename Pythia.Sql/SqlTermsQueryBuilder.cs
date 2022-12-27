using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pythia.Core;

namespace Pythia.Sql;

/// <summary>
/// SQL terms query builder. A terms query targets tokens and their
/// occurrences.
/// </summary>
public sealed class SqlTermsQueryBuilder : ISqlTermsQueryBuilder
{
    private static readonly char[] _wildcards = { '_', '%' };
    private readonly ISqlHelper _sqlHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTermsQueryBuilder"/>
    /// class.
    /// </summary>
    /// <param name="sqlHelper">The SQL helper to use.</param>
    public SqlTermsQueryBuilder(ISqlHelper sqlHelper)
    {
        _sqlHelper = sqlHelper ??
            throw new ArgumentNullException(nameof(sqlHelper));
    }

    private static void AppendClausePrefix(int clause, StringBuilder sb)
    {
        sb.Append(clause == 1 ? "WHERE\n" : "AND\n");
    }

    private void BuildAttributeClauses(
        IEnumerable<Tuple<string, string>> attributes, bool occurrence,
        StringBuilder sb)
    {
        string prefix = occurrence ? "occurrence" : "document";
        int n = 0;
        foreach (Tuple<string, string> t in attributes)
        {
            if (++n > 1) sb.Append("AND ");

            if (occurrence)
            {
                if (!TermFilter.IsExtOccurrenceAttribute(t.Item1))
                {
                    sb.Append("o.").Append(t.Item1).Append(" LIKE '%")
                      .Append(t.Item2).Append("%'\n");
                    continue;
                }
            }
            else
            {
                if (!TermFilter.IsExtDocumentAttribute(t.Item1))
                {
                    sb.Append("d.").Append(t.Item1).Append(" LIKE '%")
                      .Append(t.Item2).Append("%'\n");
                    continue;
                }
            }

            sb.Append("EXISTS(\n")
              .Append(" SELECT 1 FROM ").Append(prefix).Append("_attribute a\n")
              .Append(" WHERE a.").Append(prefix).Append("_id = ")
              .Append(occurrence ? "o" : "d").Append(".id\n")
              .Append(" AND a.name = '")
              .Append(_sqlHelper.SqlEncode(t.Item1))
              .Append("'\n");

            if (!string.IsNullOrEmpty(t.Item2))
            {
                sb.Append(" AND a.value LIKE '%").Append(t.Item2).Append("%'\n");
            }

            sb.Append(")\n");
        }
    }

    private static string GetJoinSql(TermFilter filter)
    {
        StringBuilder sb = new();
        bool joinOcc = false;

        // document
        if (filter.HasDocumentFilters())
        {
            joinOcc = true;
            sb.Append("INNER JOIN document d ON o.document_id = d.id\n");
        }

        // document attrs (unless all are intrinsic)
        if (filter.HasExtDocumentAttributes())
        {
            joinOcc = true;
            sb.Append("INNER JOIN document_attribute da " +
                      "ON o.document_id = da.document_id\n");
        }

        // occurrence attrs
        if (filter.HasExtOccurrenceAttributes())
        {
            joinOcc = true;
            sb.Append("INNER JOIN occurrence_attribute oa " +
                      "ON o.id = oa.occurrence_id\n");
        }

        // corpus
        if (!string.IsNullOrEmpty(filter.CorpusId))
        {
            joinOcc = true;
            sb.Append("INNER JOIN document_corpus dc " +
                      "ON o.document_id = dc.document_id\n");
        }

        if (joinOcc)
        {
            sb.Insert(0, "INNER JOIN occurrence o ON toc.id = o.token_id\n");
        }

        return sb.ToString();
    }

    private string GetWhereSql(TermFilter filter)
    {
        // WHERE clauses
        StringBuilder sb = new();

        int clause = 0;

        // corpus ID
        if (!string.IsNullOrEmpty(filter.CorpusId))
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("dc.corpus_id=")
              .Append(_sqlHelper.SqlEncode(filter.CorpusId, false, true))
              .Append('\n');
        }

        // author
        if (!string.IsNullOrEmpty(filter.Author))
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("d.author LIKE ").Append("'%")
              .Append(_sqlHelper.SqlEncode(filter.Author)).Append("%'\n");
        }

        // title
        if (!string.IsNullOrEmpty(filter.Title))
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("d.title LIKE ")
              .Append("'%").Append(_sqlHelper.SqlEncode(filter.Title))
              .Append("%'\n");
        }

        // source
        if (!string.IsNullOrEmpty(filter.Source))
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("d.source LIKE ")
              .Append("'%").Append(_sqlHelper.SqlEncode(filter.Source))
              .Append("%'\n");
        }

        // profile ID
        if (!string.IsNullOrEmpty(filter.ProfileId))
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("d.profile_id=").Append('\'')
              .Append(_sqlHelper.SqlEncode(filter.ProfileId)).Append("'\n");
        }

        // min date value
        if (filter.MinDateValue != 0)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("d.date_value >= ").Append(filter.MinDateValue)
              .Append('\n');
        }

        // max date value
        if (filter.MaxDateValue != 0)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("d.date_value <= ").Append(filter.MaxDateValue)
              .Append('\n');
        }

        // min time modified
        if (filter.MinTimeModified.HasValue)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("d.last_modified >= ")
              .Append(_sqlHelper.SqlEncode(filter.MinTimeModified.Value, false))
              .Append('\n');
        }

        // max time modified
        if (filter.MaxTimeModified.HasValue)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("d.last_modified <= ")
              .Append(_sqlHelper.SqlEncode(filter.MaxTimeModified.Value, false))
              .Append('\n');
        }

        // document attrs
        if (filter.DocumentAttributes?.Any() == true)
        {
            AppendClausePrefix(++clause, sb);
            BuildAttributeClauses(filter.DocumentAttributes, false, sb);
        }

        // token value
        if (!string.IsNullOrEmpty(filter.ValuePattern))
        {
            AppendClausePrefix(++clause, sb);
            bool hasWildcards = filter.ValuePattern.IndexOfAny(_wildcards) > -1;
            sb.Append("toc.value ").Append(hasWildcards ? "LIKE " : "= ")
              .Append(_sqlHelper.SqlEncode(filter.ValuePattern, hasWildcards, true))
              .Append('\n');
        }

        // value min len
        if (filter.MinValueLength > 0)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("LENGTH(toc.value) >= ").Append(filter.MinValueLength)
                .Append('\n');
        }

        // value max len
        if (filter.MaxValueLength > 0)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("LENGTH(toc.value) <= ").Append(filter.MaxValueLength)
                .Append('\n');
        }

        // occurrence attrs
        if (filter.OccurrenceAttributes?.Any() == true)
        {
            AppendClausePrefix(++clause, sb);
            BuildAttributeClauses(filter.OccurrenceAttributes, true, sb);
        }

        // min count
        if (filter.MinCount > 0)
        {
            AppendClausePrefix(++clause, sb);
            sb.Append("toc.count >= ").Append(filter.MinCount).Append('\n');
        }

        // max count
        if (filter.MaxCount > 0)
        {
            AppendClausePrefix(clause, sb);
            sb.Append("toc.count <= ").Append(filter.MaxCount).Append('\n');
        }

        return sb.ToString();
    }

    private string BuildQuery(TermFilter filter, bool count, string joins,
        string clauses)
    {
        StringBuilder sb = new();
        sb.Append("SELECT DISTINCT toc.id, toc.value, toc.count\n" +
            "FROM token_occurrence_count toc\n");
        if (joins.Length > 0) sb.Append(joins);
        if (clauses.Length > 0) sb.Append(clauses);

        string dataQueryBody = sb.ToString();

        // count-only query
        if (count)
        {
            // when no filtering is required, just count the unique value's
            if (string.IsNullOrWhiteSpace(clauses))
            {
                return "SELECT COUNT(*) FROM token";
            }

            // else count the results of the data query
            return "SELECT COUNT(sub.value) FROM\n(\n" +
                   dataQueryBody +
                   ") AS sub";
        }

        // data query
        sb.Append("ORDER BY ");
        switch (filter.SortOrder)
        {
            case TermSortOrder.ByCount:
                sb.Append("toc.count");
                break;
            case TermSortOrder.ByReversedValue:
                sb.Append("REVERSE(toc.value)");
                break;
            default:
                sb.Append("toc.value");
                break;
        }
        if (filter.IsSortDescending) sb.Append(" DESC");
        sb.AppendLine();

        sb.Append(_sqlHelper.BuildPaging(
            filter.GetSkipCount(), filter.PageSize));

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
    public Tuple<string, string> Build(TermFilter filter)
    {
        if (filter == null) throw new ArgumentNullException(nameof(filter));

        string joins = GetJoinSql(filter);
        string clauses = GetWhereSql(filter);

        return Tuple.Create(
            BuildQuery(filter, false, joins, clauses),
            BuildQuery(filter, true, joins, clauses));
    }
}
