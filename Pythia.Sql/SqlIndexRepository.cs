using System.Linq;
using Corpus.Core;
using Corpus.Sql;
using Fusi.Tools.Data;
using Microsoft.Extensions.Caching.Memory;
using Pythia.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Pythia.Core.Analysis;

namespace Pythia.Sql;

/// <summary>
/// SQL-based index repository.
/// </summary>
/// <seealso cref="SqlCorpusRepository" />
/// <seealso cref="IIndexRepository" />
public abstract class SqlIndexRepository : SqlCorpusRepository,
    IIndexRepository
{
    private readonly MemoryCache _tokenCache;
    private readonly MemoryCacheEntryOptions _tokenCacheOptions;

    /// <summary>
    /// Gets the corpus repository.
    /// </summary>
    protected ICorpusRepository CorpusRepository { get; }

    /// <summary>
    /// Gets the SQL helper.
    /// </summary>
    protected ISqlHelper SqlHelper { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlIndexRepository"/>
    /// class.
    /// </summary>
    /// <param name="sqlHelper">The SQL helper.</param>
    /// <param name="corpusRepository">The corpus repository.</param>
    /// <exception cref="ArgumentNullException">sqlHelper</exception>
    protected SqlIndexRepository(ISqlHelper sqlHelper,
        ICorpusRepository corpusRepository)
    {
        CorpusRepository = corpusRepository
            ?? throw new ArgumentNullException(nameof(corpusRepository));
        SqlHelper = sqlHelper
            ?? throw new ArgumentNullException(nameof(sqlHelper));

        // see https://michaelscodingspot.com/cache-implementations-in-csharp-net/
        _tokenCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 8192
        });
        _tokenCacheOptions = new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetPriority(CacheItemPriority.High)
            .SetSlidingExpiration(TimeSpan.FromSeconds(60))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(60 * 3));
    }

    /// <summary>
    /// Gets the schema representing the model for the corpus repository data.
    /// </summary>
    /// <returns>Schema.</returns>
    public abstract string GetSchema();

    /// <summary>
    /// Adds all the specified spans.
    /// </summary>
    /// <param name="spans">The spans.</param>
    /// <exception cref="ArgumentNullException">spans</exception>
    public void AddSpans(IEnumerable<TextSpan> spans)
    {
        ArgumentNullException.ThrowIfNull(spans);

        using IDbConnection connection = GetConnection();
        connection.Open();
        using var tr = connection.BeginTransaction();

        try
        {
            // prepare insert attr command
            DbCommand attrCmd = (DbCommand)connection.CreateCommand();
            attrCmd.CommandText = "INSERT INTO span_attribute(span_id, name," +
                "value, type)\n" +
                "VALUES(@span_id, @name, @value, @type);";
            AddParameter(attrCmd, "@span_id", DbType.Int32, 0);
            AddParameter(attrCmd, "@name", DbType.String, "");
            AddParameter(attrCmd, "@value", DbType.String, "");
            AddParameter(attrCmd, "@type", DbType.Int32, 0);

            // add each span
            foreach (TextSpan span in spans)
            {
                UpsertSpan(span, connection);

                // add span attributes
                if (span.Attributes?.Count > 0)
                {
                    foreach (Corpus.Core.Attribute attribute in span.Attributes)
                    {
                        attrCmd.Parameters["@span_id"].Value = span.Id;
                        attrCmd.Parameters["@name"].Value = attribute.Name;
                        attrCmd.Parameters["@value"].Value = attribute.Value;
                        attrCmd.Parameters["@type"].Value = (int)attribute.Type;
                        attrCmd.ExecuteNonQuery();
                    }
                }
            }

            tr.Commit();
        }
        catch (Exception ex)
        {
            tr.Rollback();
            Debug.WriteLine(ex.ToString());
            throw;
        }
    }

    /// <summary>
    /// Adds the specified attribute to all the tokens included in the
    /// specified range of the specified document. This is typically used
    /// when adding token attributes which come from structures
    /// encompassing them.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="start">The start position.</param>
    /// <param name="end">The end position (inclusive).</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <param name="type">The attribute type.</param>
    public void AddSpanAttributes(int documentId, int start, int end,
        string name, string value, AttributeType type)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        // get all the target IDs in the specified doc's range
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id FROM span\n" +
            "WHERE document_id=@document_id AND " +
            "p1 >= @start AND p2 <= @end;";
        AddParameter(cmd, "@document_id", DbType.Int32, documentId);
        AddParameter(cmd, "@start", DbType.Int32, start);
        AddParameter(cmd, "@end", DbType.Int32, end);

        List<int> ids = [];
        using (IDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read()) ids.Add(reader.GetInt32(0));
        }

        // add the received attribute to each of these occurrences
        using IDbTransaction tr = connection.BeginTransaction();
        try
        {
            cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO span_attribute" +
                "(span_id, name, value, type)\n" +
                "VALUES(@span_id, @name, @value, @type);";
            AddParameter(cmd, "@span_id", DbType.Int32, 0);
            AddParameter(cmd, "@name", DbType.String, name);
            AddParameter(cmd, "@value", DbType.String, value);
            AddParameter(cmd, "@type", DbType.Int32, (int)type);

            foreach (int id in ids)
            {
                ((DbCommand)cmd).Parameters["@span_id"].Value = id;
                cmd.ExecuteNonQuery();
            }
            tr.Commit();
        }
        catch (Exception ex)
        {
            tr.Rollback();
            Debug.WriteLine(ex.ToString());
            throw;
        }
    }

    /// <summary>
    /// Deletes all the tokens of the document with the specified ID.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="type">The span type or null to delete any spans.</param>
    public void DeleteDocumentSpans(int documentId, string? type = null)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        IDbCommand cmd = connection.CreateCommand();
        if (type != null)
        {
            cmd.CommandText = "DELETE FROM span\n" +
                "WHERE document_id=@document_id AND type=@type;";
            AddParameter(cmd, "@document_id", DbType.Int32, documentId);
            AddParameter(cmd, "@type", DbType.String, type);
        }
        else
        {
            cmd.CommandText = "DELETE FROM span\n" +
                "WHERE document_id=@document_id;";
            AddParameter(cmd, "@document_id", DbType.Int32, documentId);
        }
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Gets the range of span positions starting from the specified
    /// range of span character indexes. This is used by structure
    /// parsers, which often must determine positional ranges starting
    /// from character indexes.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="endIndex">The end index.</param>
    /// <returns>range or null</returns>
    public Tuple<int, int>? GetPositionRange(int documentId,
        int startIndex, int endIndex)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT MIN(p1), MAX(p2) FROM span\n" +
            "WHERE document_id=@document_id AND " +
            $"type='{TextSpan.TYPE_TOKEN}' AND " +
            "index >= @start_index AND index <= @end_index;";
        AddParameter(cmd, "@document_id", DbType.Int32, documentId);
        AddParameter(cmd, "@start_index", DbType.Int32, startIndex);
        AddParameter(cmd, "@end_index", DbType.Int32, endIndex);

        using IDataReader reader = cmd.ExecuteReader();
        // can be null when the target element contains no tokens
        // (e.g. an empty paragraph)
        if (!reader.Read() || reader.IsDBNull(0)) return null;

        return Tuple.Create(reader.GetInt32(0), reader.GetInt32(1));
    }

    private static void CollectAttributesStats(IDbConnection connection,
        string tableName, IDictionary<string, double> stats)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name, " +
            $"COUNT(id) FROM {tableName} GROUP BY name;";

        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
            stats["@" + reader.GetString(0)] = reader.GetDouble(1);
    }

    private static int GetCount(IDbConnection connection, string tableName)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableName};";
        long? result = cmd.ExecuteScalar() as long?;
        if (result == null) return 0;
        return (int)result;
    }

    /// <summary>
    /// Gets the counts from the specified SQL query, which is like:
    /// <code>
    /// SELECT s.type, COUNT(s.id)
    /// FROM span
    /// GROUP BY "type"
    /// ORDER BY "type";
    /// </code>.
    /// The first column is used as key, the second as value.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="sql">The SQL code.</param>
    /// <returns>The counts dictionary.</returns>
    private static Dictionary<string, int> GetCounts(IDbConnection connection,
        string sql)
    {
        Dictionary<string, int> counts = [];

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
            counts[reader.GetString(0)] = reader.GetInt32(1);

        return counts;
    }

    private static void AddRatio(string dividend, string divisor,
        Dictionary<string, double> stats)
    {
        if (stats.ContainsKey(dividend) && stats.ContainsKey(divisor)
            && stats[divisor] > 0)
        {
            stats[dividend + "_ratio"] = stats[dividend] / stats[divisor];
        }
    }

    /// <summary>
    /// Gets statistics about the index.
    /// </summary>
    /// <returns>Dictionary with statistics.</returns>
    public IDictionary<string, double> GetStatistics()
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        Dictionary<string, double> stats = [];
        CollectAttributesStats(connection, "document_attribute", stats);
        CollectAttributesStats(connection, "span_attribute", stats);
        stats["corpus_count"] = GetCount(connection, "corpus");
        stats["document_count"] = GetCount(connection, "document");
        stats["document_attribute_count"] =
            GetCount(connection, "document_attribute");
        stats["profile_count"] = GetCount(connection, "profile");
        stats["span_count"] = GetCount(connection, "span");

        // add span grouped counts from query
        Dictionary<string, int> counts = GetCounts(connection,
            "SELECT type, COUNT(id) FROM span " +
            "GROUP BY span.type " +
            "ORDER BY span.type;");
        foreach (KeyValuePair<string, int> pair in counts)
            stats["span." + pair.Key] = pair.Value;

        // calculated values
        AddRatio("document_attribute_count", "document_count", stats);
        AddRatio("span_attribute_count", "span_count", stats);

        return stats;
    }

    private static string BuildKwicSql(IList<SearchResult> results,
        int contextSize)
    {
        StringBuilder sb = new();

        foreach (SearchResult result in results)
        {
            if (sb.Length > 0) sb.AppendLine("UNION");

            int min = result.P1 - contextSize;
            int max = result.P2 + contextSize;

            // get left and right tokens
            sb.AppendFormat("SELECT document_id, p1, value, text\n" +
                "FROM span\n" +
                "WHERE span.type='{0}' " +
                "AND document_id={1} " +
                "AND p1 >= {2} AND p2 <= {3}\n",
                TextSpan.TYPE_TOKEN,
                result.DocumentId,
                min, max);
        }

        sb.AppendLine("ORDER BY document_id, p1");
        return sb.ToString();
    }

    private static KwicSearchResult CreateKwicSearchResult(
        SearchResult result, IList<KwicPart> parts, int contextSize)
    {
        // left
        List<string> left = (from p in parts
                             where p.Position < result.P1
                             orderby p.Position
                             select p.Value).ToList();
        // pad
        if (left.Count < contextSize)
        {
            left.InsertRange(0,
               Enumerable.Repeat("", contextSize - left.Count));
        }

        // right
        List<string> right = (from p in parts
                              where p.Position > result.P1
                              orderby p.Position
                              select p.Value).ToList();
        // pad
        if (right.Count < contextSize)
        {
            right.AddRange(
               Enumerable.Repeat("", contextSize - right.Count));
        }

        return new KwicSearchResult(result)
        {
            LeftContext = [.. left],
            RightContext = [.. right]
        };
    }

    /// <summary>
    /// Gets the context for the specified result(s).
    /// </summary>
    /// <param name="results">The results to get context for.</param>
    /// <param name="contextSize">Size of the context: e.g. if 5, you will
    /// get 5 tokens to the left and 5 to the right.</param>
    /// <returns>results with context</returns>
    /// <exception cref="ArgumentNullException">null results</exception>
    /// <exception cref="ArgumentOutOfRangeException">context size
    /// out of range (1-10)</exception>
    public IList<KwicSearchResult> GetResultContext(
        IList<SearchResult> results, int contextSize)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (contextSize < 1 || contextSize > 10)
            throw new ArgumentOutOfRangeException(nameof(contextSize));

        // nothing to do if no results
        if (results.Count == 0) return [];

        // collect all the KWIC parts
        using IDbConnection connection = GetConnection();
        connection.Open();
        string sql = BuildKwicSql(results, contextSize);
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        List<KwicPart> parts = [];
        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            parts.Add(new KwicPart
            {
                DocumentId = reader.GetInt32(0),
                Position = reader.GetInt32(1),
                Value = reader.GetString(2),
                Text = reader.GetString(3)
            });
        }

        // build KWIC
        List<KwicSearchResult> searchResults = [];
        int docId = parts[0].DocumentId,
            headPos = parts[0].HeadPosition,
            i = 1,
            start = 0;

        while (i < parts.Count)
        {
            if (docId != parts[i].DocumentId
                || headPos != parts[i].HeadPosition)
            {
                KwicSearchResult result = CreateKwicSearchResult(
                    results.First(r => r.DocumentId == docId
                                       && r.P1 == headPos),
                    parts.Skip(start).Take(i - start).ToList(),
                    contextSize);
                searchResults.Add(result);
                start = i;
                docId = parts[i].DocumentId;
                headPos = parts[i].HeadPosition;
            }
            i++;
        }

        if (start < i)
        {
            KwicSearchResult result = CreateKwicSearchResult(
                results.First(r => r.DocumentId == docId
                                   && r.P1 == headPos),
                parts.Skip(start).Take(i - start).ToList(),
                contextSize);
            searchResults.Add(result);
        }

        return searchResults;
    }

    /// <summary>
    /// Searches the index using the specified query.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <returns>results page</returns>
    /// <param name="literalFilters">The optional filters to apply to literal
    /// values in the query text.</param>
    /// <exception cref="ArgumentNullException">request</exception>
    /// <exception cref="ArgumentOutOfRangeException">page number
    /// or size out of allowed ranges</exception>
    public DataPage<SearchResult> Search(SearchRequest request,
        IList<ILiteralFilter>? literalFilters = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.PageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(request));
        if (request.PageSize < 1 || request.PageSize > 100)
            throw new ArgumentOutOfRangeException(nameof(request));

        SqlQueryBuilder builder = new(SqlHelper)
        {
            LiteralFilters = literalFilters
        };
        Tuple<string, string> t = builder.Build(request);
        if (t == null)
        {
            return new DataPage<SearchResult>(
                request.PageNumber, request.PageSize, 0, []);
        }

        using IDbConnection connection = GetConnection();
        connection.Open();

        // total
        IDbCommand totCmd = connection.CreateCommand();
        totCmd.CommandText = t.Item2;
        long? total = totCmd.ExecuteScalar() as long?;
        if (total == null || total.Value < 1)
        {
            return new DataPage<SearchResult>(
                request.PageNumber, request.PageSize, 0, []);
        }

        // results
        List<SearchResult> results = [];
        IDbCommand dataCmd = connection.CreateCommand();
        dataCmd.CommandText = t.Item1;
        using IDataReader reader = dataCmd.ExecuteReader();
        while (reader.Read())
        {
            int documentId = reader.GetInt32(reader.GetOrdinal("document_id"));

            results.Add(new SearchResult
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                DocumentId = documentId,
                P1 = reader.GetInt32(reader.GetOrdinal("p1")),
                P2 = reader.GetInt32(reader.GetOrdinal("p2")),
                Index = reader.GetInt32(reader.GetOrdinal("index")),
                Length = reader.GetInt16(reader.GetOrdinal("length")),
                Type = reader.GetString(reader.GetOrdinal("type")),
                Value = reader.GetString(reader.GetOrdinal("value")),
                Text = reader.GetString(reader.GetOrdinal("text")),
                Author = reader.GetString(reader.GetOrdinal("author")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                SortKey = reader.GetString(reader.GetOrdinal("sort_key"))
            });
        }

        return new DataPage<SearchResult>(
            request.PageNumber, request.PageSize, (int)total.Value, results);
    }

    /// <summary>
    /// Gets a page of index terms matching the specified filter.
    /// </summary>
    /// <param name="filter">filter</param>
    /// <returns>page</returns>
    /// <exception cref="ArgumentNullException">null filter</exception>
    public DataPage<IndexTerm> GetTerms(TermFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        ISqlTermsQueryBuilder builder = new SqlTermsQueryBuilder(SqlHelper);
        var t = builder.Build(filter);
        Debug.WriteLine($"-- Terms query:\n{t.Item1}");
        Debug.WriteLine($"-- Terms count:\n{t.Item2}\n");

        using IDbConnection connection = GetConnection();
        connection.Open();

        // total
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = t.Item2;
        long? total = cmd.ExecuteScalar() as long?;
        if (total == null || total.Value == 0)
        {
            return new DataPage<IndexTerm>(
                filter.PageNumber, filter.PageSize,
                0, new List<IndexTerm>());
        }

        // data
        List<IndexTerm> terms = new();
        cmd = connection.CreateCommand();
        cmd.CommandText = t.Item1;
        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            terms.Add(new IndexTerm
            {
                Id = reader.GetInt32(0),
                Value = reader.GetString(1),
                Count = reader.GetInt32(2)
            });
        }
        return new DataPage<IndexTerm>(
            filter.PageNumber, filter.PageSize, (int)total.Value, terms);
    }

    private static HashSet<string> GetTypedAttributeNames(bool occurrences,
        int type, IDbConnection connection)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT name FROM\n" +
            (occurrences ? "occurrence_attribute" : "document_attribute") +
            "\nWHERE type=" + type + ";";

        HashSet<string> names = new();
        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }
        return names;
    }

    private static long GetTermFrequency(int id, IDbConnection connection)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(o.id) AS freq\n" +
            $"FROM occurrence o WHERE o.token_id={id};";
        long? result = cmd.ExecuteScalar() as long?;
        return result ?? 0;
    }

    private static IDbCommand BuildTermDocCommand(
        int id, int limit, IDbConnection connection)
    {
        // select distinct da.value, count(da.value) as freq
        // from document d
        // inner join document_attribute da on d.id = da.document_id
        // inner join occurrence o on d.id = o.document_id
        // where da.name='giudicante' and o.token_id=469
        // group by da.value
        // order by freq desc
        // limit 10

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT da.value, count(da.value) as freq\n" +
            "FROM document d\n" +
            "INNER JOIN document_attribute da ON d.id = da.document_id\n" +
            "INNER JOIN occurrence o on d.id = o.document_id\n" +
            "WHERE da.name=@name AND o.token_id=@token_id\n" +
            "GROUP BY da.value ORDER BY freq DESC LIMIT @limit;";
        AddParameter(cmd, "@name", DbType.String);
        AddParameter(cmd, "@token_id", DbType.Int32, id);
        AddParameter(cmd, "@limit", DbType.Int32, limit);
        return cmd;
    }

    private static IDbCommand BuildTermDocCommand(
        int id, int limit, int interval, IDbConnection connection)
    {
        // select distinct concat(
        //  cast(cast(da.value as int) / 5 * 5 as varchar),
        //  '-',
        //  cast(cast(da.value as int) / 5 * 5 + 4 as varchar)
        // ),
        // count(da.value) as freq
        // from document d
        // inner join document_attribute da on d.id = da.document_id
        // inner join occurrence o on d.id = o.document_id
        // where da.name='nascita-avv' and o.token_id=10
        // group by cast(da.value as int) / 5
        // order by freq desc
        // limit 10

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT " +
            "CONCAT(\n" +
            $"CAST(CAST(da.value AS int) / {interval} * {interval} AS varchar),\n" +
            "'-',\n" +
            $"CAST(CAST(da.value AS int) / " +
            $"{interval} * {interval} + {interval - 1} AS varchar)\n" +
            "),\n" +
            "COUNT(da.value) as freq\n" +
            "FROM document d\n" +
            "INNER JOIN document_attribute da ON d.id = da.document_id\n" +
            "INNER JOIN occurrence o on d.id = o.document_id\n" +
            "WHERE da.name=@name AND o.token_id=@token_id\n" +
            $"GROUP BY CAST(da.value AS int) / {interval}\n" +
            "ORDER BY freq DESC LIMIT @limit;";
        AddParameter(cmd, "@name", DbType.String);
        AddParameter(cmd, "@token_id", DbType.Int32, id);
        AddParameter(cmd, "@limit", DbType.Int32, limit);
        return cmd;
    }

    private static IDbCommand BuildTermDocTotalCommand(int id,
        IDbConnection connection)
    {
        // total when exceeding limit:
        // select count(da.value) as freq
        // from document d
        // inner join document_attribute da on d.id = da.document_id
        // inner join occurrence o on d.id = o.document_id
        // where da.name='giudicante' and o.token_id=469

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(da.value) AS freq\n" +
            "FROM document d\n" +
            "INNER JOIN document_attribute da ON d.id = da.document_id\n" +
            "INNER JOIN occurrence o on d.id = o.document_id\n" +
            "WHERE da.name=@name AND o.token_id=@token_id;";
        AddParameter(cmd, "@name", DbType.String);
        AddParameter(cmd, "@token_id", DbType.Int32, id);
        return cmd;
    }

    private void GetDocTermDistributions(TermDistributionRequest request,
        TermDistributionSet set, IDbConnection connection)
    {
        HashSet<string> numericNames = GetTypedAttributeNames(false, 1, connection);
        using var totConnection = GetConnection();
        totConnection.Open();

        foreach (string attr in request.DocAttributes)
        {
            bool ranged = request.Interval > 1 && numericNames.Contains(attr);

            IDbCommand cmd = ranged
                ? BuildTermDocCommand(request.TermId, request.Limit,
                    request.Interval, connection)
                : BuildTermDocCommand(request.TermId, request.Limit + 1, connection);

            IDbCommand? totCmd = ranged
                ? null
                : BuildTermDocTotalCommand(request.TermId, totConnection);
            ((DbCommand)totCmd!).Parameters["@name"].Value = attr;

            ((DbCommand)cmd).Parameters["@name"].Value = attr;
            set.DocFrequencies[attr] = new TermDistribution(attr);

            using IDataReader reader = cmd.ExecuteReader();
            int n = 0;
            while (reader.Read())
            {
                if (++n > request.Limit && totCmd != null)
                {
                    // handle excess
                    long attrTot = (totCmd.ExecuteScalar() as long?) ?? 0;
                    set.DocFrequencies[attr].Frequencies["*"] = attrTot;
                    break;
                }
                string v = reader.GetString(0);
                set.DocFrequencies[attr].Frequencies[v] = reader.GetInt64(1);
            }
        }
    }

    private static IDbCommand BuildTermOccCommand(int id, int limit,
        IDbConnection connection)
    {
        // select distinct oa.value, count(oa.value) as freq
        // from occurrence o 
        // inner join occurrence_attribute oa on o.id = oa.occurrence_id
        // where oa.name='b' and o.token_id=469
        // group by oa.value
        // order by freq desc
        // limit 10

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT oa.value, COUNT(oa.value) AS freq\n" +
            "FROM occurrence o\n" +
            "INNER JOIN occurrence_attribute oa ON o.id = oa.occurrence_id\n" +
            "WHERE oa.name=@name AND o.token_id=@token_id\n" +
            "GROUP BY oa.value ORDER BY freq DESC LIMIT @limit;";
        AddParameter(cmd, "@name", DbType.String);
        AddParameter(cmd, "@token_id", DbType.Int32, id);
        AddParameter(cmd, "@limit", DbType.Int32, limit);
        return cmd;
    }

    private static IDbCommand BuildTermOccCommand(int id, int limit,
        int interval, IDbConnection connection)
    {
        // select distinct concat(
        //  cast(cast(oa.value as int) / 5 * 5 as varchar),
        //  '-',
        //  cast(cast(oa.value as int) / 5 * 5 + 4 as varchar)
        // ),
        // count(oa.value) as freq
        // from occurrence o 
        // inner join occurrence_attribute oa 
        // on o.id = oa.occurrence_id
        // where oa.name='len' and o.token_id=10
        // group by cast(oa.value as int) / 5
        // order by freq desc
        // limit 10

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT " +
            "CONCAT(" +
            "CAST(CAST(oa.value AS int) / 5 * 5 AS varchar)," +
            "'-',\n" +
            "CAST(CAST(oa.value AS int) / 5 * 5 + 4 AS varchar)\n" +
            "),\n" +
            "COUNT(oa.value) AS freq\n" +
            "FROM occurrence o\n" +
            "INNER JOIN occurrence_attribute oa ON o.id = oa.occurrence_id\n" +
            "WHERE oa.name=@name AND o.token_id=@token_id\n" +
            $"GROUP BY CAST(oa.value AS int) / {interval}\n" +
            "ORDER BY freq DESC LIMIT @limit;";
        AddParameter(cmd, "@name", DbType.String);
        AddParameter(cmd, "@token_id", DbType.Int32, id);
        AddParameter(cmd, "@limit", DbType.Int32, limit);
        return cmd;
    }

    private static IDbCommand BuildTermOccTotalCommand(int id,
        IDbConnection connection)
    {
        // total when exceeding limit:
        // select count(oa.value) as freq
        // from occurrence o 
        // inner join occurrence_attribute oa on o.id = oa.occurrence_id
        // where oa.name='b' and o.token_id=469

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(oa.value) AS freq\n" +
            "FROM occurrence o\n" +
            "INNER JOIN occurrence_attribute oa ON o.id = oa.occurrence_id\n" +
            "WHERE oa.name=@name AND o.token_id=@token_id;";
        AddParameter(cmd, "@name", DbType.String);
        AddParameter(cmd, "@token_id", DbType.Int32, id);
        return cmd;
    }

    private static void GetOccTermDistributions(TermDistributionRequest request,
        TermDistributionSet set, IDbConnection connection)
    {
        HashSet<string> numericNames = GetTypedAttributeNames(true, 1, connection);

        foreach (string attr in request.OccAttributes)
        {
            bool ranged = request.Interval > 1 && numericNames.Contains(attr);

            IDbCommand cmd = ranged
                ? BuildTermOccCommand(request.TermId, request.Limit, request.Interval, connection)
                : BuildTermOccCommand(request.TermId, request.Limit + 1, connection);
            IDbCommand? totCmd = ranged
                ? null
                : BuildTermOccTotalCommand(request.TermId, connection);

            ((DbCommand)cmd).Parameters["@name"].Value = attr;
            set.OccFrequencies[attr] = new TermDistribution(attr);

            using IDataReader reader = cmd.ExecuteReader();
            int n = 0;
            while (reader.Read())
            {
                if (++n > request.Limit && totCmd != null)
                {
                    // handle excess
                    long attrTot = (totCmd.ExecuteScalar() as long?) ?? 0;
                    set.OccFrequencies[attr].Frequencies["*"] = attrTot;
                    break;
                }
                string v = reader.GetString(0);
                set.OccFrequencies[attr].Frequencies[v] = reader.GetInt64(1);
            }
        }
    }

    /// <summary>
    /// Gets the distributions of the specified term with reference with
    /// the specified document/occurrence attributes.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The result.</returns>
    /// <exception cref="ArgumentNullException">request</exception>
    public TermDistributionSet GetTermDistributions(
        TermDistributionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        TermDistributionSet set = new(request.TermId);

        using IDbConnection connection = GetConnection();
        connection.Open();

        // total
        set.TermFrequency = GetTermFrequency(request.TermId, connection);
        if (set.TermFrequency == 0 || !request.HasAttributes()) return set;

        // doc attributes
        if (request.DocAttributes?.Count > 0)
            GetDocTermDistributions(request, set, connection);

        // occ attributes
        if (request.OccAttributes?.Count > 0)
            GetOccTermDistributions(request, set, connection);

        return set;
    }

    /// <summary>
    /// Finalizes the index by eventually adding calculated data into it.
    /// </summary>
    public void FinalizeIndex()
    {
        // TODO
        //using IDbConnection connection = GetConnection();
        //connection.Open();

        //IDbCommand cmd = connection.CreateCommand();
        //cmd.CommandText = "DELETE FROM token_occurrence_count;";
        //cmd.ExecuteNonQuery();

        //cmd.CommandText = "INSERT INTO token_occurrence_count(id,value,count) " +
        //    "SELECT t.id, t.value, " +
        //    "(select count(o.id) from occurrence o where o.token_id=t.id)\n" +
        //    "from token t;";
        //cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Upserts the specified span.
    /// </summary>
    /// <param name="span">The span.</param>
    /// <param name="connection">The connection.</param>
    protected abstract void UpsertSpan(TextSpan span, IDbConnection connection);
}
