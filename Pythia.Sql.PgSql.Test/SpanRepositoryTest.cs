using Npgsql;
using Pythia.Core;
using Pythia.Core.Query;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Pythia.Sql.PgSql.Test;

/// <summary>
/// Tests for span-related and other writing methods of the index repository.
/// These use the same writable database of <see cref="WordIndexTest"/>,
/// seeded by <see cref="WordIndexTest.ResetDatabase"/>.
/// </summary>
[Collection(nameof(NonParallelResourceCollection))]
public sealed class SpanRepositoryTest
{
    private static void Execute(string sql)
    {
        using IDbConnection connection = new NpgsqlConnection(WordIndexTest.CS);
        connection.Open();
        WordIndexTest.Execute(connection, sql);
    }

    private static long GetScalar(string sql)
    {
        using IDbConnection connection = new NpgsqlConnection(WordIndexTest.CS);
        connection.Open();
        IDbCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        return (long)cmd.ExecuteScalar()!;
    }

    [Fact]
    public void EnumerateSpans_TwoAttributes_MatchesSpansHavingBoth()
    {
        WordIndexTest.ResetDatabase();
        // span 2 has address=1 and len=7; span 3 has only address=1
        Execute("INSERT INTO span_attribute(span_id, name, value, type) " +
            "VALUES(2, 'len', '7', 1);");
        PgSqlIndexRepository repository = WordIndexTest.GetRepository();

        List<TextSpan> spans = [.. repository.EnumerateSpans(new TextSpanFilter
        {
            Attributes = new Dictionary<string, string>
            {
                ["address"] = "1",
                ["len"] = "7"
            }
        })];

        Assert.Single(spans);
        Assert.Equal(2, spans[0].Id);
    }

    [Fact]
    public void EnumerateSpans_OneAttribute_MatchesAllSpansHavingIt()
    {
        WordIndexTest.ResetDatabase();
        PgSqlIndexRepository repository = WordIndexTest.GetRepository();

        List<TextSpan> spans = [.. repository.EnumerateSpans(new TextSpanFilter
        {
            Attributes = new Dictionary<string, string> { ["address"] = "1" }
        })];

        Assert.Equal([2, 3], spans.Select(s => s.Id));
    }

    [Fact]
    public void AddSpanAttributes_Range_AddsToTokensOnly()
    {
        WordIndexTest.ResetDatabase();
        PgSqlIndexRepository repository = WordIndexTest.GetRepository();

        // the range 1-5 includes all the 5 tokens and the sentence (span 6)
        repository.AddSpanAttributes(1, 1, 5, "x", "y",
            Corpus.Core.AttributeType.Text);

        Assert.Equal(5, GetScalar(
            "SELECT COUNT(*) FROM span_attribute WHERE name='x';"));
        Assert.Equal(0, GetScalar(
            "SELECT COUNT(*) FROM span_attribute WHERE name='x' AND span_id=6;"));
    }

    [Fact]
    public void AddSpanAttributes_PrivilegedPos_UpdatesTokensOnly()
    {
        WordIndexTest.ResetDatabase();
        PgSqlIndexRepository repository = WordIndexTest.GetRepository();

        repository.AddSpanAttributes(1, 1, 5, "pos", "X",
            Corpus.Core.AttributeType.Text);

        Assert.Equal(5, GetScalar(
            "SELECT COUNT(*) FROM span WHERE pos='X' AND type='tok';"));
        Assert.Equal(0, GetScalar(
            "SELECT COUNT(*) FROM span WHERE pos IS NOT NULL AND type<>'tok';"));
    }

    [Fact]
    public void DeleteSpanAttributes_NoTokensInRange_NoError()
    {
        WordIndexTest.ResetDatabase();
        PgSqlIndexRepository repository = WordIndexTest.GetRepository();

        repository.DeleteSpanAttributes(1, 100, 200, ["address"]);

        Assert.Equal(2, GetScalar(
            "SELECT COUNT(*) FROM span_attribute WHERE name='address';"));
    }

    [Fact]
    public void DeleteSpanAttributes_NoNames_NoError()
    {
        WordIndexTest.ResetDatabase();
        PgSqlIndexRepository repository = WordIndexTest.GetRepository();

        repository.DeleteSpanAttributes(1, 1, 5, []);

        Assert.Equal(4, GetScalar("SELECT COUNT(*) FROM span_attribute;"));
    }

    [Fact]
    public void DeleteSpanAttributes_NameWithUnderscore_Deleted()
    {
        WordIndexTest.ResetDatabase();
        Execute("INSERT INTO span_attribute(span_id, name, value, type) " +
            "VALUES(1, 'pn_m', '1', 0), (6, 'pn_m', '1', 0);");
        PgSqlIndexRepository repository = WordIndexTest.GetRepository();

        repository.DeleteSpanAttributes(1, 1, 5, ["pn_m", "address"]);

        // token attributes deleted, while the sentence's one is kept
        Assert.Equal(0, GetScalar("SELECT COUNT(*) FROM span_attribute " +
            "WHERE name IN('pn_m','address') AND span_id<>6;"));
        Assert.Equal(1, GetScalar("SELECT COUNT(*) FROM span_attribute " +
            "WHERE name='pn_m' AND span_id=6;"));
    }

    [Fact]
    public async Task GetDocumentPairs_NumericNonPrivileged_UsesNumericRange()
    {
        WordIndexTest.ResetDatabase();
        // as strings, "10" < "9": the range must be 9-10, not 10-9
        Execute("INSERT INTO document_attribute(document_id, name, value, type) " +
            "VALUES(1, 'n', '9', 1), (1, 'n', '10', 1), (1, 'n', 'x', 1);");
        PgSqlIndexRepository repository = WordIndexTest.GetRepository();

        IList<DocumentPair> pairs = await repository.GetDocumentPairsAsync(
            new Dictionary<string, int> { ["date_value"] = 1, ["n"] = 1 },
            []);

        DocumentPair pair = Assert.Single(pairs, p => p.Name == "n");
        Assert.True(pair.IsNumeric);
        Assert.Equal(9, pair.MinValue);
        Assert.Equal(10, pair.MaxValue);
        Assert.True(pair.IsMaxIncluded);
    }

    [Fact]
    public void GetStatistics_TokenAttributeRatio_Present()
    {
        WordIndexTest.ResetDatabase();
        PgSqlIndexRepository repository = WordIndexTest.GetRepository();

        IDictionary<string, double> stats = repository.GetStatistics();

        // 4 attributes on 5 tokens
        Assert.Equal(5, stats["span.tok"]);
        Assert.Equal(0.8, stats["span_tok_attribute_count_ratio"], 3);
    }
}
