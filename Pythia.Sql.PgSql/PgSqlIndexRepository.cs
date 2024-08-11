using Corpus.Core;
using Corpus.Sql.PgSql;
using Npgsql;
using NpgsqlTypes;
using Pythia.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
            "(document_id, type, p1, p2, index, length, language, pos, lemma, " +
            "value, text)\n" +
            "VALUES(@document_id, @type, @p1, @p2, @index, @length, @language, " +
            "@pos, @lemma, @value, @text)\n" +
            "ON CONFLICT(id) DO UPDATE\n" +
            "SET document_id=@document_id, type=@type, p1=@p1, p2=@p2, " +
            "index=@index, length=@length, language=@language, pos=@pos," +
            "lemma=@lemma, value=@value, text=@text\n" +
            "RETURNING id;";
        AddParameter(cmd, "@document_id", DbType.Int32, span.DocumentId);
        AddParameter(cmd, "@type", DbType.String, span.Type);
        AddParameter(cmd, "@p1", DbType.Int32, span.P1);
        AddParameter(cmd, "@p2", DbType.Int32, span.P2);
        AddParameter(cmd, "@index", DbType.Int32, span.Index);
        AddParameter(cmd, "@length", DbType.Int32, span.Length);
        AddParameter(cmd, "@language", DbType.String, span.Language
            ?? (object)DBNull.Value);
        AddParameter(cmd, "@pos", DbType.String, span.Pos
            ?? (object)DBNull.Value);
        AddParameter(cmd, "@lemma", DbType.String, span.Lemma
            ?? (object)DBNull.Value);
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

    /// <summary>
    /// Batches the insert words.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="words">The words.</param>
    protected override async Task BatchInsertWords(IDbConnection connection,
        List<Word> words)
    {
        NpgsqlConnection cnn = (NpgsqlConnection)connection;

        const int batchSize = 100;
        for (int i = 0; i < words.Count; i += batchSize)
        {
            List<Word> batch = words.Skip(i).Take(batchSize).ToList();

            await using NpgsqlBinaryImporter importer = await
                cnn.BeginBinaryImportAsync(
                "COPY word(language, value, reversed_value, pos, lemma, count) " +
                "FROM STDIN (FORMAT BINARY)");
            foreach (Word word in batch)
            {
                await importer.StartRowAsync();

                if (string.IsNullOrEmpty(word.Language))
                {
                    await importer.WriteNullAsync();
                }
                else
                {
                    await importer.WriteAsync(
                        GetTruncatedString(word.Language, LANGUAGE_MAX),
                            NpgsqlDbType.Varchar);
                }

                await importer.WriteAsync(
                    GetTruncatedString(word.Value, VALUE_MAX),
                    NpgsqlDbType.Varchar);
                await importer.WriteAsync(
                    GetTruncatedString(word.ReversedValue, VALUE_MAX),
                    NpgsqlDbType.Varchar);

                if (string.IsNullOrEmpty(word.Pos))
                {
                    await importer.WriteNullAsync();
                }
                else
                {
                    await importer.WriteAsync(
                        GetTruncatedString(word.Pos, POS_MAX),
                            NpgsqlDbType.Varchar);
                }

                if (string.IsNullOrEmpty(word.Lemma))
                {
                    await importer.WriteNullAsync();
                }
                else
                {
                    await importer.WriteAsync(
                        GetTruncatedString(word.Lemma, LEMMA_MAX),
                            NpgsqlDbType.Varchar);
                }

                await importer.WriteAsync(word.Count, NpgsqlDbType.Integer);
            }
            await importer.CompleteAsync();
        }
    }

    /// <summary>
    /// Batches the insert lemmata.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="lemmata">The lemmata.</param>
    protected override async Task BatchInsertLemmata(IDbConnection connection,
        List<Lemma> lemmata)
    {
        NpgsqlConnection cnn = (NpgsqlConnection)connection;
        const int batchSize = 100;

        for (int i = 0; i < lemmata.Count; i += batchSize)
        {
            List<Lemma> batch = lemmata.Skip(i).Take(batchSize).ToList();

            await using var importer = await cnn.BeginBinaryImportAsync(
                "COPY lemma(language, value, reversed_value, count) " +
                "FROM STDIN (FORMAT BINARY)");
            foreach (Lemma lemma in batch)
            {
                await importer.StartRowAsync();

                if (string.IsNullOrEmpty(lemma.Language))
                {
                    await importer.WriteNullAsync();
                }
                else
                {
                    await importer.WriteAsync(GetTruncatedString(
                        lemma.Language, LANGUAGE_MAX), NpgsqlDbType.Varchar);
                }

                await importer.WriteAsync(
                    GetTruncatedString(lemma.Value, VALUE_MAX),
                    NpgsqlDbType.Varchar);
                await importer.WriteAsync(
                    GetTruncatedString(lemma.ReversedValue, VALUE_MAX),
                    NpgsqlDbType.Varchar);
                await importer.WriteAsync(lemma.Count, NpgsqlDbType.Integer);
            }

            await importer.CompleteAsync();
        }
    }

    /// <summary>
    /// Batches the insert word counts.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="counts">The counts.</param>
    protected override async Task BatchInsertWordCounts(IDbConnection connection,
        List<WordCount> counts)
    {
        NpgsqlConnection cnn = (NpgsqlConnection)connection;
        const int batchSize = 1000;

        for (int i = 0; i < counts.Count; i += batchSize)
        {
            List<WordCount> batch = counts.Skip(i).Take(batchSize).ToList();

            await using var importer = await cnn.BeginBinaryImportAsync(
                "COPY word_count(word_id, lemma_id, doc_attr_name, " +
                "doc_attr_value, count) FROM STDIN (FORMAT BINARY)");
            foreach (WordCount count in batch)
            {
                await importer.StartRowAsync();

                await importer.WriteAsync(count.WordId, NpgsqlDbType.Integer);
                await importer.WriteAsync(count.LemmaId, NpgsqlDbType.Integer);
                await importer.WriteAsync(
                    GetTruncatedString(count.Pair.Name, ATTR_NAME_MAX),
                    NpgsqlDbType.Varchar);

                string docAttrValue = count.Pair.Value
                    ?? $"{count.Pair.MinValue:F2}:{count.Pair.MaxValue:F2}";
                await importer.WriteAsync(docAttrValue, NpgsqlDbType.Varchar);

                await importer.WriteAsync(count.Value, NpgsqlDbType.Integer);
            }

            await importer.CompleteAsync();
        }
    }
}
