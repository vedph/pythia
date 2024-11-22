using Corpus.Core;
using Fusi.Tools.Configuration;
using Fusi.Tools.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Corpus.Sql;

/// <summary>
/// Base class for SQL-based corpus repositories.
/// </summary>
public abstract class SqlCorpusRepository : IConfigurable<SqlRepositoryOptions>
{
    /// <summary>
    /// Gets the connection string.
    /// </summary>
    protected string? ConnectionString { get; private set; }

    /// <summary>
    /// Configures the specified options. This sets the connection string.
    /// If overriding this for more options, be sure to call the base
    /// implementation.
    /// </summary>
    /// <param name="options">The options.</param>
    public virtual void Configure(SqlRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ConnectionString = options.ConnectionString;
    }

    /// <summary>
    /// Gets a new connection object.
    /// </summary>
    /// <returns>The connection.</returns>
    public abstract IDbConnection GetConnection();

    /// <summary>
    /// Adds a parameter to the specified command.
    /// </summary>
    /// <param name="cmd">The command.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="type">The parameter type.</param>
    /// <param name="value">The optional parameter value.</param>
    /// <returns>The parameter.</returns>
    protected static IDbDataParameter AddParameter(IDbCommand cmd,
        string name, DbType type, object? value = null)
    {
        IDbDataParameter p = cmd.CreateParameter();
        p.DbType = type;
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
        return p;
    }

    /// <summary>
    /// Builds the paging expression with the specified values.
    /// </summary>
    /// <param name="offset">The offset count.</param>
    /// <param name="limit">The limit count.</param>
    /// <returns>SQL code.</returns>
    protected abstract string GetPagingSql(int offset, int limit);

    protected static int GetTotal(IDbCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        int total = 0;
        using (var reader = command.ExecuteReader())
        {
            if (reader.Read()) total = reader.GetInt32(0);
        }
        return total;
    }

    #region Corpus
    private static Core.Corpus ReadCorpus(IDataReader reader)
    {
        return new Core.Corpus
        {
            Id = reader.GetString(0),
            Title = reader.GetString(1),
            Description = reader.GetString(2),
            UserId = reader.IsDBNull(3) ? null : reader.GetString(3),
        };
    }

    /// <summary>
    /// Gets the corpus with the specified ID.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>corpus or null if not found</returns>
    public ICorpus? GetCorpus(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        using IDbConnection connection = GetConnection();
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, title, description, user_id " +
            "FROM corpus WHERE id=@id;";
        AddParameter(cmd, "@id", DbType.String, id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        var corpus = ReadCorpus(reader);
        reader.Close();
        return corpus;
    }

    /// <summary>
    /// Appends WHERE or AND according to whether <paramref name="sb"/>
    /// is empty or not.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <returns>The string builder.</returns>
    protected static StringBuilder AppendWhereOrAnd(StringBuilder sb) =>
        sb.Append(sb.Length == 0 ? " WHERE " : " AND ");

    /// <summary>
    /// Gets the specified page of corpora matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="includeDocIds">if set to <c>true</c>, include
    /// documents IDs for each corpus; if <c>false</c>, just add a single
    /// number to each corpus documents array representing the total count
    /// of the documents included in that corpus.</param>
    /// <returns>page</returns>
    public DataPage<ICorpus> GetCorpora(CorpusFilter filter,
        bool includeDocIds)
    {
        ArgumentNullException.ThrowIfNull(filter);

        using IDbConnection connection = GetConnection();
        connection.Open();
        var cmd = connection.CreateCommand();

        StringBuilder clauseSql = new();

        if (!string.IsNullOrEmpty(filter.Id))
        {
            AddParameter(cmd, "@id", DbType.String, filter.Id);
            AppendWhereOrAnd(clauseSql).Append(
                "id LIKE CONCAT('%', @id, '%')");
        }

        if (!string.IsNullOrEmpty(filter.Prefix))
        {
            AddParameter(cmd, "@prefix", DbType.String, filter.Prefix);
            AppendWhereOrAnd(clauseSql).Append(
                "id LIKE CONCAT(@prefix, '%')");
        }

        if (!string.IsNullOrEmpty(filter.Title))
        {
            AddParameter(cmd, "@title", DbType.String, filter.Title);
            AppendWhereOrAnd(clauseSql).Append(
                "LOWER(title) LIKE CONCAT('%', LOWER(@title), '%')");
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            AddParameter(cmd, "@user_id", DbType.String, filter.UserId);
            AppendWhereOrAnd(clauseSql).Append("user_id=@user_id");
        }

        // total:
        // select count(id) from corpus where...
        cmd.CommandText = "SELECT COUNT(id) FROM corpus\n"
            + clauseSql.ToString();
        int total = GetTotal(cmd);
        if (total == 0)
        {
            return new DataPage<ICorpus>(filter.PageNumber,
                filter.PageSize, 0, Array.Empty<ICorpus>());
        }

        // data:
        // select ... from corpus where... offset limit order by
        StringBuilder dataSql = new();
        dataSql.AppendLine("SELECT corpus.id, corpus.title, " +
            "corpus.description, user_id FROM corpus")
            .AppendLine(clauseSql.ToString())
            .AppendLine("ORDER BY corpus.title, corpus.id")
            .AppendLine(GetPagingSql(filter.GetSkipCount(), filter.PageSize));
        cmd.CommandText = dataSql.ToString();

        using IDataReader reader = cmd.ExecuteReader();
        List<ICorpus> corpora = [];
        while (reader.Read()) corpora.Add(ReadCorpus(reader));
        reader.Close();

        // sub-query for matching corpora in page
        IDbCommand cmdSub = connection.CreateCommand();
        cmdSub.CommandText = includeDocIds
            ? "SELECT document_id\n" +
              "FROM document_corpus dc WHERE dc.corpus_id=@corpus_id;"
            : "SELECT COUNT(document_id)\n" +
              "FROM document_corpus dc WHERE dc.corpus_id=@corpus_id;";
        AddParameter(cmdSub, "@corpus_id", DbType.String, null);

        foreach (ICorpus corpus in corpora)
        {
            ((DbParameter)cmdSub.Parameters["@corpus_id"]).Value = corpus.Id;
            corpus.DocumentIds ??= [];

            if (includeDocIds)
            {
                using IDataReader subReader = cmdSub.ExecuteReader();
                while (subReader.Read())
                    corpus.DocumentIds.Add(subReader.GetInt32(0));
                subReader.Close();
            }
            else
            {
                object? result = cmdSub.ExecuteScalar();
                corpus.DocumentIds.Add(result != null
                    ? Convert.ToInt32(result) : 0);
            }
        }

        return new DataPage<ICorpus>(filter.PageNumber, filter.PageSize,
            total, corpora);
    }

    /// <summary>
    /// Upserts <paramref name="corpus"/>.
    /// </summary>
    /// <param name="corpus">The corpus.</param>
    /// <param name="connection">The connection.</param>
    /// <param name="tr">An optional transaction.</param>
    public abstract void UpsertCorpus(ICorpus corpus,
        IDbConnection connection, IDbTransaction? tr = null);

    /// <summary>
    /// Adds or updates the specified corpus.
    /// </summary>
    /// <param name="corpus">The corpus.</param>
    /// <param name="sourceId">The optional source corpus ID when the new
    /// corpus should get its content. This is useful to clone an existing
    /// corpus into a new one.</param>
    /// <exception cref="ArgumentNullException">null corpus</exception>
    public void AddCorpus(ICorpus corpus, string? sourceId = null)
    {
        ArgumentNullException.ThrowIfNull(corpus);

        using IDbConnection connection = GetConnection();
        connection.Open();

        using IDbTransaction tr = connection.BeginTransaction();
        try
        {
            UpsertCorpus(corpus, connection, tr);

            if (corpus.DocumentIds?.Count > 0)
            {
                // remove all the documents in the new corpus
                IDbCommand cmd = connection.CreateCommand();
                cmd.Transaction = tr;
                cmd.CommandText = "DELETE FROM document_corpus " +
                    "WHERE corpus_id=@corpus_id;";
                AddParameter(cmd, "@corpus_id", DbType.String, corpus.Id);
                cmd.ExecuteNonQuery();

                // insert documents in the new corpus
                cmd.CommandText = "INSERT INTO document_corpus" +
                    "(document_id, corpus_id) " +
                    "VALUES(@document_id, @corpus_id);";
                AddParameter(cmd, "@document_id", DbType.Int32);

                foreach (int id in corpus.DocumentIds)
                {
                    ((DbCommand)cmd).Parameters["@document_id"].Value = id;
                    cmd.ExecuteNonQuery();
                }
            }

            // copy contents from source if requested
            if (!string.IsNullOrEmpty(sourceId))
            {
                IDbCommand cmd = connection.CreateCommand();
                cmd.Transaction = tr;
                cmd.CommandText =
                    "INSERT INTO document_corpus(document_id, corpus_id) " +
                    "SELECT s.document_id, @corpus_id FROM document_corpus s " +
                    "WHERE corpus_id=@source_id;";
                AddParameter(cmd, "@corpus_id", DbType.String, corpus.Id);
                AddParameter(cmd, "@source_id", DbType.String, sourceId);
                cmd.ExecuteNonQuery();
            }

            tr.Commit();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            tr.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Deletes the corpus with the specified ID.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <exception cref="ArgumentNullException">null ID</exception>
    public void DeleteCorpus(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        using IDbConnection connection = GetConnection();
        connection.Open();
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM corpus WHERE id=@id;";
        AddParameter(cmd, "@id", DbType.String, id);
        cmd.ExecuteNonQuery();
    }

    private static HashSet<int> GetDocumentIds(DocumentFilter filter,
        IDbConnection connection)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT DISTINCT id FROM document";
        cmd.CommandText += GetDocumentFilterSql(filter, cmd) + ";";

        HashSet<int> ids = [];
        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read()) ids.Add(reader.GetInt32(0));
        reader.Close();
        return ids;
    }

    private static HashSet<int> GetCorpusDocumentIds(string corpusId,
        IDbConnection connection)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT document_id " +
            "FROM document_corpus " +
            "WHERE corpus_id=@corpus_id;";
        AddParameter(cmd, "@corpus_id", DbType.String, corpusId);

        HashSet<int> ids = [];
        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read()) ids.Add(reader.GetInt32(0));
        reader.Close();
        return ids;
    }

    private void EnsureCorpusExists(string id, string? userId,
        IDbConnection connection)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM corpus WHERE id=@id;";
        AddParameter(cmd, "@id", DbType.String, id);
        int? result = cmd.ExecuteScalar() as int?;
        if (result == 1) return;

        // add corpus
        UpsertCorpus(new Core.Corpus
        {
            Id = id,
            Title = id,
            Description = "",
            UserId = userId,
        }, connection);
    }

    /// <summary>
    /// Adds the specified documents to the specified corpus. If the corpus
    /// does not exist, it will be created.
    /// </summary>
    /// <param name="corpusId">The corpus identifier.</param>
    /// <param name="userId">The user identifier to assign to the corpus
    /// when it has to be created.</param>
    /// <param name="documentIds">The document(s) ID(s).</param>
    /// <exception cref="ArgumentNullException">corpusId</exception>
    public void AddDocumentsToCorpus(string corpusId, string? userId,
        params int[] documentIds)
    {
        ArgumentNullException.ThrowIfNull(corpusId);

        using IDbConnection connection = GetConnection();
        connection.Open();

        EnsureCorpusExists(corpusId, userId, connection);
        IDbTransaction trans = connection.BeginTransaction();
        try
        {
            IDbCommand cmdCheck = connection.CreateCommand();
            cmdCheck.Transaction = trans;
            cmdCheck.CommandText = "SELECT 1 FROM document_corpus " +
                "WHERE document_id=@document_id AND corpus_id=@corpus_id;";
            AddParameter(cmdCheck, "@document_id", DbType.Int32);
            AddParameter(cmdCheck, "@corpus_id", DbType.String, corpusId);

            IDbCommand cmdIns = connection.CreateCommand();
            cmdIns.Transaction = trans;
            cmdIns.CommandText =
                "INSERT INTO document_corpus(document_id, corpus_id) " +
                "VALUES(@document_id, @corpus_id);";
            AddParameter(cmdIns, "@document_id", DbType.Int32);
            AddParameter(cmdIns, "@corpus_id", DbType.String, corpusId);

            // insert
            foreach (int id in documentIds)
            {
                // ensure that the link does not already exists
                ((DbCommand)cmdCheck).Parameters["@document_id"].Value = id;
                object? result = cmdCheck.ExecuteScalar();
                if (result != null && (int)result == 1) continue;

                ((DbCommand)cmdIns).Parameters["@document_id"].Value = id;
                cmdIns.ExecuteNonQuery();
            }
            trans.Commit();
        }
        catch (Exception)
        {
            trans.Rollback();
            throw;
        }
    }

    /// <summary>
    /// True if the document with the specified ID is included in the corpus
    /// with the specified ID.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="corpusId">The corpus ID.</param>
    /// <param name="matchAsPrefix">True to treat <paramref name="corpusId"/> as
    /// a prefix, so that any corpus ID starting with it is a match.</param>
    /// <returns>True if included; otherwise, false.</returns>
    public bool IsDocumentInCorpus(int documentId, string corpusId,
        bool matchAsPrefix)
    {
        ArgumentNullException.ThrowIfNull(corpusId);

        using IDbConnection connection = GetConnection();
        connection.Open();

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM document_corpus " +
            "WHERE document_id=@document_id AND corpus_id" +
            (matchAsPrefix? " LIKE CONCAT(@corpus_id, '%')" : "=@corpus_id");
        AddParameter(cmd, "@document_id", DbType.Int32, documentId);
        AddParameter(cmd, "@corpus_id", DbType.String, corpusId);
        object? result = cmd.ExecuteScalar();
        return result != null && (int) result == 1;
    }

    /// <summary>
    /// Changes the corpus by specifying a documents filter.
    /// </summary>
    /// <param name="corpusId">The corpus ID, which can also be a new one.
    /// In this case, the corpus will be created.</param>
    /// <param name="userId">The user ID to be assigned to a new corpus.</param>
    /// <param name="filter">The documents filter. If empty, nothing is
    /// changed.</param>
    /// <param name="add">if set to <c>true</c>, the matching documents
    /// will be added to the corpus; if set to <c>false</c>, they will be
    /// removed.</param>
    /// <exception cref="ArgumentNullException">null corpus ID or filter
    /// </exception>
    public virtual void ChangeCorpusByFilter(string corpusId, string? userId,
        DocumentFilter filter, bool add)
    {
        ArgumentNullException.ThrowIfNull(corpusId);
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.IsEmpty()) return;

        using IDbConnection connection = GetConnection();
        connection.Open();

        // get all the matching doc IDs
        HashSet<int> filteredIds = GetDocumentIds(filter, connection);

        // get all the corpus doc IDs
        HashSet<int> corpusIds = GetCorpusDocumentIds(corpusId, connection);

        // ensure that the target corpus exists, creating it if required.
        // We check this only when it happens to be empty
        if (corpusIds.Count == 0)
            EnsureCorpusExists(corpusId, userId, connection);

        // if adding, the IDs to add are those present in filtered IDs
        // but absent from corpus IDs
        IDbCommand cmd = connection.CreateCommand();
        AddParameter(cmd, "@document_id", DbType.Int32);
        AddParameter(cmd, "@corpus_id", DbType.String, corpusId);

        IEnumerable<int> selectedIds;
        if (add)
        {
            // if adding, the IDs to add are those present in filtered IDs
            // but absent from corpus IDs
            selectedIds = filteredIds.Except(corpusIds);
            cmd.CommandText =
                "INSERT INTO document_corpus(document_id, corpus_id) " +
                "VALUES(@document_id, @corpus_id);";
        }
        else
        {
            // if removing, the IDs to remove are those present in both
            selectedIds = filteredIds.Intersect(corpusIds);
            cmd.CommandText = "DELETE FROM document_corpus " +
                "WHERE document_id=@document_id AND corpus_id=@corpus_id;";
        }

        foreach (int id in selectedIds)
        {
            ((DbParameter)cmd.Parameters["@document_id"]).Value = id;
            cmd.ExecuteNonQuery();
        }
    }
    #endregion

    #region Document        
    /// <summary>
    /// Gets the SQL code for the specified filter, also adding parameters
    /// to the specified command.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="command">The command.</param>
    /// <param name="prefix">The prefix to prepend when the filter is not
    /// empty.</param>
    /// <param name="suffix">The suffix to prepend when the filter is not
    /// empty.</param>
    /// <returns>SQL code.</returns>
    protected static string GetDocumentFilterSql(DocumentFilter filter,
        IDbCommand command, string prefix = " WHERE ", string suffix = "")
    {
        StringBuilder sql = new();

        bool addedClause = false;
        if (!string.IsNullOrEmpty(filter.CorpusId))
        {
            AddParameter(command, "@corpus_id", DbType.String, filter.CorpusId);
            sql.Append("EXISTS(SELECT 1 FROM document_corpus dc " +
                "WHERE dc.document_id=document.id AND " +
                "dc.corpus_id=@corpus_id)");
            addedClause = true;
        }

        if (!string.IsNullOrEmpty(filter.CorpusIdPrefix))
        {
            AddParameter(command, "@corpus_id_prefix", DbType.String,
                filter.CorpusIdPrefix);
            sql.Append("EXISTS(SELECT 1 FROM document_corpus dc " +
                "WHERE dc.document_id=document.id AND " +
                "dc.corpus_id LIKE CONCAT(@corpus_id_prefix, '%'))");
            addedClause = true;
        }

        if (!string.IsNullOrEmpty(filter.Author))
        {
            AddParameter(command, "@author", DbType.String, filter.Author);
            if (addedClause) sql.Append("\nAND\n");
            sql.Append("LOWER(author) LIKE CONCAT('%', LOWER(@author), '%')");
            addedClause = true;
        }

        if (!string.IsNullOrEmpty(filter.Title))
        {
            AddParameter(command, "@title", DbType.String, filter.Title);
            if (addedClause) sql.Append("\nAND\n");
            sql.Append("LOWER(title) LIKE CONCAT('%', LOWER(@title), '%')");
            addedClause = true;
        }

        if (!string.IsNullOrEmpty(filter.Source))
        {
            AddParameter(command, "@source", DbType.String, filter.Source);
            if (addedClause) sql.Append("\nAND\n");
            sql.Append("LOWER(source) LIKE CONCAT('%', LOWER(@source), '%')");
            addedClause = true;
        }

        if (!string.IsNullOrEmpty(filter.ProfileId))
        {
            AddParameter(command, "@profile_id", DbType.String, filter.ProfileId);
            if (addedClause) sql.Append("\nAND\n");
            sql.Append("profile_id = @profile_id");
            addedClause = true;
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            AddParameter(command, "@user_id", DbType.String, filter.UserId);
            if (addedClause) sql.Append("\nAND\n");
            sql.Append("user_id = @user_id");
            addedClause = true;
        }

        if (!string.IsNullOrEmpty(filter.ProfileIdPrefix))
        {
            AddParameter(command, "@profile_id_prefix", DbType.String,
                filter.ProfileIdPrefix);
            if (addedClause) sql.Append("\nAND\n");
            sql.Append("profile_id LIKE CONCAT(@profile_id_prefix, '%')");
            addedClause = true;
        }

        if (filter.MinDateValue != 0)
        {
            AddParameter(command, "@min_date", DbType.Double, filter.MinDateValue);
            if (addedClause) sql.Append("\nAND\n");
            sql.Append("date_value >= @min_date");
            addedClause = true;
        }

        if (filter.MaxDateValue != 0)
        {
            AddParameter(command, "@max_date", DbType.Double, filter.MaxDateValue);
            if (addedClause) sql.Append("\nAND\n");
            sql.Append("date_value <= @max_date");
            addedClause = true;
        }

        if (filter.MinTimeModified.HasValue)
        {
            AddParameter(command, "@min_time_modified", DbType.DateTime,
                filter.MinTimeModified.Value);
            if (addedClause) sql.Append("\nAND\n");
            sql.Append("last_modified >= @min_time_modified");
            addedClause = true;
        }

        if (filter.MaxTimeModified.HasValue)
        {
            AddParameter(command, "@max_time_modified", DbType.DateTime,
                filter.MaxTimeModified.Value);
            if (addedClause) sql.Append("\nAND\n");
            sql.Append("last_modified <= @max_time_modified");
            addedClause = true;
        }

        if (filter.Attributes?.Count > 0)
        {
            if (addedClause) sql.Append("\nAND\n");
            sql.Append("EXISTS(\n" +
                "SELECT da.document_id FROM document_attribute da\n" +
                "WHERE da.document_id=document.id AND (\n");

            for (int i = 0; i < filter.Attributes.Count; i++)
            {
                Tuple<string, string> nv = filter.Attributes[i];
                if (i > 0) sql.Append("\nOR ");
                string n = $"@n{i + 1}";
                string v = $"@v{i + 1}";
                AddParameter(command, n, DbType.String, nv.Item1);
                AddParameter(command, v, DbType.String, nv.Item2);
                sql.Append("LOWER(da.name)=LOWER(").Append(n).Append(") ")
                   .Append("AND LOWER(da.value) LIKE CONCAT('%', LOWER(")
                   .Append(v).Append("), '%')");
            }

            sql.Append("))");
        }

        if (sql.Length > 0)
        {
            if (!string.IsNullOrEmpty(prefix)) sql.Insert(0, prefix);
            if (!string.IsNullOrEmpty(suffix)) sql.Append(suffix);
        }

        return sql.ToString();
    }

    private static IDocument ReadDocument(IDataReader reader,
        bool includeContent)
    {
        return new Document
        {
            Id = reader.GetInt32(0),
            Author = reader.GetString(1),
            Title = reader.GetString(2),
            Source = reader.GetString(3),
            ProfileId = reader.GetString(4),
            DateValue = reader.GetDouble(5),
            SortKey = reader.GetString(6),
            LastModified = reader.GetDateTime(7),
            UserId = reader.IsDBNull(8) ? null : reader.GetString(8),
            Content = includeContent && !reader.IsDBNull(9)
                ? reader.GetString(9) : null
        };
    }

    private IList<Core.Attribute> GetAttributes(int id, string tableName)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        IDbCommand cmd = connection.CreateCommand();
        AddParameter(cmd, "@id", DbType.Int32, id);
        cmd.CommandText = $"SELECT id, name, value, type FROM {tableName} " +
            "WHERE document_id=@id;";

        using IDataReader reader = cmd.ExecuteReader();
        List<Core.Attribute> attributes = [];
        while (reader.Read())
        {
            attributes.Add(new Core.Attribute
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Value = reader.GetString(2),
                Type = (AttributeType)reader.GetInt32(3),
                TargetId = id
            });
        }
        reader.Close();
        return attributes;
    }

    /// <summary>
    /// Gets the document with the specified ID.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="includeContent">If set to <c>true</c>, include the
    /// document's content.</param>
    /// <returns>document or null if not found</returns>
    public IDocument? GetDocument(int id, bool includeContent)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        // get document
        StringBuilder sb = new();
        sb.Append("SELECT id, author, title, source, " +
            "profile_id, date_value, sort_key, last_modified, user_id");
        if (includeContent) sb.Append(", content");
        sb.Append("\nFROM document WHERE id=@id;");

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sb.ToString();
        AddParameter(cmd, "@id", DbType.Int32, id);

        IDocument? document = null;
        using (IDataReader reader = cmd.ExecuteReader())
        {
            if (!reader.Read()) return null;
            document = ReadDocument(reader, includeContent);
        }

        // get its attributes if any
        document.Attributes = GetAttributes(id, "document_attribute");

        return document;
    }

    /// <summary>
    /// Gets the document with the specified source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="includeContent">If set to <c>true</c>, include content.
    /// </param>
    /// <returns>document or null if not found</returns>
    /// <exception cref="ArgumentNullException">null source</exception>
    public IDocument? GetDocumentBySource(string source, bool includeContent)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        // get document
        StringBuilder sb = new();
        sb.Append("SELECT id, author, title, source, " +
            "profile_id, date_value, sort_key, last_modified, user_id");
        if (includeContent) sb.Append(", content");
        sb.Append("\nFROM document WHERE source=@source;");

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sb.ToString();
        AddParameter(cmd, "@source", DbType.String, source);

        IDocument? document = null;
        using (IDataReader reader = cmd.ExecuteReader())
        {
            if (!reader.Read()) return null;
            document = ReadDocument(reader, includeContent);
        }

        // get its attributes if any
        document.Attributes = GetAttributes(document.Id, "document_attribute");

        return document;
    }

    /// <summary>
    /// Gets the specified page of documents matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>page</returns>
    /// <exception cref="ArgumentNullException">null filter</exception>
    public DataPage<IDocument> GetDocuments(DocumentFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        using IDbConnection connection = GetConnection();
        connection.Open();

        // select count(id) from document where...
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(id) FROM document" +
            GetDocumentFilterSql(filter, cmd) + ";";
        int total = GetTotal(cmd);
        if (total == 0)
        {
            return new DataPage<IDocument>(
                filter.PageNumber,
                filter.PageSize,
                0, Array.Empty<IDocument>());
        }

        // select... from document where...
        cmd = connection.CreateCommand();
        StringBuilder sql = new();
        sql.AppendLine("SELECT id, author, title, source, " +
            "profile_id, date_value, sort_key, last_modified, user_id " +
            "FROM document");
        sql.Append(GetDocumentFilterSql(filter, cmd, suffix: "\n"));

        // order by
        sql.Append("ORDER BY ");
        switch (filter.SortOrder)
        {
            case DocumentSortOrder.Author:
                sql.Append("author")
                   .Append(filter.IsSortDescending ? " DESC" : " ASC")
                   .Append(", title, date_value, id");
                break;
            case DocumentSortOrder.Title:
                sql.Append("title")
                   .Append(filter.IsSortDescending ? " DESC" : " ASC")
                   .Append(", author, date_value, id");
                break;
            case DocumentSortOrder.Date:
                sql.Append("date_value")
                   .Append(filter.IsSortDescending ? " DESC" : " ASC")
                   .Append(", author, title, id");
                break;
            default:
                sql.Append("sort_key, id");
                break;
        }

        // offset... limit...
        sql.AppendLine().Append(
            GetPagingSql(filter.GetSkipCount(), filter.PageSize));
        sql.Append(';');

        // read documents with their attributes
        cmd.CommandText = sql.ToString();
        using IDataReader reader = cmd.ExecuteReader();

        List<IDocument> documents = [];
        while (reader.Read())
        {
            IDocument document = ReadDocument(reader, false);
            document.Attributes = GetAttributes(
                document.Id, "document_attribute");
            documents.Add(document);
        }
        reader.Close();

        return new DataPage<IDocument>(filter.PageNumber, filter.PageSize,
            total, documents);
    }

    /// <summary>
    /// Upserts <paramref name="document"/>.
    /// </summary>
    /// <param name="document">The document. Its ID is updated when it
    /// is a new inserted record.</param>
    /// <param name="hasContent">if set to <c>true</c>, also modify the
    /// document's content; else do not update/insert it.</param>
    /// <param name="connection">The connection.</param>
    /// <param name="tr">An optional transaction.</param>
    public abstract void UpsertDocument(IDocument document, bool hasContent,
        IDbConnection connection, IDbTransaction? tr = null);

    private static IList<int> GetDocumentAttributeIds(int documentId,
        IDbConnection connection, IDbTransaction tr)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.Transaction = tr;
        cmd.CommandText = "SELECT id FROM document_attribute " +
            "WHERE document_id=@document_id;";
        AddParameter(cmd, "@document_id", DbType.Int32, documentId);
        List<int> ids = [];
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ids.Add(reader.GetInt32(0));
        }
        return ids;
    }

    private static void DeleteAttributeIds(IList<int> ids,
        IDbConnection connection, IDbTransaction tr)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.Transaction = tr;
        cmd.CommandText = "DELETE FROM document_attribute " +
            $"WHERE id IN({string.Join(",", ids)});";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Adds or updates the specified document. If the document has ID=0,
    /// it will be added; else the existing document with that ID will be
    /// updated.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="hasContent">If set to <c>true</c>, the document being
    /// passed has a content. Otherwise, the <see cref="IDocument.Content" />
    /// property will not be updated.</param>
    /// <param name="hasAttributes">If set to <c>true</c>, the attributes
    /// of an existing document should be updated.</param>
    /// <exception cref="ArgumentNullException">document</exception>
    public void AddDocument(IDocument document, bool hasContent,
        bool hasAttributes)
    {
        ArgumentNullException.ThrowIfNull(document);

        using IDbConnection connection = GetConnection();
        connection.Open();
        using IDbTransaction tr = connection.BeginTransaction();

        try
        {
            // document
            bool isNew = document.Id == 0;
            UpsertDocument(document, hasContent, connection, tr);

            // attributes
            if (hasAttributes && document.Attributes?.Count > 0)
            {
                // if it was updated, delete all the removed attributes
                if (!isNew)
                {
                    // get the existing document's attributes
                    IList<int> deletedIds = GetDocumentAttributeIds(
                        document.Id, connection, tr);
                    // any existing attribute not found in the new one
                    // was removed
                    for (int i = deletedIds.Count - 1; i > -1; i--)
                    {
                        if (document.Attributes.Any(a => a.Id == deletedIds[i]))
                            deletedIds.RemoveAt(i);
                    }
                    if (deletedIds.Count > 0)
                        DeleteAttributeIds(deletedIds, connection, tr);
                }

                // add/update other attributes
                foreach (var attribute in document.Attributes)
                    UpsertAttribute(attribute, "document", connection, tr);
            }
            tr.Commit();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            tr.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Delete the document with the specified identifier with all its
    /// related data.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    public void DeleteDocument(int id)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM document WHERE id=@id;";
        AddParameter(cmd, "@id", DbType.Int32, id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Sets the content of the document with the specified identifier.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="content">The content, or null.</param>
    public void SetDocumentContent(int id, string content)
    {
        using IDbConnection connection = GetConnection();
        connection.Open();

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE document SET content=@content WHERE id=@id;";
        AddParameter(cmd, "@id", DbType.Int32, id);
        AddParameter(cmd, "@content", DbType.String, content);
        cmd.ExecuteNonQuery();
    }
    #endregion

    #region Attributes        
    /// <summary>
    /// Upserts the specified attribute updating its ID if new.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    /// <param name="target">The target.</param>
    /// <param name="connection">The connection.</param>
    /// <param name="tr">An optional transaction.</param>
    /// <exception cref="ArgumentNullException">attribute or connection
    /// </exception>
    public abstract void UpsertAttribute(Core.Attribute attribute,
        string target, IDbConnection connection, IDbTransaction? tr = null);

    /// <summary>
    /// Adds the specified attribute.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    /// <param name="target">The target for this attribute, e.g. <c>document</c>.
    /// </param>
    /// <param name="unique">If set to <c>true</c>, replace any other
    /// attribute from the same document and with the same type with the
    /// new one.</param>
    /// <exception cref="ArgumentNullException">attribute</exception>
    public void AddAttribute(Core.Attribute attribute, string target,
        bool unique)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        using IDbConnection connection = GetConnection();
        connection.Open();
        using IDbTransaction tr = connection.BeginTransaction();
        try
        {
            if (unique)
            {
                // remove all the attrs with the same name
                // when the new one must be unique
                IDbCommand cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM document_attribute " +
                    "WHERE document_id=@document_id AND name=@name;";
                AddParameter(cmd, "@target_id", DbType.Int32, attribute.TargetId);
                AddParameter(cmd, "@name", DbType.String, attribute.Name);
                cmd.ExecuteNonQuery();
            }

            // upsert
            UpsertAttribute(attribute, target, connection, tr);

            tr.Commit();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            tr.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Gets the SQL code for the specified attribute filter, also adding
    /// parameters to the specified command.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="command">The command.</param>
    /// <param name="prefix">The prefix to prepend when the filter is not
    /// empty.</param>
    /// <param name="suffix">The suffix to prepend when the filter is not
    /// empty.</param>
    /// <returns>SQL code.</returns>
    protected static string GetAttributeFilterSql(AttributeFilter filter,
        IDbCommand command, string prefix = " WHERE ", string suffix = "")
    {
        StringBuilder sql = new();

        if (!string.IsNullOrEmpty(filter.Name))
        {
            AddParameter(command, "@name", DbType.String, filter.Name);
            sql.AppendLine("WHERE LOWER(name) LIKE CONCAT('%', LOWER(@name), '%')");
        }

        if (sql.Length > 0)
        {
            if (!string.IsNullOrEmpty(prefix)) sql.Insert(0, prefix);
            if (!string.IsNullOrEmpty(suffix)) sql.Append(suffix);
        }

        return sql.ToString();
    }

    /// <summary>
    /// Gets the names of the attributes matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>page of names, or all the names when page size is 0.</returns>
    /// <exception cref="ArgumentNullException">null filter</exception>
    public DataPage<string> GetAttributeNames(AttributeFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        using IDbConnection connection = GetConnection();
        connection.Open();

        IDbCommand cmd = connection.CreateCommand();
        string sqlFilter = GetAttributeFilterSql(filter, cmd);

        cmd.CommandText = $"SELECT COUNT(DISTINCT name) FROM {filter.Target}" +
            sqlFilter + ";";
        int total = GetTotal(cmd);
        if (total == 0)
        {
            return new DataPage<string>(filter.PageNumber,
                filter.PageSize, 0, Array.Empty<string>());
        }

        cmd.CommandText = $"SELECT DISTINCT name FROM {filter.Target}" +
            sqlFilter + "\n" +
            "ORDER BY name\n" +
            (filter.PageSize > 0
                ? GetPagingSql(filter.GetSkipCount(), filter.PageSize)
                : "")
                + ";";

        using IDataReader reader = cmd.ExecuteReader();
        List<string> names = [];
        while (reader.Read()) names.Add(reader.GetString(0));
        reader.Close();

        return new DataPage<string>(filter.PageNumber, filter.PageSize,
            total, names);
    }
    #endregion

    #region Profiles
    private static Profile ReadProfile(IDataReader reader)
    {
        return new Profile
        {
            Id = reader.GetString(0),
            Content = reader.GetString(1),
            Type = reader.IsDBNull(2)? null : reader.GetString(2),
            UserId = reader.IsDBNull(3) ? null : reader.GetString(3)
        };
    }

    /// <summary>
    /// Gets the content of the profile with the specified ID.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <param name="noContent">True to retrieve only the profile metadata,
    /// without its content.</param>
    /// <returns>The profile or null if not found.</returns>
    /// <exception cref="ArgumentNullException">id</exception>
    public IProfile? GetProfile(string id, bool noContent = false)
    {
        ArgumentNullException.ThrowIfNull(id);

        using IDbConnection connection = GetConnection();
        connection.Open();
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = (noContent
            ? "SELECT id, '' AS content, type, user_id"
            : "SELECT id, content, type, user_id") +
            " FROM profile WHERE id=@id;";
        AddParameter(cmd, "@id", DbType.String, id);
        using IDataReader reader = cmd.ExecuteReader();
        Profile? profile = reader.Read()? ReadProfile(reader) : null;
        reader.Close();
        return profile;
    }

    /// <summary>
    /// Gets the SQL code corresponding to the specified profile filter,
    /// also adding parameters to <paramref name="command"/>.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="command">The command.</param>
    /// <returns>The SQL code.</returns>
    protected static string GetProfileFilterSql(ProfileFilter filter,
        IDbCommand command)
    {
        StringBuilder sql = new();

        bool addedClause = false;
        if (!string.IsNullOrEmpty(filter.Prefix))
        {
            AddParameter(command, "@id", DbType.String, filter.Prefix);
            sql.AppendLine("id LIKE CONCAT(@id, '%')");
            addedClause = true;
        }

        if (!string.IsNullOrEmpty(filter.Id))
        {
            if (addedClause) sql.Append("\nAND\n");
            AddParameter(command, "@id", DbType.String, filter.Id);
            sql.AppendLine("id LIKE CONCAT('%', @id, '%')");
            addedClause = true;
        }

        if (!string.IsNullOrEmpty(filter.Type))
        {
            if (addedClause) sql.Append("\nAND\n");
            AddParameter(command, "@type", DbType.String, filter.Type);
            sql.AppendLine("type=@type");
            addedClause = true;
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            if (addedClause) sql.Append("\nAND\n");
            AddParameter(command, "@user_id", DbType.String, filter.UserId);
            sql.AppendLine("user_id=@user_id");
        }

        if (sql.Length > 0) sql.Insert(0, "WHERE ");

        return sql.ToString();
    }

    /// <summary>
    /// Gets the specified page of profiles.
    /// </summary>
    /// <param name="filter">The profiles filter. Set page size to 0
    /// to retrieve all the matching profiles at once.</param>
    /// <param name="noContent">True to retrieve only the profile ID,
    /// without its content.</param>
    /// <returns>The page.</returns>
    public DataPage<IProfile> GetProfiles(ProfileFilter filter,
        bool noContent = false)
    {
        ArgumentNullException.ThrowIfNull(filter);

        using IDbConnection connection = GetConnection();
        connection.Open();

        IDbCommand cmd = connection.CreateCommand();
        string sqlFilter = GetProfileFilterSql(filter, cmd);

        cmd.CommandText = "SELECT COUNT(id) FROM profile " +
            sqlFilter + ";";
        int total = GetTotal(cmd);
        if (total == 0)
        {
            return new DataPage<IProfile>(filter.PageNumber,
                filter.PageSize, 0, Array.Empty<IProfile>());
        }

        cmd.CommandText = (noContent
            ? "SELECT id, type, user_id"
            : "SELECT id, type, user_id, content")
            + " FROM profile " + sqlFilter + "\nORDER BY id" +
            (filter.PageSize == 0
             ? ""
             : "\n" + GetPagingSql(filter.GetSkipCount(), filter.PageSize)
            ) + ";";

        using IDataReader reader = cmd.ExecuteReader();
        List<IProfile> profiles = [];
        while (reader.Read())
        {
            profiles.Add(new Profile
            {
                Id = reader.GetString(0),
                Type = reader.IsDBNull(1)? null : reader.GetString(1),
                UserId = reader.IsDBNull(2) ? null : reader.GetString(2),
                Content = noContent? null : reader.GetString(3)
            });
        }
        reader.Close();

        return new DataPage<IProfile>(filter.PageNumber, filter.PageSize,
            total, profiles);
    }

    /// <summary>
    /// Upserts the specified profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="connection">The connection.</param>
    public abstract void UpsertProfile(IProfile profile,
        IDbConnection connection);

    /// <summary>
    /// Adds or updates the specified profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <exception cref="ArgumentNullException">profile</exception>
    public void AddProfile(IProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        using IDbConnection connection = GetConnection();
        connection.Open();

        UpsertProfile(profile, connection);
    }

    /// <summary>
    /// Delete the profile with the specified ID.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <exception cref="ArgumentNullException">id</exception>
    public void DeleteProfile(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        using IDbConnection connection = GetConnection();
        connection.Open();
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM profile WHERE id=@id;";
        AddParameter(cmd, "@id", DbType.String, id);
        cmd.ExecuteNonQuery();
    }
    #endregion
}
