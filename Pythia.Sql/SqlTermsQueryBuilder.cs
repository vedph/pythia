using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pythia.Core;

namespace Pythia.Sql
{
    /// <summary>
    /// SQL terms query builder. A terms query targets tokens and their
    /// occurrences.
    /// </summary>
    public sealed class SqlTermsQueryBuilder
    {
        private static readonly char[] _wildcards = {'_', '%'};
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
            IEnumerable<Tuple<string, string>> attributes, string table,
            StringBuilder sb)
        {
            int n = 0;
            foreach (Tuple<string, string> t in attributes)
            {
                if (++n > 1) sb.Append("AND ");

                sb.Append("EXISTS(\n")
                  .Append(" SELECT 1 FROM ").Append(table).Append("_attribute a\n")
                  .Append(" WHERE a.document_id=occurrence.document_id\n")
                  .Append(" AND a.name='")
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

            if (filter.HasDocumentFilters())
            {
                sb.Append("INNER JOIN document ON " +
                   "occurrence.document_id=document.id\n");
            }

            if (!string.IsNullOrEmpty(filter.ValuePattern))
            {
                sb.Append("INNER JOIN token ON occurrence.token_id=token.id\n");
            }

            if (filter.DocumentAttributes?.Any() == true)
            {
                sb.Append("INNER JOIN document_attribute " +
                          "ON occurrence.document_id=document_attribute.document_id\n");
            }

            if (filter.TokenAttributes?.Any() == true)
            {
                sb.Append("INNER JOIN occurrence_attribute " +
                          "ON occurrence.document_id=token_attribute.document_id");
            }

            if (!string.IsNullOrEmpty(filter.CorpusId))
            {
                sb.Append("INNER JOIN document_corpus " +
                          "ON token.document_id=document_corpus.document_id\n");
            }

            return sb.ToString();
        }

        private Tuple<string,string> BuildClauses(TermFilter filter)
        {
            // WHERE clauses
            StringBuilder sb = new();

            int clause = 0;
            if (!string.IsNullOrEmpty(filter.CorpusId))
            {
                AppendClausePrefix(++clause, sb);
                sb.Append("document_corpus.corpus_id=")
                  .Append(_sqlHelper.SqlEncode(filter.CorpusId, false, true))
                  .Append('\n');
            }

            if (!string.IsNullOrEmpty(filter.Author))
            {
                AppendClausePrefix(++clause, sb);
                sb.Append("document.author LIKE ").Append("'%")
                  .Append(_sqlHelper.SqlEncode(filter.Author)).Append("%'\n");
            }

            if (!string.IsNullOrEmpty(filter.Title))
            {
                AppendClausePrefix(++clause, sb);
                sb.Append("document.title LIKE ")
                  .Append("'%").Append(_sqlHelper.SqlEncode(filter.Title))
                  .Append("%'\n");
            }

            if (!string.IsNullOrEmpty(filter.Source))
            {
                AppendClausePrefix(++clause, sb);
                sb.Append("document.source LIKE ")
                  .Append("'%").Append(_sqlHelper.SqlEncode(filter.Source))
                  .Append("%'\n");
            }

            if (!string.IsNullOrEmpty(filter.ProfileId))
            {
                AppendClausePrefix(++clause, sb);
                sb.Append("document.profile_id=").Append('\'')
                  .Append(_sqlHelper.SqlEncode(filter.ProfileId)).Append("'\n");
            }

            if (filter.MinDateValue != 0)
            {
                AppendClausePrefix(++clause, sb);
                sb.Append("document.date_value >= ").Append(filter.MinDateValue)
                  .Append('\n');
            }

            if (filter.MaxDateValue != 0)
            {
                AppendClausePrefix(++clause, sb);
                sb.Append("document.date_value <= ").Append(filter.MaxDateValue)
                  .Append('\n');
            }

            if (filter.MinTimeModified.HasValue)
            {
                AppendClausePrefix(++clause, sb);
                sb.Append("document.last_modified >= ")
                  .Append(_sqlHelper.SqlEncode(filter.MinTimeModified.Value, false))
                  .Append('\n');
            }

            if (filter.MaxTimeModified.HasValue)
            {
                AppendClausePrefix(++clause, sb);
                sb.Append("document.last_modified <= ")
                  .Append(_sqlHelper.SqlEncode(filter.MaxTimeModified.Value, false))
                  .Append('\n');
            }

            if (filter.DocumentAttributes?.Any() == true)
            {
                AppendClausePrefix(++clause, sb);
                BuildAttributeClauses(filter.DocumentAttributes,
                    "document_attribute", sb);
            }

            if (!string.IsNullOrEmpty(filter.ValuePattern))
            {
                AppendClausePrefix(++clause, sb);
                bool hasWildcards = filter.ValuePattern.IndexOfAny(_wildcards) > -1;
                sb.Append("token.value ").Append(hasWildcards ? "LIKE " : "=")
                  .Append(_sqlHelper.SqlEncode(filter.ValuePattern, hasWildcards, true));
            }

            if (filter.TokenAttributes?.Any() == true)
            {
                AppendClausePrefix(++clause, sb);
                BuildAttributeClauses(filter.TokenAttributes,
                    "occurrence_attribute", sb);
            }

            // HAVING clauses, if any, must come after the GROUP BY statement
            string having = "";
            if (filter.MinCount > 0)
            {
                if (filter.MaxCount > 0)
                {
                    having = $"\nHAVING oc >= {filter.MinCount} " +
                             $"AND oc <= {filter.MaxCount}";
                } // min&max
                else having = $"\nHAVING oc >= {filter.MinCount} ";
            } // min
            else if (filter.MaxCount > 0)
            {
                having = $"\nHAVING oc <= {filter.MaxCount} ";
            } // max

            return Tuple.Create(sb.ToString(), having);
        }

        private string BuildQuery(TermFilter filter, bool count, string joins,
            string clauses, string having)
        {
            string dataQueryBody =
                "SELECT token.id, token.value, " +
                "(SELECT COUNT(o.id) FROM occurrence o " +
                "WHERE o.token_id=token.id) AS oc\n" +
                "FROM token\n" +
                joins +
                clauses + (string.IsNullOrWhiteSpace(clauses) ? "" : "\n") +
                "GROUP BY token.id" +
                having;

            // count-only query
            if (count)
            {
                // when no filtering is required, just count the unique value's
                if (string.IsNullOrWhiteSpace(clauses)
                    && string.IsNullOrWhiteSpace(having))
                {
                    return "SELECT COUNT(*) FROM token";
                }

                // else count the results of the data query
                return "SELECT COUNT(sub.value) FROM\n(\n" +
                       dataQueryBody +
                       ") AS sub";
            }

            // data query
            StringBuilder sb = new(dataQueryBody);

            sb.Append("\nORDER BY ");
            switch (filter.SortOrder)
            {
                case TermSortOrder.ByCount:
                    sb.Append("oc");
                    break;
                case TermSortOrder.ByReversedValue:
                    sb.Append("REVERSE(token.value)");
                    break;
                default:
                    sb.Append("token.value");
                    break;
            }
            if (filter.IsSortDescending) sb.Append(" DESC");
            sb.AppendLine();

            sb.Append(_sqlHelper.BuildPaging(
                filter.GetSkipCount(), filter.PageSize));

            return sb.ToString();
        }

        /// <summary>
        /// Builds the query corresponding to the specified filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// The query
        /// </returns>
        /// <exception cref="ArgumentNullException">filter</exception>
        public Tuple<string, string> Build(TermFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            string joins = GetJoinSql(filter);
            var clauses = BuildClauses(filter);

            return Tuple.Create(
                BuildQuery(filter, false, joins, clauses.Item1, clauses.Item2),
                BuildQuery(filter, true, joins, clauses.Item1, clauses.Item2));
        }
    }
}
