using Corpus.Sql;
using Fusi.Tools.Data;
using Pythia.Core;
using Xunit;

namespace Pythia.Sql.PgSql.Test;

#region Tokens dump
// first tokens in sample document:
// 1 ad
// 2 arrium
// 3 chommoda
// 4 dicebat
// 5 si
// 6 quando
// 7 commoda
// 8 vellet
// 9 dicere
// 10 et
// 11 insidias
// 12 arrius
// 13 hinsidias
// 14 et
// 15 tum
// 16 mirifice
// 17 sperabat
// 18 se
// 19 esse
// 20 locutum
// 21 cum
// 22 quantum
// 23 poterat
// 24 dixerat
// 25 hinsidias
// 26 credo
// 27 sic
// 28 mater
// 29 sic
// 30 liber
// 31 avunculus
// 32 eius
// 33 sic
// 34 maternus
// 35 avus
// 36 dixerat
// 37 atque
// 38 avia
#endregion

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
        // document_id
        // position
        // index
        // length
        // entity_type
        // entity_id
        // value
        // author
        // title
        // sort_key
        string[] fields = csv.Split(',');
        return new SearchResult
        {
            DocumentId = int.Parse(fields[0]),
            Position = int.Parse(fields[1]),
            Index = int.Parse(fields[2]),
            Length = short.Parse(fields[3]),
            EntityType = fields[4],
            EntityId = int.Parse(fields[5]),
            Value = fields[6],
            Author = fields[7],
            Title = fields[8],
            SortKey = fields[9]
        };
    }

    private static void AssertResult(string expectedCsv, SearchResult actual)
    {
        SearchResult expected = ParseResult(expectedCsv);
        Assert.Equal(expected.DocumentId, actual.DocumentId);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.Index, actual.Index);
        Assert.Equal(expected.Length, actual.Length);
        Assert.Equal(expected.EntityType, actual.EntityType);
        Assert.Equal(expected.EntityId, actual.EntityId);
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
        AssertResult("1,3,431,8,t,3," +
            "chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
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
        AssertResult("1,3,431,8,t,3,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            result);
    }

    [Fact]
    public void ValueEqChommodaInCorpus_0()
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
            "1,27,813,3,t,27,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,29,824,3,t,29,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
        AssertResult(
            "1,33,872,3,t,33,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[2]);
    }

    [Fact]
    public void ValueNotEqSic_71()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value<>\"sic\"]"
        });
        Assert.Equal(71, page.Total);
        Assert.Equal(20, page.Items.Count);
        AssertResult(
            "1,1,364,2,t,1,ad,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,20,666,8,t,20,locutum,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[19]);
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
            "1,3,431,8,t,3,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,7,467,7,t,7,commoda,Catullus,carmina,catullus-carmina-A-0054.00",
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
        SearchResult result = page.Items[0];
        AssertResult("1,3,431,8,t,3," +
            "chommoda,Catullus,carmina,catullus-carmina-A-0054.00", result);
    }

    [Fact]
    public void ValueEndsWithTer_3()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value$=\"ter\"]"
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
    public void ValueMatchesWildsChStarDa_1()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[value?=\"ch*da\"]"
        });
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        AssertResult("1,3,431,8,t,3,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
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
        AssertResult("1,3,431,8,t,3,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,7,467,7,t,7,commoda,Catullus,carmina,catullus-carmina-A-0054.00",
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
        AssertResult("1,3,431,8,t,3,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,7,467,7,t,7,commoda,Catullus,carmina,catullus-carmina-A-0054.00",
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
        AssertResult("1,12,535,6,t,12,arrius,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,67,1369,6,t,67,arrius,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
    }

    [Fact]
    public void LenGreaterThan9_2()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[len>\"9\"]"
        });
        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        AssertResult("1,43,1005,10,t,43,requierant,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,62,1240,11,t,62,horribilis,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[1]);
    }
    #endregion

    #region Single structure
    [Fact]
    public void StrNameEqLg_72()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[$name=\"lg\"]"
        });
        Assert.Equal(72, page.Total);
        Assert.Equal(20, page.Items.Count);
        AssertResult("1,3,431,8,s,2," +
            "chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult("1,22,702,7,s,5," +
            "quantum,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[19]);
    }

    [Fact]
    public void StrLg_72()
    {
        DataPage<SearchResult> page = _repository.Search(new SearchRequest
        {
            Query = "[$lg]"
        });
        Assert.Equal(72, page.Total);
        Assert.Equal(20, page.Items.Count);
        AssertResult("1,3,431,8,s,2," +
            "chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult("1,22,702,7,s,5," +
            "quantum,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[19]);
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
            "1,3,431,8,t,3,chommoda,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,28,817,6,t,28,mater,Catullus,carmina,catullus-carmina-A-0054.00",
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
            "1,63,1317,6,t,63,ionios,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,71,1436,6,t,71,ionios,Catullus,carmina,catullus-carmina-A-0054.00",
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
            "1,27,813,3,t,27,sic,Catullus,carmina,catullus-carmina-A-0054.00",
            page.Items[0]);
        AssertResult(
            "1,29,824,3,t,29,sic,Catullus,carmina,catullus-carmina-A-0054.00",
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
            "1,33,872,3,t,33,sic,Catullus,carmina,catullus-carmina-A-0054.00",
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
            "1,27,813,3,t,27,sic,Catullus,carmina,catullus-carmina-A-0054.00",
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
