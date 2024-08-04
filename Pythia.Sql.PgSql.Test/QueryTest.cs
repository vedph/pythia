using Corpus.Sql;
using Fusi.Tools.Data;
using Pythia.Core;
using Xunit;

namespace Pythia.Sql.PgSql.Test;

// https://github.com/xunit/xunit/issues/1999

[Collection(nameof(NonParallelResourceCollection))]
public sealed class QueryTest : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly SqlIndexRepository _repository;

    public QueryTest(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _repository = new PgSqlIndexRepository();
        _repository.Configure(new SqlRepositoryOptions
        {
            ConnectionString = DatabaseFixture.ConnectionString
        });
    }

    #region Helpers
    private static SearchResult ParseResult(string csv)
    {
        // 0 id
        // 1 document_id
        // 2 p1
        // 3 p2
        // 4 type
        // 5 index
        // 6 length
        // 7 value
        // 8 author
        // 9 title
        // 10 sort_key
        string[] fields = csv.Split(',');
        return new SearchResult
        {
            Id = int.Parse(fields[0]),
            DocumentId = int.Parse(fields[1]),
            P1 = int.Parse(fields[2]),
            P2 = int.Parse(fields[3]),
            Type = fields[4],
            Index = int.Parse(fields[5]),
            Length = int.Parse(fields[6]),
            Value = fields[7],
            Author = fields[8],
            Title = fields[9],
            SortKey = fields[10]
        };
    }

    /// <summary>
    /// Asserts the result.
    /// </summary>
    /// <param name="expectedCsv">The expected CSV.</param>
    /// <param name="actual">The actual.</param>
    private static void AssertResult(string expectedCsv, SearchResult actual)
    {
        SearchResult expected = ParseResult(expectedCsv);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.DocumentId, actual.DocumentId);
        Assert.Equal(expected.P1, actual.P1);
        Assert.Equal(expected.P2, actual.P2);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.Index, actual.Index);
        Assert.Equal(expected.Length, actual.Length);
        Assert.Equal(expected.Value, actual.Value);
        Assert.Equal(expected.Author, actual.Author);
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.SortKey, actual.SortKey);
    }
    #endregion

    #region Single token
    [Fact]
    public void ValueEqChommoda_1()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value=\"chommoda\"]"
        });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        AssertResult(
            "3,1,3,3,tok,1123,8,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
    }

    [Fact]
    public void ValueEqChommodaInAuthorEqCatullus_1()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "@[author=\"Catullus\"];[value=\"chommoda\"]"
        });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        SearchResult result = page.Items[0];
        AssertResult(
            "3,1,3,3,tok,1123,8,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            result);
    }

    [Fact]
    public void ValueEqChommodaInCorpus_1()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "@@neoteroi;@[author=\"Catullus\"];[value=\"chommoda\"]"
        });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        AssertResult(
            "3,1,3,3,tok,1123,8,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
    }

    [Fact]
    public void ValueEqChommodaInNotExistingCorpus_0()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "@@alpha;@[author=\"Catullus\"];[value=\"chommoda\"]"
        });
        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    [Fact]
    public void ValueEqSic_3()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value=\"sic\"]"
        });
        Assert.Equal(3, page.Total);
        Assert.Equal(3, page.Items.Count);
        AssertResult(
            "27,1,27,27,tok,1655,3,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "29,1,29,29,tok,1666,3,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
        AssertResult(
            "33,1,33,33,tok,1736,3,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[2]);
    }

    [Fact]
    public void ValueNotEqSic_180()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value<>\"sic\"]"
        });
        // select count(*) from span where type = 'tok' returns 183,
        // so here we matched all the tokens except the 3 'sic'
        Assert.Equal(180, page.Total);
        Assert.Equal(20, page.Items.Count);
    }

    [Fact]
    public void ValueContainsOmmo_2()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value*=\"ommo\"]"
        });
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        AssertResult(
            "3,1,3,3,tok,1123,8,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "7,1,7,7,tok,1159,7,commoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
    }

    [Fact]
    public void ValueStartsWithCh_1()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value^=\"ch\"]"
        });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        AssertResult(
            "3,1,3,3,tok,1123,8,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
    }

    [Fact]
    public void ValueEndsWithTer_4()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value$=\"ter\"]"
        });
        Assert.Equal(4, page.Total);
        Assert.Equal(4, page.Items.Count);
        AssertResult(
            "28,1,28,28,tok,1659,6,mater,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "49,1,49,49,tok,2037,7,leniter,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
        AssertResult(
            "51,1,51,51,tok,2048,8,leviter,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[2]);
        AssertResult(
            "126,2,29,29,tok,1376,8,iuppiter,Horatius,carmina liber I,horatius-carminaliberi-A-0030.00",
            page.Items[3]);
    }

    [Fact]
    public void ValueMatchesWildsChStarDa_1()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value?=\"ch*da\"]"
        });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        AssertResult(
            "3,1,3,3,tok,1123,8,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
    }

    [Fact]
    public void ValueMatchesRegexChommoda_2()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value~=\"ch?ommoda\"]"
        });
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        AssertResult(
            "3,1,3,3,tok,1123,8,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "7,1,7,7,tok,1159,7,commoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
    }

    [Fact]
    public void ValueMatchesFuzzyChommoda_2()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value%=\"chommoda:0.5\"]"
        });
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        AssertResult(
            "3,1,3,3,tok,1123,8,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "7,1,7,7,tok,1159,7,commoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
    }

    [Fact]
    public void PnEqArrius_2()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[pn=\"Arrius\"]"
        });
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        AssertResult("12,1,12,12,tok,1249,6,arrius,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "67,1,67,67,tok,2461,6,arrius,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
    }

    [Fact]
    public void LenGreaterThan9_4()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[len>\"9\"]"
        });
        Assert.Equal(4, page.Total);
        Assert.Equal(4, page.Items.Count);
        AssertResult(
            "43,1,43,43,tok,1922,10,requierant,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "62,1,62,62,tok,2279,11,horribilis,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
        AssertResult(
            "101,2,4,4,tok,1036,11,quaesieris,Horatius,carmina liber I,horatius-carminaliberi-A-0030.00",
            page.Items[2]);
        AssertResult(
            "113,2,16,16,tok,1185,10,babylonios,Horatius,carmina liber I,horatius-carminaliberi-A-0030.00",
            page.Items[3]);
    }
    #endregion

    #region Single structure
    [Fact]
    public void StrNameEqLg_9()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[$name=\"lg\"]"
        });
        Assert.Equal(9, page.Total);
        Assert.Equal(9, page.Items.Count);
        AssertResult("76,1,3,13,lg,1053,138,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult("77,1,14,25,lg,1337,138,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
        AssertResult("78,1,26,38,lg,1585,138,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[2]);
        AssertResult("79,1,39,51,lg,1818,166,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[3]);
        AssertResult("80,1,52,62,lg,2101,138,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[4]);
        AssertResult("81,1,63,74,lg,2335,135,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[5]);
        AssertResult("209,2,59,76,lg,1873,190,,Horatius,carmina liber I,horatius-carminaliberi-A-0030.00",
            page.Items[6]);
        AssertResult("210,2,77,92,lg,2238,181,,Horatius,carmina liber I,horatius-carminaliberi-A-0030.00",
            page.Items[7]);
        AssertResult("211,2,93,109,lg,2594,184,,Horatius,carmina liber I,horatius-carminaliberi-A-0030.00",
            page.Items[8]);
    }

    [Fact]
    public void StrLg_9()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[$lg]"
        });
        Assert.Equal(9, page.Total);
        Assert.Equal(9, page.Items.Count);
        AssertResult("76,1,3,13,lg,1053,138,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult("77,1,14,25,lg,1337,138,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
        AssertResult("78,1,26,38,lg,1585,138,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[2]);
        AssertResult("79,1,39,51,lg,1818,166,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[3]);
        AssertResult("80,1,52,62,lg,2101,138,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[4]);
        AssertResult("81,1,63,74,lg,2335,135,,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[5]);
        AssertResult("209,2,59,76,lg,1873,190,,Horatius,carmina liber I,horatius-carminaliberi-A-0030.00",
            page.Items[6]);
        AssertResult("210,2,77,92,lg,2238,181,,Horatius,carmina liber I,horatius-carminaliberi-A-0030.00",
            page.Items[7]);
        AssertResult("211,2,93,109,lg,2594,184,,Horatius,carmina liber I,horatius-carminaliberi-A-0030.00",
            page.Items[8]);
    }
    #endregion

    #region Two tokens
    [Fact]
    public void ValueEqChommodaOrMater_2()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value=\"chommoda\"] OR [value=\"mater\"]"
        });
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        AssertResult(
            "3,1,3,3,tok,1123,8,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "28,1,28,28,tok,1659,6,mater,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
    }

    [Fact]
    public void ValueEqIoniosAndGn_2()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value=\"ionios\"] AND [gn]"
        });
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        AssertResult(
            "63,1,63,63,tok,2409,6,ionios,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "71,1,71,71,tok,2550,6,ionios,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
    }

    [Fact]
    public void ValueEqIoniosAndNotGn_0()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value=\"ionios\"] AND NOT [gn]"
        });
        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }
    #endregion

    #region Collocations
    [Fact]
    public void ValueSicNearMater_2()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value=\"sic\"] NEAR(m=0,s=l) [value=\"mater\"]"
        });
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        AssertResult(
            "27,1,27,27,tok,1655,3,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "29,1,29,29,tok,1666,3,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
    }

    [Fact]
    public void ValueSicNotNearMater_1()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value=\"sic\"] NOT NEAR(m=0) [value=\"mater\"]"
        });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        AssertResult(
            "33,1,33,33,tok,1736,3,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
    }

    [Fact]
    public void ValueSicBeforeMater_1()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value=\"sic\"] BEFORE(m=0,s=l) [value=\"mater\"]"
        });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        AssertResult(
            "27,1,27,27,tok,1655,3,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
    }

    [Fact]
    public void ValueSicNotBeforeMater_1()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value=\"sic\"] NOT BEFORE(m=0) [value=\"mater\"]"
        });
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        AssertResult(
            "1,29,824,3,t,29,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,33,872,3,t,33,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
    }

    [Fact]
    public void ValueSicAfterMater_1()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value=\"sic\"] AFTER(m=0,s=l) [value=\"mater\"]"
        });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        AssertResult(
            "1,29,824,3,t,29,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
    }

    [Fact]
    public void ValueSicNotAfterMater_2()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value=\"sic\"] NOT AFTER(m=0) [value=\"mater\"]"
        });
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        AssertResult(
            "1,27,813,3,t,27,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,33,872,3,t,33,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
    }

    [Fact]
    public void ValueTerInsideL_3()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value$=\"ter\"] INSIDE() [$l]"
        });
        Assert.Equal(3, page.Total);
        Assert.Equal(3, page.Items.Count);
        AssertResult(
            "1,28,817,6,t,28,mater,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,49,1073,7,t,49,leniter,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
        AssertResult(
            "1,51,1084,8,t,51,leviter,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[2]);
    }

    [Fact]
    public void ValueTerInsideLAtEnd_1()
    {
        // -mater, -leniter, +leviter
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value$=\"ter\"] INSIDE(me=0) [$l]"
        });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        AssertResult(
            "1,51,1084,8,t,51,leviter,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
    }

    [Fact]
    public void ValueLen2InsideL_6()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[len=\"2\"] INSIDE() [$lg]"
        });
        Assert.Equal(6, page.Total);
        Assert.Equal(6, page.Items.Count);
    }

    [Fact]
    public void ValueLen2NotInsideL_1()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[len=\"2\"] NOT INSIDE() [$lg]"
        });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        AssertResult(
            "1,1,364,2,t,1,ad,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
    }
    #endregion
}
