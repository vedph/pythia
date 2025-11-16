using Corpus.Core;
using Corpus.Sql;
using Fusi.Tools;
using Fusi.Tools.Data;
using Pythia.Core;
using Pythia.Core.Analysis;
using Pythia.Core.Query;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
    /// Word count.
    /// </summary>
    protected class WordCount(int wordId, int lemmaId, DocumentPair pair,
        int count)
    {
        /// <summary>
        /// Gets the word identifier.
        /// </summary>
        public int WordId { get; } = wordId;

        /// <summary>
        /// Gets the lemma identifier.
        /// </summary>
        public int LemmaId { get; } = lemmaId;

        /// <summary>
        /// Gets the pair.
        /// </summary>
        public DocumentPair Pair { get; } = pair;

        /// <summary>
        /// Gets the value.
        /// </summary>
        public int Value { get; } = count;
    }

    /// <summary>
    /// The language maximum length in the DB schema.
    /// </summary>
    protected const int LANGUAGE_MAX = 50;

    /// <summary>
    /// The attribute name maximum length in the DB schema.
    /// </summary>
    protected const int ATTR_NAME_MAX = 100;
    /// <summary>
    /// The attribute value maximum length in the DB schema.
    /// </summary>
    protected const int ATTR_VALUE_MAX = 500;

    /// <summary>
    /// The span type maximum length in the DB schema.
    /// </summary>
    protected const int TYPE_MAX = 50;
    /// <summary>
    /// The POS maximum length in the DB schema.
    /// </summary>
    protected const int POS_MAX = 50;
    /// <summary>
    /// The lemma maximum length in the DB schema.
    /// </summary>
    protected const int LEMMA_MAX = 500;
    /// <summary>
    /// The span value maximum length in the DB schema.
    /// </summary>
    protected const int VALUE_MAX = 500;
    /// <summary>
    /// The text maximum length in the DB schema.
    /// </summary>
    protected const int TEXT_MAX = 1000;

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
    }

    /// <summary>
    /// Gets the schema representing the model for the corpus repository data.
    /// </summary>
    /// <returns>Schema.</returns>
    public abstract string GetSchema();

    /// <summary>
    /// Gets the a truncated version of the received string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>String or null.</returns>
    protected static string? GetTruncatedString(string? value, int maxLength)
    {
        if (value == null || value.Length <= maxLength) return value;

        Debug.WriteLine($"Truncating: \"{value}\" ({value.Length})");
        return value[..maxLength];
    }

    /// <summary>
    /// Gets the full list of document attributes names.
    /// </summary>
    /// <param name="privileged">True to include also the privileged attribute
    /// names in the list.</param>
    /// <returns>Sorted list of unique names.</returns>
    public IList<AttributeInfo> GetDocAttributeInfo(bool privileged)
    {
        List<AttributeInfo> infos = [];
        if (privileged)
        {
            infos.AddRange(TextSpan.GetPrivilegedAttrs(false).Select(n =>
                new AttributeInfo(n, SqlQueryBuilder.GetPrivilegedAttrType(n))));
        }

        using IDbConnection connection = GetConnection();
        connection.Open();
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT name,type FROM document_attribute " +
            "ORDER BY name,type;";
        using DbDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            infos.Add(new AttributeInfo(reader.GetString(0), reader.GetInt32(1)));
        }

        return privileged ? infos.OrderBy(a => a.Name).ToList() : [.. infos];
    }

    /// <summary>
    /// Gets the spans starting at the specified position.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="p1">The start position (P1).</param>
    /// <param name="type">The optional type filter.</param>
    /// <param name="attributes">True to include span attributes.</param>
    /// <returns>Spans.</returns>
    public IList<TextSpan> GetSpansAt(int documentId, int p1, string? type = null,
        bool attributes = false)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();
        List<TextSpan> spans = [];
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "SELECT id, document_id, type, p1, p2, index, length, " +
            "language, pos, lemma, value, text\n" +
            "FROM span\n" +
            "WHERE document_id=@document_id AND p1=@p1" +
            (type == null ? "" : " AND type=@type") + ";";
        AddParameter(cmd, "@document_id", DbType.Int32, documentId);
        AddParameter(cmd, "@p1", DbType.Int32, p1);
        if (type != null) AddParameter(cmd, "@type", DbType.String, type);

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                spans.Add(new TextSpan
                {
                    Id = reader.GetInt32(0),
                    DocumentId = reader.GetInt32(1),
                    Type = reader.GetString(2),
                    P1 = reader.GetInt32(3),
                    P2 = reader.GetInt32(4),
                    Index = reader.GetInt32(5),
                    Length = reader.GetInt32(6),
                    Language = reader.IsDBNull(7)? null : reader.GetString(7),
                    Pos = reader.IsDBNull(8)? null : reader.GetString(8),
                    Lemma = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Value = reader.GetString(10),
                    Text = reader.GetString(11)
                });
            }
        }

        if (attributes)
        {
            cmd.CommandText = "SELECT name, value, type FROM span_attribute\n" +
                "WHERE span_id=@span_id;";
            AddParameter(cmd, "@span_id", DbType.Int32, 0);

            foreach (TextSpan span in spans)
            {
                List<Corpus.Core.Attribute> attrs = [];
                cmd.Parameters["@span_id"].Value = span.Id;
                using DbDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    attrs.Add(new Corpus.Core.Attribute
                    {
                        Name = reader.GetString(0),
                        Value = reader.GetString(1),
                        Type = (AttributeType)reader.GetInt32(2)
                    });
                }
                span.Attributes = attrs;
            }
        }

        return spans;
    }

    private static void BuildSpanQuery(TextSpanFilter filter, DbCommand cmd)
    {
        StringBuilder sql = new();

        sql.AppendLine("SELECT DISTINCT s.id, s.document_id, s.type, " +
            "s.p1, s.p2, s.index, s.length, s.language, s.pos, s.lemma, " +
            "s.value, s.text FROM span s");

        if (filter.Attributes?.Count > 0)
            sql.AppendLine("INNER JOIN span_attribute sa ON s.id=sa.span_id");

        if (!filter.IsEmpty)
        {
            sql.AppendLine("WHERE");
            List<string> clauses = [];

            if (!string.IsNullOrEmpty(filter.Type))
            {
                clauses.Add("s.type=@type");
                AddParameter(cmd, "@type", DbType.String, filter.Type);
            }

            if (filter.PositionMin > 0)
            {
                clauses.Add("s.p1 >= @p1");
                AddParameter(cmd, "@p1", DbType.Int32, filter.PositionMin);
            }

            if (filter.PositionMax > 0)
            {
                clauses.Add("s.p2 <= @p2");
                AddParameter(cmd, "@p2", DbType.Int32, filter.PositionMax);
            }

            if (filter.DocumentIds?.Count > 0)
            {
                clauses.Add("s.document_id IN ("
                    + string.Join(",", filter.DocumentIds.Select(
                        n => n.ToString(CultureInfo.InvariantCulture)))
                    + ")");
            }

            if (filter.Attributes?.Count > 0)
            {
                int n = 0;
                foreach (KeyValuePair<string, string> kvp in filter.Attributes)
                {
                    n++;
                    clauses.Add($"sa.name=@name{n} AND sa.value=@value{n}");
                    AddParameter(cmd, $"@name{n}", DbType.String, kvp.Key);
                    AddParameter(cmd, $"@value{n}", DbType.String, kvp.Value);
                }
            }

            sql.AppendJoin("\nAND ", clauses).AppendLine();
        }

        sql.AppendLine("ORDER BY s.document_id, s.p1, s.p2;");
        cmd.CommandText = sql.ToString();
    }

    /// <summary>
    /// Enumerates the spans matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="attributes">True to include span attributes.</param>
    /// <returns>Spans.</returns>
    public IEnumerable<TextSpan> EnumerateSpans(TextSpanFilter filter,
        bool attributes = false)
    {
        ArgumentNullException.ThrowIfNull(filter);

        using IDbConnection connection = GetConnection();
        connection.Open();
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        BuildSpanQuery(filter, cmd);

        DbCommand? attrCmd = null;
        if (attributes)
        {
            attrCmd = (DbCommand)connection.CreateCommand();
            attrCmd.CommandText = "SELECT name, value, type FROM span_attribute\n" +
                "WHERE span_id=@span_id;";
            AddParameter(attrCmd, "@span_id", DbType.Int32, 0);
        }

        using DbDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            TextSpan span = new()
            {
                Id = reader.GetInt32(0),
                DocumentId = reader.GetInt32(1),
                Type = reader.GetString(2),
                P1 = reader.GetInt32(3),
                P2 = reader.GetInt32(4),
                Index = reader.GetInt32(5),
                Length = reader.GetInt32(6),
                Language = reader.IsDBNull(7) ? null : reader.GetString(7),
                Pos = reader.IsDBNull(8) ? null : reader.GetString(8),
                Lemma = reader.IsDBNull(9) ? null : reader.GetString(9),
                Value = reader.GetString(10),
                Text = reader.GetString(11)
            };

            if (attributes)
            {
                attrCmd!.Parameters["@span_id"].Value = span.Id;

                List<Corpus.Core.Attribute> attrs = [];
                using DbDataReader attrReader = attrCmd.ExecuteReader();
                while (attrReader.Read())
                {
                    attrs.Add(new Corpus.Core.Attribute
                    {
                        Name = attrReader.GetString(0),
                        Value = attrReader.GetString(1),
                        Type = (AttributeType)attrReader.GetInt32(2)
                    });
                }
                span.Attributes = attrs;
            }

            yield return span;
        }
    }

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
                        attrCmd.Parameters["@name"].Value =
                            GetTruncatedString(attribute.Name, ATTR_NAME_MAX);
                        attrCmd.Parameters["@value"].Value =
                            GetTruncatedString(attribute.Value, ATTR_VALUE_MAX);
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

    private static List<int> GetRangeTokenIds(IDbConnection connection,
        int documentId, int start, int end)
    {
        // get all the target IDs in the specified doc's range
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "SELECT id FROM span\n" +
        "WHERE document_id=@document_id AND " +
            "p1 >= @start AND p2 <= @end;";
        AddParameter(cmd, "@document_id", DbType.Int32, documentId);
        AddParameter(cmd, "@start", DbType.Int32, start);
        AddParameter(cmd, "@end", DbType.Int32, end);

        List<int> ids = [];
        using DbDataReader reader = cmd.ExecuteReader();
        while (reader.Read()) ids.Add(reader.GetInt32(0));

        return ids;
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
    /// <param name="name">The attribute name. This can be either a
    /// non-privileged or a privileged attribute name.</param>
    /// <param name="value">The attribute value.</param>
    /// <param name="type">The attribute type.</param>
    public void AddSpanAttributes(int documentId, int start, int end,
        string name, string value, AttributeType type)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        // get all the target IDs in the specified doc's range
        List<int> ids = GetRangeTokenIds(connection, documentId, start, end);

        // truncate value if needed
        value = name switch
        {
            "type" or "language" or "pos" => GetTruncatedString(value, 50)!,
            "text" => GetTruncatedString(value, TEXT_MAX)!,
            _ => GetTruncatedString(value, ATTR_VALUE_MAX)!,
        };

        // add the received attribute to each of these occurrences
        using IDbTransaction tr = connection.BeginTransaction();
        try
        {
            DbCommand cmd = (DbCommand)connection.CreateCommand();

            if (TextSpan.IsPrivilegedSpanAttr(name))
            {
                cmd.CommandText =
                    $"UPDATE span SET {name}=@value WHERE id=@span_id;";
                AddParameter(cmd, "@span_id", DbType.Int32, 0);
                AddParameter(cmd, "@value", DbType.String, value);
            }
            else
            {
                cmd.CommandText = "INSERT INTO span_attribute" +
                    "(span_id, name, value, type)\n" +
                    "VALUES(@span_id, @name, @value, @type);";
                AddParameter(cmd, "@span_id", DbType.Int32, 0);
                AddParameter(cmd, "@name", DbType.String,
                    GetTruncatedString(name, ATTR_NAME_MAX));
                AddParameter(cmd, "@value", DbType.String, value);
                AddParameter(cmd, "@type", DbType.Int32, (int)type);
            }

            foreach (int id in ids)
            {
                cmd.Parameters["@span_id"].Value = id;
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
    /// Deletes the specified attribute(s) from all the tokens included in the
    /// specified range of the specified document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="start">The start position.</param>
    /// <param name="end">The end position (inclusive).</param>
    /// <param name="names">The names of the attributes to remove.</param>
    public void DeleteSpanAttributes(int documentId, int start, int end,
        IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        // get all the target token IDs in the specified doc's range
        using IDbConnection connection = GetConnection();
        connection.Open();

        List<int> ids = GetRangeTokenIds(connection, documentId, start, end);

        // remove the received attributes from each of these tokens
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "DELETE FROM span_attribute\n" +
            $"WHERE span_id IN({string.Join(",", ids)}) AND\n" +
            $"name IN({string.Join(",",
                names.Select(s => SqlHelper.SqlEncode(s, false, true)))});";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Deletes all the tokens of the document with the specified ID.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="type">The span type or null to delete any spans.</param>
    /// <param name="negatedType">True to delete any type except the specified
    /// one.</param>
    public void DeleteDocumentSpans(int documentId, string? type = null,
        bool negatedType = false)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        IDbCommand cmd = connection.CreateCommand();
        if (type != null)
        {
            cmd.CommandText = "DELETE FROM span\n" +
                "WHERE document_id=@document_id AND " +
                $"type{(negatedType? "<>":"=")}@type;";
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
        string tableName, Dictionary<string, double> stats)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name, " +
            $"COUNT(id) FROM {tableName} GROUP BY name;";

        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
            stats["@" + reader.GetString(0)] = reader.GetDouble(1);
    }

    private static int GetCount(IDbConnection connection, string tableName,
        string? tail = null)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        if (tail != null) cmd.CommandText += $" {tail}";

        object? result = cmd.ExecuteScalar();

        if (result == DBNull.Value || result == null) return 0;
        return Convert.ToInt32(result);
    }

    private static int GetMax(IDbConnection connection, string tableName,
        string fieldName)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT MAX({fieldName}) FROM {tableName};";
        object? result = cmd.ExecuteScalar();
        if (result == DBNull.Value || result == null) return 0;
        return Convert.ToInt32(result);
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
        if (stats.TryGetValue(dividend, out double de) &&
            stats.TryGetValue(divisor, out double dv) && dv > 0)
        {
            stats[dividend + "_ratio"] = de / dv;
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
                Pos = reader.IsDBNull(3) ? null : reader.GetString(3),
                Language = reader.IsDBNull(4) ? null : reader.GetString(4),
                Count = reader.GetInt32(5)
            });
        }

        return new DataPage<Lemma>(
            filter.PageNumber, filter.PageSize, (int)total.Value, lemmata);
    }

    /// <summary>
    /// Gets the word or lemma counts for the subset of documents having
    /// the specified attribute name and any of its values. The counts are
    /// grouped by value or value bin when numeric.
    /// </summary>
    /// <param name="lemma">if set to <c>true</c> get lemma counts, else
    /// get word counts.</param>
    /// <param name="id">The identifier.</param>
    /// <param name="attrName">Name of the attribute.</param>
    /// <returns>Token counts.</returns>
    public IList<TokenCount> GetTokenCounts(bool lemma, int id,
        string attrName)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();
        string name = lemma ? "lemma" : "word";
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "SELECT doc_attr_value, count\n" +
            $"FROM {name}_count\n" +
            $"WHERE {name}_id=@id AND doc_attr_name=@doc_attr_name AND count>0\n" +
            "ORDER BY count DESC";
        AddParameter(cmd, "@id", DbType.Int32, id);
        AddParameter(cmd, "@doc_attr_name", DbType.String, attrName);

        List<TokenCount> counts = [];
        using DbDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            counts.Add(new TokenCount(
                id, attrName, reader.GetString(0), reader.GetInt32(1)));
        }

        return counts;
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

        // attributes
        CollectAttributesStats(connection, "document_attribute", stats);
        CollectAttributesStats(connection, "span_attribute", stats);

        // corpus
        stats["corpus_count"] = GetCount(connection, "corpus");
        // documents
        stats["document_count"] = GetCount(connection, "document");
        stats["document_attribute_count"] =
            GetCount(connection, "document_attribute");
        // profiles
        stats["profile_count"] = GetCount(connection, "profile");
        // spans
        stats["span_count"] = GetCount(connection, "span");
        stats["span_attribute_count"] = GetCount(connection, "span_attribute");
        stats["span_tok_attribute_count"] =
            GetCount(connection, "span_attribute", "INNER JOIN span " +
            "ON span_attribute.span_id=span.id WHERE span.type='tok';");

        // words
        stats["word_count"] = GetCount(connection, "word");
        // lemmata
        stats["lemma_count"] = GetCount(connection, "lemma");

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
        AddRatio("span_tok_attribute_count", "span_tok_count", stats);

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
        List<string> left = [.. (from p in parts
                             where p.Position < result.P1
                             orderby p.Position
                             select p.Value)];
        // pad
        if (left.Count < contextSize)
        {
            left.InsertRange(0,
               Enumerable.Repeat("", contextSize - left.Count));
        }

        // right
        List<string> right = [.. (from p in parts
                              where p.Position > result.P1
                              orderby p.Position
                              select p.Value)];
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
                    [.. parts.Skip(start).Take(i - start)],
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
                [.. parts.Skip(start).Take(i - start)],
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

    #region Word Index Management
    /// <summary>
    /// Clears the word index, removing all words and lemmata and their counts.
    /// </summary>
    /// <param name="connection">connection</param>
    private static void ClearWordIndex(IDbConnection connection)
    {
        using IDbTransaction trans = connection.BeginTransaction();
        IDbCommand cmd = connection.CreateCommand();
        cmd.Transaction = trans;

        try
        {
            // remove foreign key constraints from span table
            cmd.CommandText = "ALTER TABLE span DROP CONSTRAINT span_word_fk;";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "ALTER TABLE span DROP CONSTRAINT span_lemma_fk;";
            cmd.ExecuteNonQuery();

            // set word_id and lemma_id to NULL in span table
            cmd.CommandText = "UPDATE span SET word_id = NULL, lemma_id = NULL;";
            cmd.ExecuteNonQuery();

            // truncate the tables
            cmd.CommandText = "TRUNCATE TABLE word_count, lemma_count, " +
                "word, lemma RESTART IDENTITY;";
            cmd.ExecuteNonQuery();

            // add back the foreign key constraints to span table
            cmd.CommandText = """
            ALTER TABLE span ADD CONSTRAINT span_word_fk 
                FOREIGN KEY (word_id) REFERENCES word(id) ON DELETE SET NULL 
                ON UPDATE CASCADE;
            ALTER TABLE span ADD CONSTRAINT span_lemma_fk 
                FOREIGN KEY (lemma_id) REFERENCES lemma(id) ON DELETE SET NULL 
                ON UPDATE CASCADE;
            """;
            cmd.ExecuteNonQuery();

            trans.Commit();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            trans.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Insert a set of words.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="words">The words to insert.</param>
    protected virtual async Task BatchInsertWords(IDbConnection connection,
        List<Word> words)
    {
        await using DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText =
            "INSERT INTO word(language, value, reversed_value, pos, lemma, count)\n" +
            "VALUES(@language, @value, @reversed_value, @pos, @lemma, @count);";
        AddParameter(cmd, "@language", DbType.String, "");
        AddParameter(cmd, "@value", DbType.String, "");
        AddParameter(cmd, "@reversed_value", DbType.String, "");
        AddParameter(cmd, "@pos", DbType.String, "");
        AddParameter(cmd, "@lemma", DbType.String, "");
        AddParameter(cmd, "@count", DbType.Int32, 0);

        foreach (Word word in words)
        {
            cmd.Parameters["@language"].Value =
                GetTruncatedString(word.Language, LANGUAGE_MAX)
                ?? (object)DBNull.Value;
            cmd.Parameters["@value"].Value =
                GetTruncatedString(word.Value, VALUE_MAX);
            cmd.Parameters["@reversed_value"].Value =
                GetTruncatedString(word.ReversedValue, VALUE_MAX);
            cmd.Parameters["@pos"].Value =
                GetTruncatedString(word.Pos, POS_MAX)
                ?? (object)DBNull.Value;
            cmd.Parameters["@lemma"].Value =
                GetTruncatedString(word.Lemma, LEMMA_MAX)
                ?? (object)DBNull.Value;
            cmd.Parameters["@count"].Value = word.Count;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Build the SQL query to fetch words from the database by grouping
    /// spans by language, value, POS and lemma, possibly excluding the
    /// specified POS values and span attribute names.
    /// </summary>
    /// <param name="excludedAttrNames">The span attribute names which when
    /// present exclude it from results.</param>
    /// <param name="excludedPosValues">The POS values which when assigned to
    /// a span exclude it from results.</param>
    /// <param name="order">True to add an order by clause.</param>
    /// <returns>SQL query.</returns>
    private string BuildWordFetchQuery(HashSet<string> excludedAttrNames,
        HashSet<string> excludedPosValues, bool order)
    {
        // words are fetched from token spans grouped by language, value, pos
        // and lemma
        StringBuilder sb = new();
        sb.Append(
            "SELECT language, LOWER(value) AS value, pos, LOWER(lemma) as lemma, " +
            "COUNT(id) as count\n" +
            "FROM span WHERE type='tok'\n");

        // if some POS values are excluded, add the condition
        if (excludedPosValues.Count > 0)
        {
            sb.Append("AND pos NOT IN (")
                .AppendJoin(",", excludedPosValues.Select(
                    p => SqlHelper.SqlEncode(p, false, true)))
                .Append(")\n");
        }

        // if some span attribute names are excluded, add the condition
        if (excludedAttrNames.Count > 0)
        {
            sb.Append("AND NOT EXISTS\n(\n" +
                "  SELECT 1 FROM span_attribute sa\n" +
                "  WHERE sa.id=span.id\n" +
                "  AND sa.name IN(")
                .AppendJoin(",",
                    excludedAttrNames.Select(n => $"'{SqlHelper.SqlEncode(n)}'"))
                .Append(")\n)\n");
        }

        // group by language, value, pos and lemma
        sb.Append("GROUP BY language, LOWER(value), pos, LOWER(lemma)\n");

        // if the order is requested, add it
        if (order)
            sb.Append("ORDER BY language, LOWER(value), pos, LOWER(lemma)\n");

        return sb.ToString();
    }

    /// <summary>
    /// Assign word ID FK to token spans based on the grouping which defines
    /// a word (language, value, POS and lemma), possibly excluding those spans
    /// defined by the specified attribute names and POS values.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="excludedAttrNames">The span attributes names used to exclude
    /// spans from words. A span with any of these attributes names will be
    /// excluded.</param>
    /// <param name="excludedPosValues">The POS values used to exclude spans
    /// from words. A span with any of these POS values will be excluded.</param>
    private async Task AssignWordIdsAsync(IDbConnection connection,
        HashSet<string> excludedAttrNames,
        HashSet<string> excludedPosValues)
    {
        // build the update SQL query with the same exclusion criteria
        StringBuilder updateSql = new();
        updateSql.AppendLine("UPDATE span SET word_id = word.id");
        updateSql.AppendLine("FROM word");
        updateSql.AppendLine("WHERE span.type = 'tok'");
        updateSql.AppendLine("  AND COALESCE(span.language, '') = " +
            "COALESCE(word.language, '')");
        updateSql.AppendLine("  AND span.value = word.value");
        updateSql.AppendLine("  AND COALESCE(span.pos, '') = " +
            "COALESCE(word.pos, '')");
        updateSql.AppendLine("  AND span.lemma = word.lemma");

        // add the same POS exclusion criteria used when collecting words
        if (excludedPosValues.Count > 0)
        {
            updateSql.Append("  AND span.pos NOT IN (")
                    .AppendJoin(",", excludedPosValues.Select(
                        p => SqlHelper.SqlEncode(p, false, true)))
                    .AppendLine(")");
        }

        // add the same attribute-based exclusion criteria used
        // when collecting words
        if (excludedAttrNames.Count > 0)
        {
            updateSql.AppendLine("  AND NOT EXISTS (");
            updateSql.AppendLine("    SELECT 1 FROM span_attribute sa");
            updateSql.AppendLine("    WHERE sa.span_id = span.id");
            updateSql.Append("      AND sa.name IN(")
                    .AppendJoin(",", excludedAttrNames.Select(
                        n => $"'{SqlHelper.SqlEncode(n)}'"))
                    .AppendLine("))");
        }

        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = updateSql.ToString();
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Inserts words into the database, fetching them from the token spans
    /// grouped by language, value, POS and lemma.
    /// </summary>
    /// <remarks>This works as follows:
    /// <para>- count the total pages of token spans to fetch;</para>
    /// <para>- fetch each token spans page, where spans are grouped by language,
    /// value, POS, and lemma;</para>
    /// <para>- for each word, add it to the words table(in batches for better
    /// performance);</para>
    /// <para>- finally, assign FK word_id to each token span which was grouped
    /// into a word.</para>
    /// </remarks>
    /// <param name="connection">The connection.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="excludedAttrNames">The span attributes names used to exclude
    /// spans from words. A span with any of these attributes names will be
    /// excluded.</param>
    /// <param name="excludedPosValues">The POS values used to exclude spans
    /// from words. A span with any of these POS values will be excluded.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <param name="progress">The progress reporter.</param>
    private async Task InsertWordsAsync(IDbConnection connection,
        int pageSize, HashSet<string> excludedAttrNames,
        HashSet<string> excludedPosValues,
        CancellationToken cancel,
        IProgress<ProgressReport>? progress = null)
    {
        // setup
        ProgressReport report = new();
        const int batchSize = 1000;
        List<Word> words = new(batchSize);

        // get a new connection for the insert
        using IDbConnection connection2 = GetConnection();
        connection2.Open();

        // get the SQL query to fetch (unordered) words from token spans
        string sql = BuildWordFetchQuery(excludedAttrNames, excludedPosValues,
            false);

        // count the unique combinations of language, value, pos, and lemma,
        // corresponding to the number of words to extract, except for those
        // excluded by the specified attributes and POS values
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM(\n" + sql + ") AS s;";

        // execute the count command and calculate the page count
        object? result = await cmd.ExecuteScalarAsync();
        if (result == null) return;
        long? total = result as long?;
        if (total == null || total.Value == 0) return;
        int pageCount = (int)Math.Ceiling((double)total.Value / pageSize);

        // prepare the corresponding paged fetch command to get words
        sql = BuildWordFetchQuery(excludedAttrNames, excludedPosValues, true);
        cmd.CommandText = sql + SqlHelper.BuildPaging(0, pageSize);

        // for each words page
        int offset = 0;
        for (int i = 0; i < pageCount; i++)
        {
            await using (DbDataReader reader = await cmd.ExecuteReaderAsync())
            {
                // for each word in page
                while (await reader.ReadAsync())
                {
                    if (cancel.IsCancellationRequested) break;

                    // materialize the word
                    Word word = new()
                    {
                        Language = await reader.IsDBNullAsync(0)
                            ? null : reader.GetString(0),
                        Value = reader.GetString(1),
                        Pos = await reader.IsDBNullAsync(2)
                            ? null : reader.GetString(2),
                        Lemma = await reader.IsDBNullAsync(3)
                            ? null : reader.GetString(3),
                        Count = reader.GetInt32(4)
                    };
                    // skip non-letter words
                    if (!word.Value.All(char.IsLetter) ||
                        word.Lemma?.All(char.IsLetter) != true)
                    {
                        continue;
                    }
                    word.ReversedValue = word.Value.Length > 1
                        ? new string([.. word.Value.Reverse()])
                        : word.Value;

                    // add the word to the batch
                    words.Add(word);

                    // if the batch is full, flush it
                    if (words.Count == batchSize)
                    {
                        await BatchInsertWords(connection2, words);
                        words.Clear();
                    }
                }
            }

            // move to the next page
            offset += pageSize;
            cmd.CommandText = sql + SqlHelper.BuildPaging(offset, pageSize);

            // report progress if requested
            if (progress != null)
            {
                report.Percent = (i + 1) * 100 / pageCount;
                report.Message = "Inserting words...";
                progress.Report(report);
            }
        }

        // if there are any words left in the batch, flush them
        if (words.Count > 0) await BatchInsertWords(connection2, words);

        // update word ID FK in span table
        report.Message = "Updating span table...";
        progress?.Report(report);
        await AssignWordIdsAsync(connection2,
            excludedAttrNames, excludedPosValues);
    }

    /// <summary>
    /// Insert a batch of lemmata.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="lemmata">The lemmata.</param>
    protected virtual async Task BatchInsertLemmata(IDbConnection connection,
        List<Lemma> lemmata)
    {
        DbCommand cmdInsert = (DbCommand)connection.CreateCommand();
        cmdInsert.CommandText =
            "INSERT INTO lemma(pos, language, value, reversed_value, count)\n" +
            "VALUES(@pos, @language, @value, @reversed_value, @count);";
        AddParameter(cmdInsert, "@pos", DbType.String, "");
        AddParameter(cmdInsert, "@language", DbType.String, "");
        AddParameter(cmdInsert, "@value", DbType.String, "");
        AddParameter(cmdInsert, "@reversed_value", DbType.String, "");
        AddParameter(cmdInsert, "@count", DbType.Int32, 0);

        foreach (Lemma lemma in lemmata)
        {
            cmdInsert.Parameters["@pos"].Value =
                GetTruncatedString(lemma.Pos, POS_MAX)
                ?? (object)DBNull.Value;
            cmdInsert.Parameters["@language"].Value =
                GetTruncatedString(lemma.Language, LANGUAGE_MAX)
                ?? (object)DBNull.Value;
            cmdInsert.Parameters["@value"].Value =
                GetTruncatedString(lemma.Value, VALUE_MAX);
            cmdInsert.Parameters["@reversed_value"].Value =
                GetTruncatedString(lemma.ReversedValue, VALUE_MAX);
            cmdInsert.Parameters["@count"].Value = lemma.Count;

            await cmdInsert.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Insert lemmata into the database, fetching them from the list of words,
    /// grouped by their language, POS and lemma.
    /// </summary>
    /// <remarks>This works as follows:
    /// <para>- count the total pages of words to fetch;</para>
    /// <para>- fetch each word page, where words are grouped by language, 
    /// POS, and lemma(excluding words having a NULL lemma);</para>
    /// <para>- for each lemma, add it to the lemmata table(in batches for
    /// better performance); the lemma count is equal to the sum of all its
    /// words counts;</para>
    /// <para>- finally, assign FK lemma_id to each word which was grouped into
    /// a lemma, and to each span having a word_id FK and belonging to the same
    /// lemma.</para>
    /// </remarks>
    /// <param name="connection">The connection.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <param name="progress">The progress reporter.</param>
    private async Task InsertLemmataAsync(IDbConnection connection,
        int pageSize,
        CancellationToken cancel,
        IProgress<ProgressReport>? progress = null)
    {
        ProgressReport? report = new();
        const int batchSize = 1000;
        List<Lemma> lemmata = new(batchSize);

        // prepare insert command
        IDbConnection connection2 = GetConnection();
        connection2.Open();

        // get the total counts of lemmata and their pages
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM(\n" +
            "SELECT pos, language, LOWER(lemma)\n" +
            "FROM word\n" +
            "WHERE lemma IS NOT NULL\n" +
            "GROUP BY pos, language, LOWER(lemma))\nAS s;";
        object? result = await cmd.ExecuteScalarAsync();
        if (result == null) return;
        long? total = result as long?;
        if (total == null || total.Value == 0) return;
        int pageCount = (int)Math.Ceiling((double)total.Value / pageSize);

        // prepare fetch command
        const string sql =
            "SELECT pos, language, LOWER(lemma) AS value, SUM(count) AS count\n" +
            "FROM word\n" +
            "WHERE lemma IS NOT NULL\n" +
            "GROUP BY pos, language, LOWER(lemma)\n" +
            "ORDER BY LOWER(lemma)\n";
        cmd.CommandText = sql + SqlHelper.BuildPaging(0, pageSize);

        // process by pages
        int offset = 0;
        for (int i = 0; i < pageCount; i++)
        {
            await using (DbDataReader reader = await cmd.ExecuteReaderAsync())
            {
                // for each lemma in the page
                while (await reader.ReadAsync())
                {
                    if (cancel.IsCancellationRequested) break;

                    // materialize the lemma
                    Lemma lemma = new()
                    {
                        Pos = await reader.IsDBNullAsync(0)
                            ? null : reader.GetString(0),
                        Language = await reader.IsDBNullAsync(1)
                            ? null : reader.GetString(1),
                        Value = reader.GetString(2),
                        Count = reader.GetInt32(3)
                    };
                    lemma.ReversedValue = lemma.Value.Length > 1
                        ? new string([.. lemma.Value.Reverse()])
                        : lemma.Value;

                    // skip non-letter lemmata
                    if (!lemma.Value.All(char.IsLetter)) continue;

                    // add the lemma to the batch
                    lemmata.Add(lemma);

                    // if the batch is full, flush it
                    if (lemmata.Count == batchSize)
                    {
                        await BatchInsertLemmata(connection2, lemmata);
                        lemmata.Clear();
                    }
                }
            }

            // move to the next page
            offset += pageSize;
            cmd.CommandText = sql + SqlHelper.BuildPaging(offset, pageSize);

            // report progress if requested
            if (progress != null)
            {
                report.Percent = (i + 1) * 100 / pageCount;
                report.Message = "Inserting lemmata...";
                progress.Report(report);
            }
        }

        // if there are any lemmata left in the batch, flush them
        if (lemmata.Count > 0)
            await BatchInsertLemmata(connection2, lemmata);

        // update lemma ID FK in word table
        report.Message = "Updating word table...";
        progress?.Report(report);
        cmd.CommandText = "UPDATE word SET lemma_id=lemma.id\n" +
            "FROM lemma\n" +
            "WHERE COALESCE (word.pos,'')=COALESCE(lemma.pos, '')\n" +
            "AND COALESCE (word.language,'')=COALESCE(lemma.language, '')\n" +
            "AND word.lemma = lemma.value\n" +
            "AND lemma IS NOT NULL;";
        await cmd.ExecuteNonQueryAsync();

        // update lemma ID FK in span table - only for spans that already have
        // word_id. This ensures we respect the same exclusions that were
        // applied during word collection
        report.Message = "Updating span table...";
        progress?.Report(report);
        cmd.CommandText = "UPDATE span SET lemma_id=lemma.id\n" +
            "FROM lemma\n" +
            "WHERE COALESCE (span.pos,'')=COALESCE(lemma.pos, '')\n" +
            "AND COALESCE (span.language,'')=COALESCE(lemma.language, '')\n" +
            "AND span.lemma = lemma.value\n" +
            "AND lemma IS NOT NULL\n" +
            // only update spans that have a word_id, ensuring consistency
            // with word exclusions
            "AND span.word_id IS NOT NULL;";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets the distinct names of all document's attributes.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <returns>Names.</returns>
    private static async Task<HashSet<string>> GetDocAttrNamesAsync(
        IDbConnection connection)
    {
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT name FROM document_attribute;";

        HashSet<string> names = [];
        using DbDataReader reader = await cmd.ExecuteReaderAsync();
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
    /// <param name="integer">True to use integer bins.</param>
    /// <param name="binCount">The desired bins count.</param>
    /// <returns>List of pairs.</returns>
    private static async Task<IList<DocumentPair>> ReadDocumentPairMinMaxAsync(
        DbCommand cmd, string name, bool privileged, bool integer, int binCount)
    {
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            if (await reader.IsDBNullAsync(0)) return [];

            // if fields are strings, read as string and then parse double
            double min, max;
            if (reader.GetFieldType(0) == typeof(string))
            {
                string n = reader.GetString(0);
                string m = reader.GetString(1);

                if (string.IsNullOrEmpty(n) || string.IsNullOrEmpty(m))
                    return [];
                min = double.Parse(n, CultureInfo.InvariantCulture);
                max = double.Parse(m, CultureInfo.InvariantCulture);
                return DocumentPair.GenerateBinPairs(name, privileged, integer,
                    min, max, binCount);
            }
            else
            {
                min = reader.GetDouble(0);
                max = reader.GetDouble(1);
            }

            return DocumentPair.GenerateBinPairs(name, privileged, integer,
                min, max, binCount);
        }
        return [];
    }

    private static (string, bool) ParseBinName(string name)
    {
        return name.StartsWith('^')
            ? (name[1..], true)
            : (name, false);
    }

    /// <summary>
    /// Gets all the document attributes name=value pairs to be used when filling
    /// word and lemma document counts.
    /// </summary>
    /// <param name="binCounts">The desired bins counts. For each attribute
    /// (either privileged or not) which must be handled as a number,
    /// this dictionary includes its name as the key, and the desired count
    /// of bins as the value. For instance, an attribute named <c>year</c>
    /// whose value is a year number would have an entry with key=<c>year</c>
    /// and value=<c>3</c>, meaning that we want to distribute its values in
    /// 3 bins.</param>
    /// <param name="excludedAttrNames">The names of the non-privileged
    /// attributes to be excluded from the pairs. All the names of
    /// non-categorical attributes (like e.g. the file path of a document,
    /// which is unique for each document) should be excluded.</param>
    /// <returns>Built pairs.</returns>
    /// <exception cref="ArgumentNullException">binCounts or excludedAttrNames
    /// </exception>
    public async Task<IList<DocumentPair>> GetDocumentPairsAsync(
        IDictionary<string, int> binCounts,
        HashSet<string> excludedAttrNames)
    {
        ArgumentNullException.ThrowIfNull(binCounts);
        ArgumentNullException.ThrowIfNull(excludedAttrNames);

        // open a connection to the database
        using IDbConnection connection = GetConnection();
        connection.Open();

        // prepare the set of privileged document attributes
        HashSet< string> privilegedDocAttrs = [
            .. TextSpan.GetPrivilegedAttrs(false).Except(
                ["title", "source", "profile_id", "sort_key"])];
        List<DocumentPair> pairs = [];
        StringBuilder sql = new();

        // (A) non-numeric:
        // (A.1) privileged
        foreach (string name in privilegedDocAttrs
            .Where(n => !binCounts.ContainsKey(n) &&
                        !binCounts.ContainsKey("^" + n)))
        {
            if (sql.Length > 0) sql.Append("UNION\n");

            sql.Append(
                $"SELECT DISTINCT '{name}' AS name, {name} AS value, true AS p " +
                "FROM document\n");
        }

        // (A.2) non-privileged
        IList<string> docAttrNames = [.. 
            (await GetDocAttrNamesAsync(connection)).Except(excludedAttrNames)];

        foreach (string name in docAttrNames
            .Where(n => !binCounts.ContainsKey(n) &&
                        !binCounts.ContainsKey("^" + n) &&
                        !privilegedDocAttrs.Contains(n) &&
                        !privilegedDocAttrs.Contains("^" + n)))
        {
            if (sql.Length > 0) sql.Append("UNION\n");

            sql.Append("SELECT DISTINCT name, value, false as p " +
                $"FROM document_attribute WHERE name='{name}'\n");
        }

        // fetch pairs from the generated SQL queries
        await using (DbCommand cmd = (DbCommand)connection.CreateCommand())
        {
            cmd.CommandText = sql.ToString();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                DocumentPair pair = new(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetBoolean(2));
                pairs.Add(pair);
            }
        }

        // (B): numeric
        await using (DbCommand cmd = (DbCommand)connection.CreateCommand())
        {
            // (B.1): numeric, privileged
            foreach (var pair in binCounts.Where(
                p => privilegedDocAttrs.Contains(p.Key) ||
                     privilegedDocAttrs.Contains("^" + p.Key)))
            {
                // a name prefixed by ^ means we want integer bins
                (string name, bool integer) = ParseBinName(pair.Key);

                cmd.CommandText = $"SELECT MIN({name}) AS n," +
                    $"MAX({name}) AS m FROM document";
                pairs.AddRange(await ReadDocumentPairMinMaxAsync(cmd,
                    name, true, integer, pair.Value));
            }

            // (B.2): numeric, non-privileged
            foreach (var pair in binCounts.Where(
                p => !privilegedDocAttrs.Contains(p.Key) &&
                     !privilegedDocAttrs.Contains("^" + p.Key)))
            {
                // a name prefixed by ^ means we want integer bins
                (string name, bool integer) = ParseBinName(pair.Key);

                cmd.CommandText = "SELECT MIN(value) AS n, MAX(value) AS m\n" +
                    "FROM document_attribute\n" +
                    $"WHERE name='{name}'";
                pairs.AddRange(
                    await ReadDocumentPairMinMaxAsync(cmd, name, false, integer,
                    pair.Value));
            }
        }

        return pairs;
    }

    /// <summary>
    /// Appends a document pair clause to the SQL query.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <param name="pair">The pair.</param>
    /// <param name="sql">The SQL builder.</param>
    private void AppendDocPairClause(string table, DocumentPair pair,
        StringBuilder sql)
    {
        // if the pair is numeric, use a numeric comparison within a defined range
        if (pair.IsNumeric || pair.Value == null)
        {
            string nn = SqlHelper.BuildTextAsNumber($"{table}.{pair.Name}");
            sql.Append(nn)
               .Append(">=")
               .AppendFormat(CultureInfo.InvariantCulture,
                    pair.MinValue % 1 == 0
                    ? "{0:0}" : "{0:0.00}", pair.MinValue)
               .Append(" AND ")
               .Append(nn)
               .Append('<')
               .AppendFormat(CultureInfo.InvariantCulture,
                    pair.MaxValue % 1 == 0
                    ? "{0:0}" : "{0:0.00}", pair.MaxValue);
        }
        // else, use a string comparison
        else
        {
            sql.Append($"{table}.{pair.Name}='{pair.Value}'");
        }
    }

    /// <summary>
    /// Append a document attribute pair clause to the SQL query.
    /// </summary>
    /// <param name="table">The table name.</param>
    /// <param name="pair">The pair.</param>
    /// <param name="sql">The SQL builder.</param>
    private void AppendDocAttrPairClause(string table, DocumentPair pair,
        StringBuilder sql)
    {
        // if the pair is numeric, use a numeric comparison within a defined range
        if (pair.IsNumeric)
        {
            string nn = SqlHelper.BuildTextAsNumber($"{table}.value");
            sql.Append(nn)
               .Append(">=")
               .AppendFormat(CultureInfo.InvariantCulture, "{0:F2}", pair.MinValue)
               .Append(" AND ")
               .Append(nn)
               .Append('<')
               .AppendFormat(CultureInfo.InvariantCulture, "{0:F2}", pair.MaxValue)
               .Append(" AND ")
               .Append($"{table}.name='{SqlHelper.SqlEncode(pair.Name)}'");
        }
        // else, use a string comparison
        else
        {
            sql.Append($"{table}.value='{SqlHelper.SqlEncode(pair.Value!)}' " +
                $"AND {table}.name='{SqlHelper.SqlEncode(pair.Name)}'");
        }
    }

    /// <summary>
    /// Insert a group of word counts.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="counts">The counts.</param>
    protected virtual async Task BatchInsertWordCounts(IDbConnection connection,
        List<WordCount> counts)
    {
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "INSERT INTO word_count(" +
            "word_id, lemma_id, doc_attr_name, doc_attr_value, count)\n" +
            "VALUES(@word_id, @lemma_id, @doc_attr_name, @doc_attr_value, @count);";
        AddParameter(cmd, "@word_id", DbType.Int32, 0);
        AddParameter(cmd, "@lemma_id", DbType.Int32, 0);
        AddParameter(cmd, "@doc_attr_name", DbType.String, "");
        AddParameter(cmd, "@doc_attr_value", DbType.String, "");
        AddParameter(cmd, "@count", DbType.Int32, 0);

        foreach (WordCount count in counts)
        {
            cmd.Parameters["@word_id"].Value = count.WordId;
            cmd.Parameters["@lemma_id"].Value = count.LemmaId == 0
                ? DBNull.Value : count.LemmaId;
            cmd.Parameters["@doc_attr_name"].Value =
                GetTruncatedString(count.Pair.Name, ATTR_NAME_MAX);
            cmd.Parameters["@doc_attr_value"].Value =
                count.Pair.Value ??
                $"{count.Pair.MinValue:F2}:{count.Pair.MaxValue:F2}";
            cmd.Parameters["@count"].Value = count.Value;

            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Inserts word counts of the specified word and lemma for the specified
    /// document's attributes name=value pairs.
    /// </summary>
    /// <param name="docPairs">The document's name=value attributes pairs to
    /// calculate counts for.</param>
    /// <param name="wordId">The word's ID.</param>
    /// <param name="lemmaId">The lemma's ID.</param>
    private async Task InsertWordCountsAsyncFor(IList<DocumentPair> docPairs,
        int wordId, int lemmaId)
    {
        // open a connection to the database
        using IDbConnection connection2 = GetConnection();
        connection2.Open();

        // parallelize the counting of occurrences for each document pair
        const int batchSize = 1000;
        ConcurrentBag<WordCount> counts = [];
        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        // for each document pair
        await Parallel.ForEachAsync(docPairs, options, async (pair, _) =>
        {
            // open a new connection
            using IDbConnection connection = GetConnection();
            connection.Open();
            StringBuilder sql = new();

            // if the pair is privileged, use the document table, else use
            // the document_attribute table via INNER JOIN
            if (pair.IsPrivileged)
            {
                sql.Append("SELECT COUNT(s.id) FROM span s\n" +
                    "INNER JOIN document d ON s.document_id=d.id\n" +
                    $"WHERE word_id={wordId} AND\n");
                AppendDocPairClause("d", pair, sql);
            }
            else
            {
                sql.Append("SELECT COUNT(s.id) FROM span s\n" +
                    "INNER JOIN document_attribute da ON " +
                    "s.document_id=da.document_id\n" +
                    $"WHERE word_id={wordId} AND\n");
                AppendDocAttrPairClause("da", pair, sql);
            }

            // execute SQL getting count
            object? result = null;
            await using (DbCommand cmd = (DbCommand)connection.CreateCommand())
            {
                cmd.CommandText = sql.ToString();
                result = await cmd.ExecuteScalarAsync(CancellationToken.None);
            }
            if (result != null)
            {
                // materialize the count
                WordCount count = new(wordId, lemmaId, pair,
                    Convert.ToInt32(result));

                // add the count to the bag
                counts.Add(count);

                // if the bag is full, flush it
                if (counts.Count == batchSize)
                {
                    await BatchInsertWordCounts(connection2, [.. counts]);
                    counts.Clear();
                }
            }
            sql.Clear();
        });

        // if there are any counts left in the bag, flush them
        if (!counts.IsEmpty)
            await BatchInsertWordCounts(connection2, [.. counts]);
    }

    /// <summary>
    /// Gets the word's lemma IDs for words in the specified range.
    /// </summary>
    /// <param name="firstId">The ID of the first word to get.</param>
    /// <param name="lastId">The ID of the last word to get.</param>
    /// <returns>List of tuples with each word's ID and lemma ID.</returns>
    private async Task<List<Tuple<int, int>>> GetWordLemmaIdsAsync(
        int firstId, int lastId)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "SELECT id, lemma_id, value FROM word " +
            "WHERE id>=@first_id AND id<=@last_id;";
        AddParameter(cmd, "@first_id", DbType.Int32, firstId);
        AddParameter(cmd, "@last_id", DbType.Int32, lastId);
        List<Tuple<int, int>> ids = [];

        await using DbDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            // ignore non-letter words
            string value = reader.GetString(2);
            if (string.IsNullOrEmpty(value) || !value.All(char.IsLetter))
                continue;

            ids.Add(Tuple.Create(
                reader.GetInt32(0),
                await reader.IsDBNullAsync(1) ? 0 : reader.GetInt32(1)));
        }
        return ids;
    }

    /// <summary>
    /// Inserts counts for all words for the specified document's attributes
    /// name=value pairs.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="docPairs">The document's attributes name=value pairs. Each
    /// pair defines the context for a count.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <param name="progress">The optional progress reporter.</param>
    private async Task InsertWordCountsAsync(IDbConnection connection,
        IList<DocumentPair> docPairs, CancellationToken cancel,
        IProgress<ProgressReport>? progress = null)
    {
        // get max word ID
        int maxId = GetMax(connection, "word", "id");
        if (maxId == 0) return;

        // open a second connection for batch insert
        IDbConnection connection2 = GetConnection();
        connection2.Open();

        // prepare fetch command starting from page 1
        const int pageSize = 1000;
        ProgressReport report = new();

        // for each page of words
        int pageCount = (int)Math.Ceiling((double)maxId / pageSize);

        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            // fetch a page of word and lemma IDs
            int firstId = (pageIndex * pageSize) + 1;
            int lastId = (pageIndex + 1) * pageSize;
            List<Tuple<int, int>> wlIds =
                await GetWordLemmaIdsAsync(firstId, lastId);
            if (wlIds.Count == 0) return;

            // for each word and lemma ID, count occurrences in all pairs
            int wlCount = 0;
            foreach ((int wordId, int lemmaId) in wlIds)
            {
                // insert word counts for the current word and lemma
                await InsertWordCountsAsyncFor(docPairs, wordId, lemmaId);

                // check for cancellation
                if (cancel.IsCancellationRequested) break;

                // report progress if requested
                if (progress != null)
                {
                    wlCount++;
                    report.Percent = wlCount * 100 / wlIds.Count;
                    report.Message = $"{pageIndex + 1}/{pageCount}: " +
                        $"{wlCount}/{wlIds.Count}";
                    progress.Report(report);
                }
            }
        }
    }

    private static async Task InsertLemmaCountsAsync(IDbConnection connection)
    {
        DbCommand cmd = (DbCommand)connection.CreateCommand();
        cmd.CommandText = "INSERT INTO lemma_count(" +
            "lemma_id, doc_attr_name, doc_attr_value, count)\n" +
            "SELECT lemma_id, doc_attr_name, doc_attr_value, SUM(count)\n" +
            "FROM word_count\n" +
            "WHERE lemma_id IS NOT NULL\n" +
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
    /// <param name="excludedSpanAttrNames">The names of the non-privileged
    /// span attributes to be excluded from the words index. This is used
    /// to remove tokens like proper names, foreign words, etc.</param>
    /// <param name="excludedPosValues">The POS values to be excluded from
    /// the index. These are usually like PROPN, NUM, SYM, X, and the like.</param>
    /// <param name="cancel">The cancellation token.</param>
    /// <param name="progress">The progress.</param>
    public async Task BuildWordIndexAsync(IDictionary<string, int> binCounts,
        HashSet<string> excludedAttrNames,
        HashSet<string> excludedSpanAttrNames,
        HashSet<string> excludedPosValues,
        CancellationToken cancel,
        IProgress<ProgressReport>? progress = null)
    {
        const int pageSize = 100;
        ProgressReport report = new();

        using IDbConnection connection = GetConnection();
        connection.Open();

        report.Message = "Clearing word index...";
        progress?.Report(report);
        ClearWordIndex(connection);

        report.Message = "Inserting words...";
        progress?.Report(report);
        await InsertWordsAsync(connection, pageSize, excludedSpanAttrNames,
            excludedPosValues, cancel, progress);

        report.Message = "Inserting lemmata...";
        progress?.Report(report);
        await InsertLemmataAsync(connection, pageSize, cancel, progress);

        report.Message = "Collecting document pairs...";
        progress?.Report(report);
        IList<DocumentPair> docPairs = await GetDocumentPairsAsync(
            binCounts, excludedAttrNames);

        report.Message = "Calculating word counts...";
        progress?.Report(report);
        await InsertWordCountsAsync(connection, docPairs, cancel, progress);

        report.Message = "Calculating lemma counts...";
        progress?.Report(report);
        await InsertLemmaCountsAsync(connection);
    }

    /// <summary>
    /// Finalizes the index by eventually adding calculated data into it.
    /// </summary>
    public void FinalizeIndex()
    {
    }
    #endregion

    /// <summary>
    /// Upserts the specified span.
    /// </summary>
    /// <param name="span">The span.</param>
    /// <param name="connection">The connection.</param>
    protected abstract void UpsertSpan(TextSpan span, IDbConnection connection);
}
