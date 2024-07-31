using System.Linq;
using Corpus.Core;
using Corpus.Sql;
using Fusi.Tools.Data;
using Pythia.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Pythia.Core.Analysis;
using Fusi.Tools;
using System.Threading.Tasks;
using System.Threading;
using Pythia.Core.Query;
using System.Globalization;

namespace Pythia.Sql;

/// <summary>
/// SQL-based index repository.
/// </summary>
/// <seealso cref="SqlCorpusRepository" />
/// <seealso cref="IIndexRepository" />
public abstract class SqlIndexRepository : SqlCorpusRepository,
    IIndexRepository
{
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

        //// see https://michaelscodingspot.com/cache-implementations-in-csharp-net/
        //_tokenCache = new MemoryCache(new MemoryCacheOptions
        //{
        //    SizeLimit = 8192
        //});
        //_tokenCacheOptions = new MemoryCacheEntryOptions()
        //    .SetSize(1)
        //    .SetPriority(CacheItemPriority.High)
        //    .SetSlidingExpiration(TimeSpan.FromSeconds(60))
        //    .SetAbsoluteExpiration(TimeSpan.FromSeconds(60 * 3));
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
    /// Gets the specified page of words.
    /// </summary>
    /// <param name="filter">The words filter.</param>
    /// <returns>The results page.</returns>
    /// <exception cref="ArgumentNullException">filter</exception>
    public DataPage<Word> GetWords(WordFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        SqlWordQueryBuilder builder = new(SqlHelper);
        Tuple<string, string> t = builder.Build(filter);

        using IDbConnection connection = GetConnection();
        connection.Open();

        // get count
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = t.Item2;
        long? total = cmd.ExecuteScalar() as long?;
        if (total == null || total.Value == 0)
        {
            return new DataPage<Word>(
                filter.PageNumber, filter.PageSize, 0, []);
        }

        // get data
        List<Word> words = [];
        cmd = connection.CreateCommand();
        cmd.CommandText = t.Item1;
        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            words.Add(new Word
            {
                Id = reader.GetInt32(0),
                LemmaId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                Value = reader.GetString(2),
                ReversedValue = reader.GetString(3),
                Language = reader.IsDBNull(4) ? null : reader.GetString(4),
                Pos = reader.IsDBNull(5) ? null : reader.GetString(5),
                Lemma = reader.IsDBNull(6) ? null : reader.GetString(6),
                Count = reader.GetInt32(7)
            });
        }

        return new DataPage<Word>(
            filter.PageNumber, filter.PageSize, (int)total.Value, words);
    }

    /// <summary>
    /// Gets the specified page of lemmata.
    /// </summary>
    /// <param name="filter">The lemmata filter.</param>
    /// <returns>The results page.</returns>
    /// <exception cref="ArgumentNullException">filter</exception>
    public DataPage<Lemma> GetLemmata(LemmaFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        SqlLemmaQueryBuilder builder = new(SqlHelper);
        Tuple<string, string> t = builder.Build(filter);

        using IDbConnection connection = GetConnection();
        connection.Open();

        // get count
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = t.Item2;
        long? total = cmd.ExecuteScalar() as long?;
        if (total == null || total.Value == 0)
        {
            return new DataPage<Lemma>(
                filter.PageNumber, filter.PageSize, 0, []);
        }

        // get data
        List<Lemma> lemmata = [];
        cmd = connection.CreateCommand();
        cmd.CommandText = t.Item1;
        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lemmata.Add(new Lemma
            {
                Id = reader.GetInt32(0),
                Value = reader.GetString(1),
                ReversedValue = reader.GetString(2),
                Language = reader.IsDBNull(3) ? null : reader.GetString(3),
                Count = reader.GetInt32(4)
            });
        }

        return new DataPage<Lemma>(
            filter.PageNumber, filter.PageSize, (int)total.Value, lemmata);
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
            sb.AppendFormat("SELECT document_id, p1, value, text, {0} AS id\n" +
                "FROM span\n" +
                "WHERE span.type='{1}' " +
                "AND document_id={2} " +
                "AND p1 >= {3} AND p2 <= {4}\n",
                result.Id,
                TextSpan.TYPE_TOKEN,
                result.DocumentId,
                min, max);
        }

        sb.AppendLine("ORDER BY id, p1");
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
            Text = parts.First(p => p.Id == result.Id &&
                p.Position == result.P1).Text!,
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
                Text = reader.GetString(3),
                Id = reader.GetInt32(4)
            });
        }

        // build KWIC
        List<KwicSearchResult> searchResults = [];
        int id = parts[0].Id, i = 1, start = 0;

        while (i < parts.Count)
        {
            if (id != parts[i].Id)
            {
                KwicSearchResult result = CreateKwicSearchResult(
                    results.First(r => r.Id == id),
                    parts.Skip(start).Take(i - start).ToList(),
                    contextSize);
                searchResults.Add(result);
                start = i;
                id = parts[i].Id;
            }
            i++;
        }

        if (start < i)
        {
            KwicSearchResult result = CreateKwicSearchResult(
                results.First(r => r.Id == id),
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
                // Text = reader.GetString(reader.GetOrdinal("text")),
                Author = reader.GetString(reader.GetOrdinal("author")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                SortKey = reader.GetString(reader.GetOrdinal("sort_key"))
            });
        }

        return new DataPage<SearchResult>(
            request.PageNumber, request.PageSize, (int)total.Value, results);
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

    private static async Task ClearWordIndexAsync(IDbConnection connection)
    {
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "DELETE FROM word;";
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = "DELETE FROM lemma;";
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = "DELETE FROM word_document;";
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = "DELETE FROM lemma_document;";
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task BuildWordIndexAsync(IDbConnection connection,
        int pageSize,
        CancellationToken token,
        IProgress<ProgressReport>? progress = null)
    {
        // get rows count
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM(\n" +
            "SELECT language, value, pos, lemma, COUNT(id) as count\n" +
            "FROM span WHERE type = 'tok'\n" +
            "GROUP BY language, value, pos, lemma)\nAS s;";
        object? result = await cmd.ExecuteScalarAsync();
        if (result == null) return;
        long? total = result as long?;
        if (total == null || total.Value == 0) return;
        int pageCount = (int)Math.Ceiling((double)total.Value / pageSize);

        // prepare fetch command
        const string sql =
            "SELECT language, value, pos, lemma, COUNT(id) as count\n" +
            "FROM span WHERE type = 'tok'\n" +
            "GROUP BY language, value, pos, lemma\n" +
            "ORDER BY language, value, pos, lemma\n";
        cmd.CommandText = sql + SqlHelper.BuildPaging(0, pageSize);

        // prepare insert command
        IDbConnection connection2 = GetConnection();
        connection2.Open();
        DbCommand cmdInsert = (DbCommand)connection2.CreateCommand();
        cmdInsert.CommandText =
            "INSERT INTO word(language, value, reversed_value, pos, lemma, count)\n" +
            "VALUES(@language, @value, @reversed_value, @pos, @lemma, @count);";
        AddParameter(cmdInsert, "@language", DbType.String, "");
        AddParameter(cmdInsert, "@value", DbType.String, "");
        AddParameter(cmdInsert, "@reversed_value", DbType.String, "");
        AddParameter(cmdInsert, "@pos", DbType.String, "");
        AddParameter(cmdInsert, "@lemma", DbType.String, "");
        AddParameter(cmdInsert, "@count", DbType.Int32, 0);

        // process by pages
        int offset = 0;
        ProgressReport? report = progress != null ? new ProgressReport() : null;
        for (int n = 0; n < pageCount; n++)
        {
            using (IDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (token.IsCancellationRequested) break;

                    Word word = new()
                    {
                        Language = reader.IsDBNull(0) ? null : reader.GetString(0),
                        Value = reader.GetString(1),
                        Pos = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Lemma = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Count = reader.GetInt32(4)
                    };
                    word.ReversedValue = new string(word.Value.Reverse().ToArray());

                    // insert
                    cmdInsert.Parameters["@language"].Value =
                        word.Language ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@value"].Value = word.Value;
                    cmdInsert.Parameters["@reversed_value"].Value =
                        word.ReversedValue;
                    cmdInsert.Parameters["@pos"].Value =
                        word.Pos ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@lemma"].Value =
                        word.Lemma ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@count"].Value = word.Count;
                    await cmdInsert.ExecuteNonQueryAsync();
                }
            }

            // next page
            offset += pageSize;
            cmd.CommandText = sql + SqlHelper.BuildPaging(offset, pageSize);

            if (progress != null)
            {
                report!.Percent = (n + 1) * 100 / pageCount;
                progress.Report(report);
            }
        }
    }

    private async Task BuildLemmaIndexAsync(IDbConnection connection,
        int pageSize,
        CancellationToken token,
        IProgress<ProgressReport>? progress = null)
    {
        // get rows count
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM(\n" +
            "SELECT language, lemma\n" +
            "FROM span WHERE type = 'tok'\n" +
            "GROUP BY language, lemma)\nAS s;";
        object? result = await cmd.ExecuteScalarAsync();
        if (result == null) return;
        long? total = result as long?;
        if (total == null || total.Value == 0) return;
        int pageCount = (int)Math.Ceiling((double)total.Value / pageSize);

        // prepare fetch command
        const string sql =
            "SELECT language, lemma AS value, " +
            "reverse(lemma) AS reversed_value, " +
            "SUM(count) AS count\n" +
            "FROM word\n" +
            "WHERE lemma IS NOT NULL\n" +
            "GROUP BY language, lemma\n" +
            "ORDER BY lemma\n";
        cmd.CommandText = sql + SqlHelper.BuildPaging(0, pageSize);

        // prepare insert command
        IDbConnection connection2 = GetConnection();
        connection2.Open();
        DbCommand cmdInsert = (DbCommand)connection2.CreateCommand();
        cmdInsert.CommandText =
            "INSERT INTO lemma(language, value, reversed_value, count)\n" +
            "VALUES(@language, @value, @reversed_value, @count);";
        AddParameter(cmdInsert, "@language", DbType.String, "");
        AddParameter(cmdInsert, "@value", DbType.String, "");
        AddParameter(cmdInsert, "@reversed_value", DbType.String, "");
        AddParameter(cmdInsert, "@count", DbType.Int32, 0);

        // process by pages
        int offset = 0;
        ProgressReport? report = progress != null ? new ProgressReport() : null;
        for (int n = 0; n < pageCount; n++)
        {
            using (IDataReader reader = await cmd.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    if (token.IsCancellationRequested) break;

                    Lemma lemma = new()
                    {
                        Language = reader.IsDBNull(0) ? null : reader.GetString(0),
                        Value = reader.GetString(1),
                        ReversedValue = reader.GetString(2),
                        Count = reader.GetInt32(3)
                    };

                    // insert
                    cmdInsert.Parameters["@language"].Value =
                        lemma.Language ?? (object)DBNull.Value;
                    cmdInsert.Parameters["@value"].Value = lemma.Value;
                    cmdInsert.Parameters["@reversed_value"].Value =
                        lemma.ReversedValue;
                    cmdInsert.Parameters["@count"].Value = lemma.Count;
                    await cmdInsert.ExecuteNonQueryAsync();
                }
            }

            // next page
            offset += pageSize;
            cmd.CommandText = sql + SqlHelper.BuildPaging(offset, pageSize);

            if (progress != null)
            {
                report!.Percent = (n + 1) * 100 / pageCount;
                progress.Report(report);
            }
        }

        // update lemma ID in word table
        cmd.CommandText = "UPDATE word SET lemma_id=lemma.id\n" +
            "FROM lemma\n" +
            "WHERE COALESCE (word.language,'')=COALESCE(lemma.language, '')\n" +
            "AND word.lemma = lemma.value\n" +
            "AND lemma IS NOT NULL;";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets the names of all document attributes.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <returns>Names.</returns>
    private static async Task<HashSet<string>> GetDocAttrNamesAsync(
        IDbConnection connection)
    {
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT name FROM document_attribute;";

        HashSet<string> names = [];
        using IDataReader reader = await cmd.ExecuteReaderAsync();
        while (reader.Read()) names.Add(reader.GetString(0));

        return names;
    }

    /// <summary>
    /// Reads a numeric document pair's minimum and maximum values using the
    /// specified SQL command to fetch them.
    /// </summary>
    /// <param name="cmd">The SQL command.</param>
    /// <param name="name">The pair's name.</param>
    /// <param name="privileged">True if the pair refers to a privileged
    /// attribute.</param>
    /// <param name="binCount">The desired bins count.</param>
    /// <returns>List of pairs.</returns>
    private static async Task<IList<DocumentPair>> ReadDocumentPairMinMaxAsync(
        DbCommand cmd, string name, bool privileged, int binCount)
    {
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            if (await reader.IsDBNullAsync(0)) return [];

            // if fields are strings, read as string and then parse double
            double min, max;
            if (reader.GetFieldType(0) == typeof(string))
            {
                min = double.Parse(reader.GetString(0),
                    CultureInfo.InvariantCulture);
                max = double.Parse(reader.GetString(1),
                    CultureInfo.InvariantCulture);
                return DocumentPair.GenerateBinPairs(name, privileged, min, max,
                    binCount);
            }
            else
            {
                min = reader.GetDouble(0);
                max = reader.GetDouble(1);
            }

            return DocumentPair.GenerateBinPairs(name, privileged, min, max,
                binCount);
        }
        return [];
    }

    /// <summary>
    /// Builds all the document name=value pairs to be used when filling
    /// word and lemma document counts.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="binCounts">The desired bins counts. For each attribute
    /// (either privileged or not) which must be handled as a number,
    /// this dictionary includes its name as the key, and the desired count
    /// of bins as the value. For instance, an attribute named <c>year</c>
    /// whose value is a year number would have an entry with key=<c>year</c>
    /// and value=<c>3</c>, meaning that we want to distribute its values in
    /// 3 bins.</param>
    /// <param name="excludedAttrNames">The names of the non-privileged
    /// attributes to be excluded from the pairs. All the names of non categorical
    /// attributes should be excluded.</param>
    /// <returns>Built pairs.</returns>
    private async static Task<IList<DocumentPair>> BuildDocumentPairsAsync(
        IDbConnection connection,
        IDictionary<string, int> binCounts,
        HashSet<string> excludedAttrNames)
    {
        HashSet<string> privilegedDocAttrs = new(
            SqlQueryBuilder.PrivilegedDocAttrs.Except(
                ["title", "source", "profile_id", "sort_key"]));
        List<DocumentPair> pairs = [];
        StringBuilder sql = new();
        int priCount = 0;

        // (A) non-numeric:
        // (A.1) privileged
        foreach (string name in privilegedDocAttrs
            .Where(n => !binCounts.ContainsKey(n)))
        {
            if (sql.Length > 0) sql.Append("UNION\n");

            sql.Append($"SELECT DISTINCT '{name}' AS name, {name} AS value " +
                "FROM document\n");
            priCount++;
        }

        // (A.2) non-privileged
        IList<string> docAttrNames = (await GetDocAttrNamesAsync(connection))
            .Except(excludedAttrNames).ToList();

        foreach (string name in docAttrNames
            .Where(n => !binCounts.ContainsKey(n) &&
                        !privilegedDocAttrs.Contains(n)))
        {
            if (sql.Length > 0) sql.Append("UNION\n");

            sql.Append($"SELECT DISTINCT name, value " +
                $"FROM document_attribute WHERE name='{name}'\n");
        }

        // fetch pairs from the generated SQL queries
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = sql.ToString();
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                DocumentPair pair = new(
                    reader.GetString(0),
                    reader.GetString(1),
                    --priCount >= 0);
                pairs.Add(pair);
            }
        }

        // (B): numeric
        // (B.1): numeric, privileged
        foreach (var pair in binCounts.Where(
            p => privilegedDocAttrs.Contains(p.Key)))
        {
            cmd.CommandText = $"SELECT MIN({pair.Key}) AS n," +
                $"MAX({pair.Key}) AS m FROM document";
            pairs.AddRange(
                await ReadDocumentPairMinMaxAsync(cmd, pair.Key, true, pair.Value));
        }

        // (B.2): numeric, non-privileged
        foreach (var pair in binCounts.Where(
            p => !privilegedDocAttrs.Contains(p.Key)))
        {
            cmd.CommandText =
                "SELECT MIN(value) AS n, MAX(value) AS m\n" +
                "FROM document_attribute\n" +
                $"WHERE name='{pair.Key}'";
            pairs.AddRange(
                await ReadDocumentPairMinMaxAsync(cmd, pair.Key, false, pair.Value));
        }

        return pairs;
    }

    private void AppendDocPairClause(string table, DocumentPair pair,
        StringBuilder sql)
    {
        if (pair.IsNumeric)
        {
            string nn = SqlHelper.BuildTextAsNumber($"{table}.{pair.Name}");
            sql.Append(nn)
               .Append(">=").Append(pair.MinValue)
               .Append(" AND ")
               .Append(nn)
               .Append('<').Append(pair.MaxValue);
        }
        else
        {
            sql.Append($"{table}.{pair.Name}='{pair.Value}'");
        }
    }

    private async Task BuildWordDocumentAsync(IDbConnection connection,
        IList<DocumentPair> docPairs)
    {
        const int pageSize = 1000;
        DbCommand wordCmd = (DbCommand)connection.CreateCommand();
        wordCmd.CommandText = "SELECT id FROM word ORDER BY id\n"
            + GetPagingSql(0, pageSize);
        int total = GetCount(connection, "word");
        int pageCount = (int)Math.Ceiling((double)total / pageSize);
        List<int> wordIds = new(pageSize);

        DbCommand countCmd = (DbCommand)connection.CreateCommand();

        IDbConnection connection2 = GetConnection();
        connection2.Open();
        DbCommand cmdInsert = (DbCommand)connection2.CreateCommand();
        cmdInsert.CommandText = "INSERT INTO word_document(word_id," +
            "doc_attr_name, doc_attr_value, count)\n" +
            "VALUES(@word_id, @document_id, @doc_attr_name, " +
                   "@doc_attr_value, @count);";
        AddParameter(cmdInsert, "@word_id", DbType.Int32, 0);
        AddParameter(cmdInsert, "@doc_attr_name", DbType.String, "");
        AddParameter(cmdInsert, "@doc_attr_value", DbType.String, "");
        AddParameter(cmdInsert, "@count", DbType.Int32, 0);

        StringBuilder sql = new();

        for (int i = 0; i < pageCount; i++)
        {
            await using (DbDataReader reader = await wordCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync()) wordIds.Add(reader.GetInt32(0));
            }
            foreach (int wordId in wordIds)
            {
                foreach (DocumentPair pair in docPairs)
                {
                    if (pair.IsPrivileged)
                    {
                        sql.Append("SELECT COUNT(s.id) FROM span s\n" +
                            "INNER JOIN document d ON s.document_id=d.id\n" +
                            "WHERE ");

                        AppendDocPairClause("d", pair, sql);
                    }
                    else
                    {
                        sql.Append("SELECT COUNT(s.id) FROM span s\n" +
                            "INNER JOIN document_attribute da ON " +
                            "s.document_id=da.document_id\n" +
                            "WHERE ");

                        AppendDocPairClause("da", pair, sql);
                    }

                    // execute sql getting count
                    countCmd.CommandText = sql.ToString();
                    object? result = await countCmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        cmdInsert.Parameters["@word_id"].Value = wordId;
                        cmdInsert.Parameters["@doc_attr_name"].Value = pair.Name;
                        cmdInsert.Parameters["@doc_attr_value"].Value =
                            pair.Value ?? $"{pair.MinValue}:{pair.MaxValue}";
                        cmdInsert.Parameters["@count"].Value =
                            Convert.ToInt32(result);
                        await cmdInsert.ExecuteNonQueryAsync();
                    }

                    sql.Clear();
                }
                wordIds.Clear();
            } // foreach wordId
        }
    }

    private static async Task BuildLemmaDocumentAsync(IDbConnection connection)
    {
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "INSERT INTO lemma(lemma_id, doc_attr_name, " +
            "doc_attr_value, count)\n" +
            "SELECT lemma_id, doc_attr_name, doc_attr_value, SUM(count)\n" +
            "FROM word_document\n" +
            "GROUP BY lemma_id, doc_attr_name, doc_attr_value;";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Builds the words index basing on tokens.
    /// </summary>
    /// <param name="binCounts">The desired bins counts. For each attribute
    /// (either privileged or not) which must be handled as a number,
    /// this dictionary includes its name as the key, and the desired count
    /// of bins as the value. For instance, an attribute named <c>year</c>
    /// whose value is a year number would have an entry with key=<c>year</c>
    /// and value=<c>3</c>, meaning that we want to distribute its values in
    /// 3 bins.</param>
    /// <param name="excludedAttrNames">The names of the non-privileged
    /// attributes to be excluded from the pairs. All the names of non categorical
    /// attributes should be excluded.</param>
    /// <param name="token">The cancellation token.</param>
    /// <param name="progress">The progress.</param>
    public async Task BuildWordIndexAsync(IDictionary<string, int> binCounts,
        HashSet<string> excludedAttrNames,
        CancellationToken token,
        IProgress<ProgressReport>? progress = null)
    {
        const int pageSize = 100;
        using IDbConnection connection = GetConnection();
        connection.Open();

        await ClearWordIndexAsync(connection);
        await BuildWordIndexAsync(connection, pageSize, token, progress);
        await BuildLemmaIndexAsync(connection, pageSize, token, progress);

        IList<DocumentPair> docPairs = await BuildDocumentPairsAsync(
            connection, binCounts, excludedAttrNames);
        await BuildWordDocumentAsync(connection, docPairs);
        await BuildLemmaDocumentAsync(connection);
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
