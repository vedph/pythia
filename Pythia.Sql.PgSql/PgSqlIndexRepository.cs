using Corpus.Core;
using Corpus.Sql.PgSql;
using Npgsql;
using Pythia.Core;
using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pythia.Sql.PgSql;

/// <summary>
/// PostgreSQL-based Pythia index repository.
/// </summary>
/// <seealso cref="SqlIndexRepository" />
public sealed class PgSqlIndexRepository : SqlIndexRepository
{
    private readonly PgSqlCorpusRepository _corpus;

    /// <summary>
    /// Initializes a new instance of the <see cref="PgSqlIndexRepository"/>
    /// class.
    /// </summary>
    public PgSqlIndexRepository() :
        base(new PgSqlHelper(), new PgSqlCorpusRepository())
    {
        _corpus = (PgSqlCorpusRepository)CorpusRepository;
    }

    private static string LoadResourceText(string name)
    {
        using StreamReader reader = new(
            Assembly.GetExecutingAssembly().GetManifestResourceStream(
                $"Pythia.Sql.PgSql.Assets.{name}")!, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Gets the DDL SQL code for the database schema. This is the sum
    /// of the corpus schema plus the index schema.
    /// </summary>
    /// <returns>SQL code.</returns>
    public override string GetSchema()
    {
        StringBuilder sql = new();

        // corpus
        sql.AppendLine(CorpusRepository.GetSchema());

        // pythia
        sql.AppendLine(LoadResourceText("Schema.pgsql"));

        // functions
        sql.AppendLine(LoadResourceText("Functions.pgsql"));

        return sql.ToString();
    }

    /// <summary>
    /// Gets a new connection object.
    /// </summary>
    /// <returns>The connection.</returns>
    public override IDbConnection GetConnection()
        => new NpgsqlConnection(ConnectionString);

    /// <summary>
    /// Builds the paging expression with the specified values.
    /// </summary>
    /// <param name="offset">The offset count.</param>
    /// <param name="limit">The limit count.</param>
    /// <returns>SQL code.</returns>
    protected override string GetPagingSql(int offset, int limit)
        => $"OFFSET {offset} LIMIT {limit}";

    /// <summary>
    /// Upserts the specified structure.
    /// </summary>
    /// <param name="span">The structure.</param>
    /// <param name="connection">The connection.</param>
    /// <exception cref="ArgumentNullException">structure or connection</exception>
    protected override void UpsertSpan(TextSpan span, IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(span);
        ArgumentNullException.ThrowIfNull(connection);

        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO span" +
            "(document_id, type, p1, p2, index, length, language, pos, value, text)\n" +
            "VALUES(@document_id, @type, @p1, @p2, @index, @length, @language, " +
            "@pos, @value, @text)\n" +
            "ON CONFLICT(id) DO UPDATE\n" +
            "SET document_id=@document_id, type=@type, p1=@p1, p2=@p2, " +
            "index=@index, length=@length, language=@language, pos=@pos," +
            "value=@value, text=@text\n" +
            "RETURNING id;";
        AddParameter(cmd, "@document_id", DbType.Int32, span.DocumentId);
        AddParameter(cmd, "@type", DbType.String, span.Type);
        AddParameter(cmd, "@p1", DbType.Int32, span.P1);
        AddParameter(cmd, "@p2", DbType.Int32, span.P2);
        AddParameter(cmd, "@index", DbType.Int32, span.Index);
        AddParameter(cmd, "@length", DbType.Int32, span.Length);
        AddParameter(cmd, "@language", DbType.String, span.Language);
        AddParameter(cmd, "@pos", DbType.Int32, span.Pos);
        AddParameter(cmd, "@value", DbType.String, span.Value);
        AddParameter(cmd, "@text", DbType.String, span.Text);

        int n = (cmd.ExecuteScalar() as int?) ?? 0;
        if (span.Id == 0) span.Id = n;
        if (span.Attributes?.Count > 0)
        {
            foreach (Corpus.Core.Attribute attribute in span.Attributes)
                attribute.TargetId = n;
        }
    }

    /// <summary>
    /// Upserts the profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="connection">The connection.</param>
    public override void UpsertProfile(IProfile profile, IDbConnection connection)
    {
        _corpus.UpsertProfile(profile, connection);
    }

    /// <summary>
    /// Upserts the attribute.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    /// <param name="target">The target.</param>
    /// <param name="connection">The connection.</param>
    /// <param name="tr">The optional transaction.</param>
    public override void UpsertAttribute(Corpus.Core.Attribute attribute,
        string target, IDbConnection connection, IDbTransaction? tr = null)
        => _corpus.UpsertAttribute(attribute, target, connection, tr);

    /// <summary>
    /// Upserts the document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="hasContent">if set to <c>true</c> [has content].</param>
    /// <param name="connection">The connection.</param>
    /// <param name="tr">The optional transaction.</param>
    public override void UpsertDocument(IDocument document, bool hasContent,
        IDbConnection connection, IDbTransaction? tr = null)
        => _corpus.UpsertDocument(document, hasContent, connection, tr);

    /// <summary>
    /// Upserts the corpus.
    /// </summary>
    /// <param name="corpus">The corpus.</param>
    /// <param name="connection">The connection.</param>
    /// <param name="tr">The optional transaction.</param>
    public override void UpsertCorpus(ICorpus corpus, IDbConnection connection,
        IDbTransaction? tr = null)
        => _corpus.UpsertCorpus(corpus, connection, tr);
}
