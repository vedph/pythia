using Fusi.DbManager.PgSql;
using Npgsql;
using Pythia.Core;
using Corpus.Sql;
using Fusi.Tools.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Pythia.Sql.PgSql.Test;

/// <summary>
/// Tests for building the words index. These use a dedicated database
/// because building the index writes into it, while the other tests
/// use their database only for reading.
/// </summary>
[Collection(nameof(NonParallelResourceCollection))]
public sealed class WordIndexTest
{
    private const string CST = "User ID=postgres;Password=postgres;" +
        "Host=localhost;Port=5432;Database={0};Include Error Detail=True";
    private const string DB_NAME = "pythia-test-words";
    internal static readonly string CS = string.Format(CST, DB_NAME);

    internal static PgSqlIndexRepository GetRepository()
    {
        PgSqlIndexRepository repository = new();
        repository.Configure(new SqlRepositoryOptions
        {
            ConnectionString = CS
        });
        return repository;
    }

    internal static void Execute(IDbConnection connection, string sql)
    {
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Ensures the database exists and seeds it with a single document
    /// with these tokens (all lowercase, no language):
    /// <list type="bullet">
    /// <item>1: via (NOUN, lemma via), no attributes;</item>
    /// <item>2: yomaris (ADJ, lemma yomaris), with attribute address;</item>
    /// <item>3: via (NOUN, lemma via), with attribute address;</item>
    /// <item>4: atto (NOUN, lemma atto), with attribute deprel only;</item>
    /// <item>5: rosciano (PROPN, lemma rosciano), no attributes.</item>
    /// </list>
    /// Attributes are inserted so that their IDs do not match their span
    /// IDs, to catch any wrong join between span and span_attribute.
    /// </summary>
    internal static void ResetDatabase()
    {
        PgSqlDbManager manager = new(CST);
        if (!manager.Exists(DB_NAME))
        {
            manager.CreateDatabase(DB_NAME,
                new PgSqlIndexRepository().GetSchema(), null);
        }

        using IDbConnection connection = new NpgsqlConnection(CS);
        connection.Open();
        Execute(connection,
            "TRUNCATE word_count, lemma_count, word, lemma, span_attribute, " +
            "span, document_attribute, document, profile " +
            "RESTART IDENTITY CASCADE;");

        Execute(connection,
            "INSERT INTO profile(id, content) VALUES('p', '{}');\n" +
            "INSERT INTO document(author, title, source, profile_id, " +
            "date_value, sort_key, last_modified) " +
            "VALUES('a', 't', 's', 'p', 1998.5, 'a', NOW());\n" +
            "INSERT INTO span(document_id, type, p1, p2, index, length, " +
            "pos, lemma, value, text) VALUES\n" +
            "(1, 'tok', 1, 1, 0, 3, 'NOUN', 'via', 'via', 'Via'),\n" +
            "(1, 'tok', 2, 2, 4, 7, 'ADJ', 'yomaris', 'yomaris', 'yomaris'),\n" +
            "(1, 'tok', 3, 3, 12, 3, 'NOUN', 'via', 'via', 'via'),\n" +
            "(1, 'tok', 4, 4, 16, 4, 'NOUN', 'atto', 'atto', 'atto'),\n" +
            "(1, 'tok', 5, 5, 21, 8, 'PROPN', 'rosciano', 'rosciano', " +
            "'Rosciano'),\n" +
            "(1, 'snt', 1, 5, 0, 29, NULL, NULL, '29', " +
            "'Via yomaris via atto Rosciano');\n" +
            // attribute IDs: 1=deprel@4, 2=len@1, 3=address@2, 4=address@3;
            // note that attribute 1 has the same ID of span 1 (via), and
            // attribute 4 has the same ID of span 4 (atto)
            "INSERT INTO span_attribute(span_id, name, value, type) VALUES\n" +
            "(4, 'deprel', 'obj', 0),\n" +
            "(1, 'len', '3', 1),\n" +
            "(2, 'address', '1', 0),\n" +
            "(3, 'address', '1', 0);");
    }

    private static int GetSpanWordCount(int spanId)
    {
        using IDbConnection connection = new NpgsqlConnection(CS);
        connection.Open();
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM span WHERE id={spanId} " +
            "AND word_id IS NOT NULL;";
        return (int)(long)cmd.ExecuteScalar()!;
    }

    private static Task BuildAsync(PgSqlIndexRepository repository,
        HashSet<string> excludedSpanAttrNames,
        HashSet<string> excludedPosValues,
        IDictionary<string, int>? binCounts = null)
    {
        return repository.BuildWordIndexAsync(null,
            binCounts ?? new Dictionary<string, int> { ["date_value"] = 3 },
            [],
            excludedSpanAttrNames,
            excludedPosValues,
            CancellationToken.None);
    }

    private static List<(string Name, string Value, int Count)>
        GetWordCounts(string word)
    {
        using IDbConnection connection = new NpgsqlConnection(CS);
        connection.Open();
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT wc.doc_attr_name, wc.doc_attr_value, " +
            "wc.count FROM word_count wc INNER JOIN word w " +
            $"ON wc.word_id=w.id WHERE w.value='{word}' " +
            "ORDER BY wc.doc_attr_name;";
        List<(string, string, int)> counts = [];
        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            counts.Add((reader.GetString(0), reader.GetString(1),
                reader.GetInt32(2)));
        }
        return counts;
    }

    private static long GetScalar(string sql)
    {
        using IDbConnection connection = new NpgsqlConnection(CS);
        connection.Open();
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private static string GetSpanIds()
    {
        using IDbConnection connection = new NpgsqlConnection(CS);
        connection.Open();
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, word_id, lemma_id FROM span ORDER BY id;";
        List<string> rows = [];
        using IDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            rows.Add($"{reader.GetInt32(0)}:" +
                $"{(reader.IsDBNull(1) ? "-" : reader.GetInt32(1))}:" +
                $"{(reader.IsDBNull(2) ? "-" : reader.GetInt32(2))}");
        }
        return string.Join(" ", rows);
    }

    private static void AssertSpanIdsConsistent()
    {
        // each span with a word has its word's lemma, and no span without
        // a word has a lemma
        Assert.Equal(0, GetScalar("SELECT COUNT(*) FROM span s " +
            "INNER JOIN word w ON w.id=s.word_id " +
            "WHERE s.lemma_id IS DISTINCT FROM w.lemma_id;"));
        Assert.Equal(0, GetScalar("SELECT COUNT(*) FROM span " +
            "WHERE word_id IS NULL AND lemma_id IS NOT NULL;"));
        // span FKs are valid
        Assert.Equal(2, GetScalar("SELECT COUNT(*) FROM pg_constraint " +
            "WHERE conname IN('span_word_fk','span_lemma_fk') " +
            "AND convalidated;"));
    }

    [Fact]
    public async Task BuildWordIndex_Rebuilt_SameIds()
    {
        ResetDatabase();
        PgSqlIndexRepository repository = GetRepository();

        await BuildAsync(repository, ["address"], []);
        string ids = GetSpanIds();
        await BuildAsync(repository, ["address"], []);

        Assert.Equal(ids, GetSpanIds());
        AssertSpanIdsConsistent();
    }

    [Fact]
    public async Task BuildWordIndex_RebuiltWithExclusions_ClearsExcluded()
    {
        ResetDatabase();
        PgSqlIndexRepository repository = GetRepository();

        // all the tokens get a word
        await BuildAsync(repository, [], []);
        Assert.Equal(5, GetScalar(
            "SELECT COUNT(*) FROM span WHERE word_id IS NOT NULL;"));
        Assert.Equal(5, GetScalar(
            "SELECT COUNT(*) FROM span WHERE lemma_id IS NOT NULL;"));

        // spans 2-3 (address) and 5 (PROPN) are excluded now
        await BuildAsync(repository, ["address"], ["PROPN"]);
        Assert.Equal(1, GetSpanWordCount(1));
        Assert.Equal(0, GetSpanWordCount(2));
        Assert.Equal(0, GetSpanWordCount(3));
        Assert.Equal(1, GetSpanWordCount(4));
        Assert.Equal(0, GetSpanWordCount(5));
        Assert.Equal(2, GetScalar(
            "SELECT COUNT(*) FROM span WHERE lemma_id IS NOT NULL;"));
        AssertSpanIdsConsistent();

        // back to no exclusions
        await BuildAsync(repository, [], []);
        Assert.Equal(5, GetScalar(
            "SELECT COUNT(*) FROM span WHERE word_id IS NOT NULL;"));
        AssertSpanIdsConsistent();
    }

    [Fact]
    public async Task BuildWordIndex_UnbinnedDateValue_CountedAsCategory()
    {
        ResetDatabase();
        PgSqlIndexRepository repository = GetRepository();

        await BuildAsync(repository, ["address"], [],
            new Dictionary<string, int>());

        List<(string Name, string Value, int Count)> counts =
            GetWordCounts("via");
        Assert.Contains(("author", "a", 1), counts);
        Assert.Contains(("date_value", "1998.5", 1), counts);
    }

    [Fact]
    public async Task BuildWordIndex_BinnedDateValue_CountedAsRange()
    {
        ResetDatabase();
        PgSqlIndexRepository repository = GetRepository();

        await BuildAsync(repository, ["address"], []);

        List<(string Name, string Value, int Count)> counts =
            GetWordCounts("via");
        Assert.Contains(counts, c => c.Name == "date_value" &&
            c.Value.Contains(':') && c.Count == 1);
    }

    [Fact]
    public async Task BuildWordIndex_NoExclusions_AllWords()
    {
        ResetDatabase();
        PgSqlIndexRepository repository = GetRepository();

        await BuildAsync(repository, [], []);

        DataPage<Word> page = repository.GetWords(
            new WordFilter { PageSize = 100 });
        Assert.Equal(4, page.Total);
        Word via = page.Items.First(w => w.Value == "via");
        Assert.Equal(2, via.Count);
        Assert.Contains(page.Items, w => w.Value == "yomaris");
        Assert.Contains(page.Items, w => w.Value == "rosciano");
    }

    [Fact]
    public async Task BuildWordIndex_ExcludedSpanAttr_ExcludesWordsAndCounts()
    {
        ResetDatabase();
        PgSqlIndexRepository repository = GetRepository();

        await BuildAsync(repository, ["address"], []);

        // words: yomaris excluded; via counted only once
        DataPage<Word> words = repository.GetWords(
            new WordFilter { PageSize = 100 });
        Assert.Equal(3, words.Total);
        Assert.DoesNotContain(words.Items, w => w.Value == "yomaris");
        Word via = words.Items.First(w => w.Value == "via");
        Assert.Equal(1, via.Count);
        Assert.Contains(words.Items, w => w.Value == "atto");
        Assert.Contains(words.Items, w => w.Value == "rosciano");

        // lemmata derive from words
        DataPage<Lemma> lemmata = repository.GetLemmata(
            new LemmaFilter { PageSize = 100 });
        Assert.DoesNotContain(lemmata.Items, l => l.Value == "yomaris");
        Assert.Equal(1, lemmata.Items.First(l => l.Value == "via").Count);

        // excluded spans have no word ID
        Assert.Equal(1, GetSpanWordCount(1));
        Assert.Equal(0, GetSpanWordCount(2));
        Assert.Equal(0, GetSpanWordCount(3));
        Assert.Equal(1, GetSpanWordCount(4));
    }

    [Fact]
    public async Task BuildWordIndex_ExcludedSpanAttrAndPos_ExcludesBoth()
    {
        ResetDatabase();
        PgSqlIndexRepository repository = GetRepository();

        await BuildAsync(repository, ["address", "pn-m"], ["PROPN", "NUM"]);

        DataPage<Word> words = repository.GetWords(
            new WordFilter { PageSize = 100 });
        Assert.Equal(2, words.Total);
        Assert.Equal(1, words.Items.First(w => w.Value == "via").Count);
        Assert.Contains(words.Items, w => w.Value == "atto");
        Assert.Equal(0, GetSpanWordCount(5));
    }
}
