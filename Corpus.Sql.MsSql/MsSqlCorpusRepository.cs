﻿using Corpus.Core;
using Fusi.Tools.Configuration;
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text;

namespace Corpus.Sql.MsSql;

/// <summary>
/// SQLServer-based corpus repository.
/// <para>Tag: <c>corpus-repository.sql.ms</c>.</para>
/// </summary>
/// <seealso cref="SqlCorpusRepository" />
/// <seealso cref="ICorpusRepository" />
[Tag("corpus-repository.sql.ms")]
public class MsSqlCorpusRepository : SqlCorpusRepository, ICorpusRepository
{
    /// <summary>
    /// Gets a new connection object.
    /// </summary>
    /// <returns>The connection.</returns>
    public override IDbConnection GetConnection()
        => new SqlConnection(ConnectionString);

    /// <summary>
    /// Gets the DDL SQL code for the database schema.
    /// </summary>
    /// <returns>SQL code.</returns>
    public virtual string GetSchema()
    {
        using StreamReader reader = new(
            Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "Corpus.Sql.MsSql.Assets.Schema.mssql")!, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Upserts <paramref name="corpus"/>.
    /// </summary>
    /// <param name="corpus">The corpus.</param>
    /// <param name="connection">The connection.</param>
    /// <param name="tr">An optional transaction.</param>
    /// <exception cref="ArgumentNullException">corpus or connection</exception>
    public override void UpsertCorpus(ICorpus corpus,
        IDbConnection connection, IDbTransaction? tr = null)
    {
        ArgumentNullException.ThrowIfNull(corpus);
        ArgumentNullException.ThrowIfNull(connection);

        // specialized upsert (better performance)
        // https://stackoverflow.com/questions/64061101/microsoft-sql-server-best-way-to-update-if-exists-or-insert

        IDbCommand cmd = connection.CreateCommand();
        cmd.Transaction = tr;
        cmd.CommandText = "UPDATE corpus SET title=@title, " +
            "description=@description, user_id=@user_id WHERE id=@id;\n" +
            "IF @@ROWCOUNT=0\n" +
            "INSERT INTO corpus(id, title, description, user_id)\n" +
            "VALUES(@id, @title, @description, @user_id);";

        AddParameter(cmd, "@id", DbType.String, corpus.Id);
        AddParameter(cmd, "@title", DbType.String, corpus.Title);
        AddParameter(cmd, "@description", DbType.String, corpus.Description);
        AddParameter(cmd, "@user_id", DbType.String,
            corpus.UserId ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Upserts <paramref name="document" />.
    /// </summary>
    /// <param name="document">The document. Its ID is updated when it
    /// is a new inserted record.</param>
    /// <param name="hasContent">if set to <c>true</c>, also modify the
    /// document's content; else do not update/insert it.</param>
    /// <param name="connection">The connection.</param>
    /// <param name="tr">An optional transaction.</param>
    public override void UpsertDocument(IDocument document, bool hasContent,
        IDbConnection connection, IDbTransaction? tr = null)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.Transaction = tr;

        // upsert if not new
        if (document.Id != 0)
        {
            cmd.CommandText = "UPDATE document SET author=@author, " +
                "title=@title, source=@source, profile_id=@profile_id, " +
                "date_value=@date_value, sort_key=@sort_key, " +
                "last_modified=@last_modified, user_id=@user_id" +
                (hasContent? ", content = @content\n" : "\n") +
                "WHERE id=@id;\n" +
                "IF @@ROWCOUNT=0\n" +
                "BEGIN\n" +
                "INSERT INTO document(author, title, source, profile_id, " +
                "date_value, sort_key, last_modified, user_id" +
                (hasContent ? ", content" : "") +
                ")\nVALUES(@author, @title, @source, @profile_id, @date_value, " +
                "@sort_key, @last_modified, @user_id" +
                (hasContent ? ", @content" : "") + ");\n" +
                "SELECT CAST(SCOPE_IDENTITY() AS INT) AS id;\n" +
                "END\n" +
                "ELSE\n" +
                "BEGIN\n" +
                "SELECT @id;\n" +
                "END\n";

            AddParameter(cmd, "@id", DbType.Int32, document.Id);
        }
        // else just insert
        else
        {
            cmd.CommandText =
                "INSERT INTO document(author, title, source, profile_id, " +
                "date_value, sort_key, last_modified, user_id" +
                (hasContent ? ", content" : "") +
                ")\nVALUES(@author, @title, @source, @profile_id, @date_value, " +
                "@sort_key, @last_modified, @user_id" +
                (hasContent ? ", @content" : "") + ");" +
                "SELECT CAST(SCOPE_IDENTITY() AS INT) AS id;";
        }

        AddParameter(cmd, "@author", DbType.String, document.Author);
        AddParameter(cmd, "@title", DbType.String, document.Title);
        AddParameter(cmd, "@source", DbType.String, document.Source ?? "");
        AddParameter(cmd, "@profile_id", DbType.String, document.ProfileId);
        AddParameter(cmd, "@date_value", DbType.Double, document.DateValue);
        AddParameter(cmd, "@sort_key", DbType.String, document.SortKey);
        AddParameter(cmd, "@last_modified", DbType.DateTime, document.LastModified);
        AddParameter(cmd, "@user_id", DbType.String,
            document.UserId ?? (object)DBNull.Value);
        if (hasContent)
        {
            AddParameter(cmd, "@content", DbType.String,
                document.Content ?? (object)DBNull.Value);
        }

        int n = (int)cmd.ExecuteScalar()!;
        if (document.Id == 0) document.Id = n;
        if (document?.Attributes?.Count > 0)
        {
            foreach (Core.Attribute attribute in document.Attributes)
                attribute.TargetId = document.Id;
        }
    }

    /// <summary>
    /// Upserts the specified profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="connection">The connection.</param>
    public override void UpsertProfile(IProfile profile,
        IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(connection);

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText =
            "UPDATE profile SET content=@content, type=@type, " +
            "user_id=@user_id WHERE id=@id;\n" +
            "IF @@ROWCOUNT=0\n" +
            "INSERT INTO profile(id, content, type, user_id)\n" +
            "VALUES(@id, @content, @type, @user_id);";
        AddParameter(cmd, "@id", DbType.String, profile.Id);
        AddParameter(cmd, "@content", DbType.String, profile.Content);
        AddParameter(cmd, "@type", DbType.String,
            profile.Type ?? (object)DBNull.Value);
        AddParameter(cmd, "@user_id", DbType.String,
            profile.UserId ?? (object)DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Upserts the specified attribute updating its ID if new.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    /// <param name="target">The target.</param>
    /// <param name="connection">The connection.</param>
    /// <param name="tr">An optional transaction.</param>
    /// <exception cref="ArgumentNullException">attribute or target or
    /// connection</exception>
    public override void UpsertAttribute(Core.Attribute attribute,
        string target, IDbConnection connection, IDbTransaction? tr = null)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(connection);

        IDbCommand cmd = connection.CreateCommand();
        cmd.Transaction = tr;

        string ta = target + "_attribute";
        string ti = target + "_id";

        // upsert if not new
        if (attribute.Id != 0)
        {
            cmd.CommandText = $"UPDATE {ta} SET {ti}=@target_id, " +
                "name=@name, value=@value, type=@type\n" +
                "WHERE id=@id;\n" +
                "IF @@ROWCOUNT=0\n" +
                "BEGIN\n" +
                $"INSERT INTO {ta}({ti}, name, value, type) " +
                "VALUES(@target_id, @name, @value, @type);\n" +
                "SELECT CAST(SCOPE_IDENTITY() AS INT) AS id;\n" +
                "END\n" +
                "ELSE\n" +
                "BEGIN\n" +
                "SELECT @id;\n" +
                "END\n";

            AddParameter(cmd, "@id", DbType.Int32, attribute.Id);
        }
        // else just insert
        else
        {
            cmd.CommandText = $"INSERT INTO {ta}({ti}, name, value, type) " +
                "VALUES(@target_id, @name, @value, @type);\n" +
                "SELECT CAST(SCOPE_IDENTITY() AS INT) AS id;\n";
        }

        AddParameter(cmd, "@target_id", DbType.Int32, attribute.TargetId);
        AddParameter(cmd, "@name", DbType.String, attribute.Name);
        AddParameter(cmd, "@value", DbType.String, attribute.Value);
        AddParameter(cmd, "@type", DbType.Int32, (int)attribute.Type);

        int n = (int)cmd.ExecuteScalar()!;
        if (attribute.Id == 0) attribute.Id = n;
    }

    /// <summary>
    /// Builds the paging expression with the specified values.
    /// </summary>
    /// <param name="offset">The offset count.</param>
    /// <param name="limit">The limit count.</param>
    /// <returns>SQL code.</returns>
    protected override string GetPagingSql(int offset, int limit)
    {
        return $"OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";
    }
}
