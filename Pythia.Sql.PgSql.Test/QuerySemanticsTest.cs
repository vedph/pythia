using Npgsql;
using Pythia.Core;
using Pythia.Core.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Pythia.Sql.PgSql.Test;

/// <summary>
/// Tests for the semantics of Pythia queries: the results of each query
/// are compared with the results of an oracle, i.e. a plain SQL query
/// written independently from the query builder, directly from the
/// definitions of the query language. Each test also checks that the total
/// count is equal to the number of distinct results.
/// </summary>
[Collection(nameof(NonParallelResourceCollection))]
public sealed class QuerySemanticsTest : IClassFixture<DatabaseFixture>
{
    private const string MAX = "2147483647";

    private readonly SqlQueryBuilder _builder = new(new PgSqlHelper());

    #region Helpers
    private static List<int> ReadIds(string sql)
    {
        using NpgsqlConnection connection = new(DatabaseFixture.ConnectionString);
        connection.Open();
        using NpgsqlCommand cmd = new(sql, connection);
        using NpgsqlDataReader reader = cmd.ExecuteReader();
        List<int> ids = [];
        while (reader.Read()) ids.Add(reader.GetInt32(0));
        return ids;
    }

    private static long ReadCount(string sql)
    {
        using NpgsqlConnection connection = new(DatabaseFixture.ConnectionString);
        connection.Open();
        using NpgsqlCommand cmd = new(sql, connection);
        return (long)cmd.ExecuteScalar()!;
    }

    private (List<int> ids, long count) Run(string query,
        IList<string>? sortFields = null, int pageNumber = 1, int pageSize = 10000)
    {
        Tuple<string, string> sql = _builder.Build(new SearchRequest
        {
            Query = query,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortFields = sortFields
        });
        return (ReadIds(sql.Item1), ReadCount(sql.Item2));
    }

    /// <summary>
    /// Tokens matching the specified condition on span.
    /// </summary>
    private static string Tok(string condition) =>
        $"SELECT * FROM span WHERE type='tok' AND {condition}";

    /// <summary>
    /// Tokens with an attribute named <paramref name="name"/> matching the
    /// specified condition on its value (v).
    /// </summary>
    private static string TokAttr(string name, string condition = "TRUE") =>
        Tok("EXISTS (SELECT 1 FROM span_attribute sa WHERE sa.span_id=span.id " +
            $"AND sa.name='{name}' AND {condition.Replace("v", "sa.value")})");

    /// <summary>
    /// Spans of A having (or not having) at least a span of B in the same
    /// document satisfying condition, optionally both inside the same
    /// structure of type s.
    /// </summary>
    private static string Loc(string a, string b, string condition,
        bool not = false, string? s = null)
    {
        string structure = s == null ? "" :
            " AND EXISTS (SELECT 1 FROM span st WHERE st.document_id=a.document_id" +
            $" AND st.type='{s}' AND st.p1<=a.p1 AND st.p1<=b.p1" +
            " AND st.p2>=a.p2 AND st.p2>=b.p2)";
        return $"SELECT a.* FROM ({a}) a WHERE {(not ? "NOT " : "")}EXISTS " +
            $"(SELECT 1 FROM ({b}) b WHERE b.document_id=a.document_id AND " +
            $"({condition}){structure})";
    }

    // definitions of positions from the query language documentation
    private static string PosBefore(string n, string m) =>
        $"a.p2 < b.p1 AND b.p1 - a.p2 - 1 BETWEEN {n} AND {m}";
    private static string PosAfter(string n, string m) =>
        $"a.p1 > b.p2 AND a.p1 - b.p2 - 1 BETWEEN {n} AND {m}";
    private static string PosNear(string n, string m) =>
        $"({PosBefore(n, m)}) OR ({PosAfter(n, m)})";
    private static string PosInside(string ns, string ms, string ne, string me) =>
        $"a.p1 >= b.p1 AND a.p2 <= b.p2 AND a.p1 - b.p1 BETWEEN {ns} AND {ms}" +
        $" AND b.p2 - a.p2 BETWEEN {ne} AND {me}";
    private static string PosOverlaps(string n, string m) =>
        "b.p1 <= a.p2 AND b.p2 >= a.p1 AND " +
        $"LEAST(a.p2, b.p2) - GREATEST(a.p1, b.p1) + 1 BETWEEN {n} AND {m}";
    private static string PosLAlign(string n, string m) =>
        $"a.p1 >= b.p1 AND a.p1 - b.p1 BETWEEN {n} AND {m}";
    private static string PosRAlign(string n, string m) =>
        $"a.p2 <= b.p2 AND b.p2 - a.p2 BETWEEN {n} AND {m}";

    private void AssertSame(string query, string oracle, bool allowEmpty = false)
    {
        List<int> expected = [.. ReadIds($"SELECT DISTINCT id FROM ({oracle}) o")
            .Order()];
        if (!allowEmpty) Assert.NotEmpty(expected);

        (List<int> ids, long count) = Run(query);

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Equal(expected, ids.Order().ToList());
        Assert.Equal(expected.Count, count);
    }
    #endregion

    #region Pairs
    [Theory]
    // token privileged attributes and text operators
    [InlineData("[value=\"sic\"]", "lower(value)='sic'")]
    [InlineData("[value=\"SIC\"]", "lower(value)='sic'")]
    [InlineData("[value<>\"sic\"]", "lower(value)<>'sic'")]
    [InlineData("[value*=\"ommo\"]", "lower(value) LIKE '%ommo%'")]
    [InlineData("[value^=\"ch\"]", "lower(value) LIKE 'ch%'")]
    [InlineData("[value$=\"ter\"]", "lower(value) LIKE '%ter'")]
    [InlineData("[value?=\"ch*da\"]", "lower(value) LIKE 'ch%da'")]
    [InlineData("[value?=\"?um\"]", "lower(value) LIKE '_um'")]
    [InlineData("[value?=\"sic\"]", "lower(value)='sic'")]
    [InlineData("[value~=\"^ch?ommoda$\"]", "value ~ '^ch?ommoda$'")]
    [InlineData("[value~=\"^CHOMMODA$\"]", "value ~* '^chommoda$'")]
    [InlineData("[value%=\"chommoda:0.5\"]", "similarity(lower(value),'chommoda')>=0.5")]
    [InlineData("[value%=\"chommoda:0.2\"]", "similarity(lower(value),'chommoda')>=0.2")]
    [InlineData("[value%=\"chommoda\"]", "similarity(lower(value),'chommoda')>=0.9")]
    [InlineData("[pos=\"noun\"]", "pos='NOUN'")]
    // escapes
    [InlineData("[value=\"&0073;ic\"]", "lower(value)='sic'")]
    // numeric privileged attributes
    [InlineData("[p1<\"5\"]", "p1<5")]
    [InlineData("[p1=\"5\"]", "p1=5")]
    [InlineData("[p1<>\"5\"]", "p1<>5")]
    [InlineData("[length>=\"9\"]", "length>=9")]
    [InlineData("[index!=\"0\"]", "index<>0")]
    public void TokenPair(string query, string condition)
    {
        AssertSame(query, Tok(condition));
    }

    [Theory]
    // non-privileged attributes, with or without a value
    [InlineData("[pn=\"arrius\"]", "pn", "lower(v)='arrius'")]
    [InlineData("[PN=\"Arrius\"]", "pn", "lower(v)='arrius'")]
    [InlineData("[gn]", "gn", "TRUE")]
    [InlineData("[q]", "q", "TRUE")]
    [InlineData("[case^=\"ab\"]", "case", "v LIKE 'ab%'")]
    // attribute names which were keywords for location arguments
    [InlineData("[number=\"plur\"]", "number", "v='plur'")]
    // numeric comparisons of text values
    [InlineData("[len==\"2\"]", "len", "v::int=2")]
    [InlineData("[len!=\"2\"]", "len", "v::int<>2")]
    [InlineData("[len>\"8\"]", "len", "v::int>8")]
    [InlineData("[len>=\"8\"]", "len", "v::int>=8")]
    [InlineData("[len<\"2\"]", "len", "v::int<2")]
    [InlineData("[len<=\"2.5\"]", "len", "v::int<=2")]
    // text comparison of numbers
    [InlineData("[len=\"2\"]", "len", "v='2'")]
    public void TokenAttributePair(string query, string name, string condition)
    {
        AssertSame(query, TokAttr(name, condition));
    }

    [Theory]
    // LIKE wildcards in values are literals
    [InlineData("[value*=\"%\"]")]
    [InlineData("[value*=\"_\"]")]
    [InlineData("[value^=\"s_c\"]")]
    [InlineData("[value?=\"s_c\"]")]
    [InlineData("[value?=\"s%\"]")]
    // quotes and SQL in values are literals
    [InlineData("[value=\"x'); DELETE FROM span; --\"]")]
    [InlineData("[value=\"x\n) OR (TRUE\"]")]
    [InlineData("[value*=\"'\"]")]
    // structure names are compared literally
    [InlineData("[$name=\"tok\"]")]
    public void Pair_NoMatch(string query)
    {
        (List<int> ids, long count) = Run(query);
        Assert.Empty(ids);
        Assert.Equal(0, count);
        // the data are still there
        Assert.NotEqual(0, ReadCount("SELECT COUNT(*) FROM span"));
    }

    [Theory]
    [InlineData("[$lg]", "type='lg'")]
    [InlineData("[$name=\"lg\"]", "type='lg'")]
    [InlineData("[$NAME=\"lg\"]", "type='lg'")]
    [InlineData("[$name^=\"l\"]", "type IN ('l', 'lg')")]
    [InlineData("[$name<>\"l\"]", "type NOT IN ('tok', 'l')")]
    [InlineData("[$p1<\"10\"]", "type<>'tok' AND p1<10")]
    // structure attributes
    [InlineData("[$l=\"5\"]", "type<>'tok' AND EXISTS (SELECT 1 FROM span_attribute sa " +
        "WHERE sa.span_id=span.id AND sa.name='l' AND sa.value='5')")]
    [InlineData("[$l>\"10\"]", "type<>'tok' AND EXISTS (SELECT 1 FROM span_attribute sa " +
        "WHERE sa.span_id=span.id AND sa.name='l' AND sa.value::int>10)")]
    // legacy aliases of structure attributes
    [InlineData("[$_l=\"5\"]", "type<>'tok' AND EXISTS (SELECT 1 FROM span_attribute sa " +
        "WHERE sa.span_id=span.id AND sa.name='l' AND sa.value='5')")]
    [InlineData("[_l=\"5\"]", "type<>'tok' AND EXISTS (SELECT 1 FROM span_attribute sa " +
        "WHERE sa.span_id=span.id AND sa.name='l' AND sa.value='5')")]
    [InlineData("[$lg] AND [_lg=\"2\"]", "type='lg' AND EXISTS (SELECT 1 FROM " +
        "span_attribute sa WHERE sa.span_id=span.id AND sa.name='lg' AND sa.value='2')")]
    public void StructurePair(string query, string condition)
    {
        AssertSame(query, $"SELECT * FROM span WHERE {condition}");
    }
    #endregion

    #region Logical
    [Theory]
    [InlineData("[value=\"sic\"] OR [value=\"mater\"]",
        "lower(value) IN ('sic','mater')")]
    [InlineData("[value=\"ionios\"] AND [gn]",
        "lower(value)='ionios' AND EXISTS (SELECT 1 FROM span_attribute sa " +
        "WHERE sa.span_id=span.id AND sa.name='gn')")]
    // AND binds tighter than OR
    [InlineData("[value=\"sic\"] OR [value=\"et\"] AND [pos=\"CCONJ\"]",
        "lower(value)='sic' OR (lower(value)='et' AND pos='CCONJ')")]
    [InlineData("[value=\"et\"] AND [pos=\"CCONJ\"] OR [value=\"sic\"]",
        "lower(value)='sic' OR (lower(value)='et' AND pos='CCONJ')")]
    [InlineData("([value=\"sic\"] OR [value=\"et\"]) AND [pos=\"CCONJ\"]",
        "lower(value) IN ('sic','et') AND pos='CCONJ'")]
    // AND NOT keeps spans having no value for the negated attribute
    [InlineData("[pos=\"NOUN\"] AND NOT [number=\"plur\"]",
        "pos='NOUN' AND NOT EXISTS (SELECT 1 FROM span_attribute sa " +
        "WHERE sa.span_id=span.id AND sa.name='number' AND sa.value='plur')")]
    [InlineData("[value=\"et\"] AND NOT [language=\"lat\"]",
        "lower(value)='et'")]
    public void Logical(string query, string condition)
    {
        AssertSame(query, Tok($"({condition})"));
    }

    [Fact]
    public void Logical_AndNotSelf_Empty()
    {
        AssertSame("[value=\"sic\"] AND NOT [value=\"sic\"]",
            Tok("FALSE"), true);
    }
    #endregion

    #region Scope
    [Theory]
    [InlineData("@[author=\"Catullus\"];", "document_id=1")]
    [InlineData("@[author=\"catullus\"];", "document_id=1")]
    [InlineData("@[author^=\"hor\"];", "document_id=2")]
    [InlineData("@[author=\"Catullus\"] OR [author=\"Horatius\"];",
        "document_id IN (1,2)")]
    // non-privileged attributes in AND must not be on the same attribute row
    [InlineData("@[category=\"poetry\"] AND [date-value=\"-54\"];", "document_id=1")]
    [InlineData("@[category=\"poetry\"] AND [date-value<\"-40\"];", "document_id=1")]
    [InlineData("@[category];", "document_id IN (1,2)")]
    // numeric privileged attributes
    [InlineData("@[date_value<\"-40\"];", "document_id=1")]
    [InlineData("@[date_value==\"-30\"];", "document_id=2")]
    [InlineData("@[date_value=\"-30\"];", "document_id=2")]
    [InlineData("@[id=\"2\"];", "document_id=2")]
    [InlineData("@[id==\"1\"];", "document_id=1")]
    [InlineData("@[ID>\"1\"];", "document_id=2")]
    // AND NOT, OR NOT
    [InlineData("@[category=\"poetry\"] AND NOT [author=\"Catullus\"];",
        "document_id=2")]
    [InlineData("@[author=\"Horatius\"] OR NOT [category=\"poetry\"];",
        "document_id=2")]
    // AND binds tighter than OR
    [InlineData("@[author=\"Catullus\"] OR [author=\"Horatius\"] AND " +
        "[title=\"carmina\"];", "document_id=1")]
    [InlineData("@([author=\"Catullus\"] OR [author=\"Horatius\"]) AND " +
        "[title^=\"carmina\"];", "document_id IN (1,2)")]
    // corpora
    [InlineData("@@neoteroi;", "document_id=1")]
    [InlineData("@@lyrici;", "document_id IN (1,2)")]
    [InlineData("@@\"Lyrici\";", "document_id IN (1,2)")]
    [InlineData("@@neoteroi lyrici;", "document_id IN (1,2)")]
    [InlineData("@@lyrici;@[author=\"Horatius\"];", "document_id=2")]
    public void Scope(string scope, string condition)
    {
        AssertSame(scope + "[value=\"et\"]",
            Tok($"lower(value)='et' AND {condition}"));
    }

    [Theory]
    [InlineData("@@alpha;")]
    [InlineData("@@\"neoteroi') OR (TRUE\";")]
    [InlineData("@[author=\"x' OR 'a'='a\"];")]
    public void Scope_NoMatch(string scope)
    {
        (List<int> ids, long count) = Run(scope + "[value=\"et\"]");
        Assert.Empty(ids);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Scope_Location_AppliesToBothOperands()
    {
        string et = Tok("lower(value)='et' AND document_id=2");
        string noun = Tok("pos='NOUN' AND document_id=2");
        AssertSame("@[author=\"Horatius\"];[value=\"et\"] NEAR(m=3) [pos=\"NOUN\"]",
            Loc(et, noun, PosNear("0", "3")));
    }
    #endregion

    #region Location
    private static readonly string Sic = Tok("lower(value)='sic'");
    private static readonly string Mater = Tok("lower(value)='mater'");
    private static readonly string Et = Tok("lower(value)='et'");
    private static readonly string Noun = Tok("pos='NOUN'");
    private static readonly string Verb = Tok("pos='VERB'");

    [Fact]
    public void Near_M0()
    {
        AssertSame("[value=\"sic\"] NEAR(m=0) [value=\"mater\"]",
            Loc(Sic, Mater, PosNear("0", "0")));
    }

    [Fact]
    public void NotNear_M0()
    {
        AssertSame("[value=\"sic\"] NOT NEAR(m=0) [value=\"mater\"]",
            Loc(Sic, Mater, PosNear("0", "0"), true));
    }

    [Fact]
    public void Near_N1M3()
    {
        AssertSame("[value=\"et\"] NEAR(n=1,m=3) [pos=\"NOUN\"]",
            Loc(Et, Noun, PosNear("1", "3")));
    }

    [Fact]
    public void Near_NoArgs()
    {
        AssertSame("[value=\"sic\"] NEAR() [value=\"sic\"]",
            Loc(Sic, Sic, PosNear("0", MAX)));
    }

    [Fact]
    public void Near_Self_NotIncluded()
    {
        // a span is never near to itself
        AssertSame("[value=\"chommoda\"] NEAR() [value=\"chommoda\"]",
            Tok("FALSE"), true);
    }

    [Fact]
    public void Near_S()
    {
        AssertSame("[value=\"et\"] NEAR(m=5,s=l) [pos=\"NOUN\"]",
            Loc(Et, Noun, PosNear("0", "5"), s: "l"));
    }

    [Fact]
    public void NotNear_S()
    {
        AssertSame("[value=\"et\"] NOT NEAR(m=5,s=l) [pos=\"NOUN\"]",
            Loc(Et, Noun, PosNear("0", "5"), true, "l"));
    }

    [Fact]
    public void Near_S_DiffersFromNoS()
    {
        (List<int> withS, _) = Run("[pos=\"NOUN\"] NEAR(m=8,s=l) [pos=\"VERB\"]");
        (List<int> withoutS, _) = Run("[pos=\"NOUN\"] NEAR(m=8) [pos=\"VERB\"]");
        Assert.True(withS.Count < withoutS.Count);
        Assert.Subset(withoutS.ToHashSet(), withS.ToHashSet());
    }

    [Fact]
    public void Before()
    {
        AssertSame("[value=\"sic\"] BEFORE(m=0) [value=\"mater\"]",
            Loc(Sic, Mater, PosBefore("0", "0")));
    }

    [Fact]
    public void Before_S()
    {
        AssertSame("[pos=\"NOUN\"] BEFORE(n=1,m=4,s=lg) [pos=\"VERB\"]",
            Loc(Noun, Verb, PosBefore("1", "4"), s: "lg"));
    }

    [Fact]
    public void NotBefore()
    {
        AssertSame("[value=\"sic\"] NOT BEFORE(m=0) [value=\"mater\"]",
            Loc(Sic, Mater, PosBefore("0", "0"), true));
    }

    [Fact]
    public void After()
    {
        AssertSame("[pos=\"NOUN\"] AFTER(m=2) [pos=\"VERB\"]",
            Loc(Noun, Verb, PosAfter("0", "2")));
    }

    [Fact]
    public void NotAfter_S()
    {
        AssertSame("[pos=\"NOUN\"] NOT AFTER(m=2,s=snt) [pos=\"VERB\"]",
            Loc(Noun, Verb, PosAfter("0", "2"), true, "snt"));
    }

    [Theory]
    [InlineData("[value$=\"ter\"] INSIDE(me=0) [$l]", "0", MAX, "0", "0")]
    [InlineData("[value=\"et\"] INSIDE() [$l]", "0", MAX, "0", MAX)]
    [InlineData("[value=\"et\"] INSIDE(ns=1,ms=3) [$l]", "1", "3", "0", MAX)]
    [InlineData("[value=\"et\"] INSIDE(ns=0,ms=0) [$l]", "0", "0", "0", MAX)]
    [InlineData("[value=\"et\"] INSIDE(ne=2,me=4) [$lg]", "0", MAX, "2", "4")]
    public void Inside(string query, string ns, string ms, string ne, string me)
    {
        string a = Tok(query.Contains("ter")
            ? "lower(value) LIKE '%ter'" : "lower(value)='et'");
        string type = query.Contains("[$lg]") ? "lg" : "l";
        AssertSame(query, Loc(a, $"SELECT * FROM span WHERE type='{type}'",
            PosInside(ns, ms, ne, me)));
    }

    [Fact]
    public void NotInside()
    {
        AssertSame("[len==\"2\"] NOT INSIDE() [$lg]",
            Loc(TokAttr("len", "v::int=2"), "SELECT * FROM span WHERE type='lg'",
                PosInside("0", MAX, "0", MAX), true));
    }

    [Theory]
    [InlineData("[$l] OVERLAPS() [$snt]", "1", MAX, false)]
    [InlineData("[$l] OVERLAPS(n=2,m=4) [$snt]", "2", "4", false)]
    [InlineData("[$l] OVERLAPS(n=1,m=3) [$snt]", "1", "3", false)]
    [InlineData("[$l] NOT OVERLAPS(n=3) [$snt]", "3", MAX, true)]
    public void Overlaps_Structures(string query, string n, string m, bool not)
    {
        AssertSame(query, Loc("SELECT * FROM span WHERE type='l'",
            "SELECT * FROM span WHERE type='snt'", PosOverlaps(n, m), not), not);
    }

    [Theory]
    [InlineData("[$l] LALIGN(m=0) [$snt]", "0", "0", false)]
    [InlineData("[$l] LALIGN(n=1,m=3) [$snt]", "1", "3", false)]
    [InlineData("[$l] NOT LALIGN(m=0) [$snt]", "0", "0", true)]
    public void LAlign_Structures(string query, string n, string m, bool not)
    {
        AssertSame(query, Loc("SELECT * FROM span WHERE type='l'",
            "SELECT * FROM span WHERE type='snt'", PosLAlign(n, m), not));
    }

    [Theory]
    [InlineData("[$l] RALIGN(m=0) [$snt]", "0", "0", false)]
    [InlineData("[$l] RALIGN(n=1,m=3) [$snt]", "1", "3", false)]
    [InlineData("[$l] NOT RALIGN(m=0) [$snt]", "0", "0", true)]
    public void RAlign_Structures(string query, string n, string m, bool not)
    {
        AssertSame(query, Loc("SELECT * FROM span WHERE type='l'",
            "SELECT * FROM span WHERE type='snt'", PosRAlign(n, m), not));
    }

    [Fact]
    public void LAlign_TokenAtVerseStart()
    {
        AssertSame("[pos=\"NOUN\"] LALIGN(m=0) [$l]",
            Loc(Noun, "SELECT * FROM span WHERE type='l'", PosLAlign("0", "0")));
    }

    [Fact]
    public void RAlign_TokenAtVerseEnd()
    {
        AssertSame("[pos=\"VERB\"] RALIGN(m=0,s=lg) [$l]",
            Loc(Verb, "SELECT * FROM span WHERE type='l'", PosRAlign("0", "0"),
                s: "lg"));
    }

    [Fact]
    public void Chain_IsRightAssociative()
    {
        // sic immediately before mater, which is immediately before sic
        string materBeforeSic = Loc(Mater, Sic, PosBefore("0", "0"));
        AssertSame("[value=\"sic\"] BEFORE(m=0) [value=\"mater\"] BEFORE(m=0) " +
            "[value=\"sic\"]",
            Loc(Sic, materBeforeSic, PosBefore("0", "0")));
        (List<int> ids, _) = Run("[value=\"sic\"] BEFORE(m=0) [value=\"mater\"] " +
            "BEFORE(m=0) [value=\"sic\"]");
        Assert.Single(ids);
    }

    [Fact]
    public void Chain_KeepsIntermediateConditions()
    {
        // the condition on the middle operand must not be lost: this has 4
        // results, while it would have 30 without the condition on VERB
        string query = "[pos=\"NOUN\"] NEAR(m=1) [pos=\"VERB\"] NEAR(m=3) " +
            "[value=\"et\"]";
        AssertSame(query,
            Loc(Noun, Loc(Verb, Et, PosNear("0", "3")), PosNear("0", "1")));
        Assert.Equal(4, Run(query).count);
    }

    [Fact]
    public void Chain_Parenthesized_LeftOperand()
    {
        // NOUN near a VERB, and also after "et"
        AssertSame("([pos=\"NOUN\"] NEAR(m=2) [pos=\"VERB\"]) AFTER(m=3) [value=\"et\"]",
            Loc(Loc(Noun, Verb, PosNear("0", "2")), Et, PosAfter("0", "3")));
    }

    [Theory]
    // samples from the documentation: "credo, sic mater, sic liber"
    [InlineData("[value=\"sic\"] BEFORE(m=0,s=l) [value=\"mater\"] BEFORE(m=0,s=l) " +
        "[value=\"sic\"]")]
    [InlineData("([value=\"sic\"] BEFORE(m=0) [value=\"mater\"]) AFTER(m=5) " +
        "[value=\"credo\"]")]
    public void DocumentationSample_FirstSicOfLine5(string query)
    {
        (List<int> ids, long count) = Run(query);
        Assert.Equal(1, count);
        Assert.Equal(ReadIds("SELECT s.id FROM span s WHERE s.type='tok' AND " +
            "s.value='sic' AND s.document_id=1 AND EXISTS (SELECT 1 FROM span m " +
            "WHERE m.document_id=1 AND m.value='mater' AND m.p1=s.p1+1)"), ids);
    }

    [Fact]
    public void Location_RightOr()
    {
        AssertSame("[value=\"sic\"] BEFORE(m=0) ([value=\"mater\"] OR [value=\"liber\"])",
            Loc(Sic, Tok("lower(value) IN ('mater','liber')"), PosBefore("0", "0")));
    }

    [Fact]
    public void Location_OrBindsTighter()
    {
        // (chommoda OR sic) NEAR mater
        AssertSame("[value=\"chommoda\"] OR [value=\"sic\"] NEAR(m=0) [value=\"mater\"]",
            Loc(Tok("lower(value) IN ('chommoda','sic')"), Mater, PosNear("0", "0")));
    }

    [Fact]
    public void Location_Or()
    {
        AssertSame("([value=\"sic\"] NEAR(m=0) [value=\"mater\"]) OR [value=\"chommoda\"]",
            $"{Loc(Sic, Mater, PosNear("0", "0"))} UNION {Tok("lower(value)='chommoda'")}");
    }

    [Fact]
    public void Location_And()
    {
        AssertSame("([value=\"et\"] NEAR(m=3) [pos=\"VERB\"]) AND " +
            "([value=\"et\"] NEAR(m=3) [pos=\"NOUN\"])",
            $"{Loc(Et, Verb, PosNear("0", "3"))} INTERSECT {Loc(Et, Noun, PosNear("0", "3"))}");
    }

    [Fact]
    public void Location_AndNot()
    {
        AssertSame("[value=\"et\"] AND NOT ([value=\"et\"] NEAR(m=0) [pos=\"VERB\"])",
            $"{Et} EXCEPT {Loc(Et, Verb, PosNear("0", "0"))}");
    }

    [Fact]
    public void Location_LeftAndRightCompoundFilters()
    {
        AssertSame("[pos=\"NOUN\"] AND [number=\"plur\"] NEAR(m=2) " +
            "[pos=\"VERB\"] OR [pos=\"AUX\"]",
            Loc(Tok("pos='NOUN' AND EXISTS (SELECT 1 FROM span_attribute sa " +
                "WHERE sa.span_id=span.id AND sa.name='number' AND sa.value='plur')"),
                Tok("pos IN ('VERB','AUX')"), PosNear("0", "2")));
    }

    [Fact]
    public void Location_Huge_Distance_NoOverflow()
    {
        AssertSame("[value=\"sic\"] NEAR(m=2147483646) [value=\"mater\"]",
            Loc(Sic, Mater, PosNear("0", "2147483646")));
    }
    #endregion

    #region Sort and paging
    [Fact]
    public void Paging_IsDeterministic()
    {
        (List<int> all, long count) = Run("[value=\"et\"] OR [value=\"sic\"]");
        List<int> paged = [];
        for (int page = 1; paged.Count < count; page++)
        {
            (List<int> ids, _) = Run("[value=\"et\"] OR [value=\"sic\"]", null, page, 2);
            Assert.NotEmpty(ids);
            paged.AddRange(ids);
        }
        Assert.Equal(all, paged);
    }

    [Theory]
    [InlineData("-p1")]
    [InlineData("author")]
    [InlineData("-date_value")]
    [InlineData("reversed_value")]
    [InlineData("date-value")]
    [InlineData("id")]
    [InlineData("last_modified")]
    public void Sort_Executes(string field)
    {
        (List<int> ids, long count) = Run("[value=\"et\"]", [field]);
        Assert.Equal(count, ids.Count);
        Assert.Equal(6, ids.Count);
    }

    [Fact]
    public void Sort_ByP1Descending()
    {
        (List<int> ids, _) = Run("@[author=\"Catullus\"];[pos=\"NOUN\"]", ["-p1"]);
        List<int> expected = ReadIds("SELECT id FROM span WHERE type='tok' " +
            "AND pos='NOUN' AND document_id=1 ORDER BY p1 DESC, id");
        Assert.Equal(expected, ids);
    }

    [Fact]
    public void Sort_ByCustomDocumentAttribute()
    {
        // attribute values are sorted as text: Catullus ("-54") comes
        // before Horatius ("-30") in descending order
        (List<int> ids, _) = Run("[value=\"et\"]", ["-date-value", "p1"]);
        List<int> expected = ReadIds("SELECT id FROM span WHERE type='tok' " +
            "AND lower(value)='et' ORDER BY document_id, p1, id");
        Assert.Equal(expected, ids);
    }
    #endregion

    #region Errors
    [Theory]
    // syntax: trailing text, invalid characters, OR NOT in text
    [InlineData("[value=\"a\"] [value=\"b\"]")]
    [InlineData("[value=\"a\"] #")]
    [InlineData("[value=\"a\"] OR NOT [value=\"b\"]")]
    [InlineData("[value=\"a\"")]
    [InlineData("[$]")]
    [InlineData("[value=\"a\"] NEAR [value=\"b\"]")]
    // location arguments
    [InlineData("[value=\"a\"] NEAR(x=1) [value=\"b\"]")]
    [InlineData("[value=\"a\"] NEAR(ns=1) [value=\"b\"]")]
    [InlineData("[value=\"a\"] INSIDE(n=1) [$l]")]
    [InlineData("[value=\"a\"] NEAR(m=1,m=2) [value=\"b\"]")]
    [InlineData("[value=\"a\"] NEAR(n=3,m=2) [value=\"b\"]")]
    [InlineData("[value=\"a\"] INSIDE(ns=3,ms=2) [$l]")]
    [InlineData("[value=\"a\"] INSIDE(ne=3,me=2) [$l]")]
    [InlineData("[value=\"a\"] NEAR(m=x) [value=\"b\"]")]
    [InlineData("[value=\"a\"] NEAR(s=1) [value=\"b\"]")]
    [InlineData("[value=\"a\"] NEAR(m=99999999999) [value=\"b\"]")]
    [InlineData("[value=\"a\"] NEAR(m=-1) [value=\"b\"]")]
    // numeric values
    [InlineData("[len>\"abc\"]")]
    [InlineData("[len>\"0 OR 1=1\"]")]
    [InlineData("[p1=\"0 OR TRUE\"]")]
    [InlineData("[p1*=\"1\"]")]
    [InlineData("@[id=\"1 OR 1=1\"];[value=\"et\"]")]
    [InlineData("@[date_value^=\"1\"];[value=\"et\"]")]
    // short pairs with privileged attributes
    [InlineData("[value]")]
    [InlineData("[$value]AND[value]")]
    [InlineData("@[author];[value=\"et\"]")]
    public void InvalidQuery_Throws(string query)
    {
        Assert.Throws<PythiaQueryException>(() => _builder.Build(
            new SearchRequest { Query = query }));
    }

    [Fact]
    public void InvalidQuery_HasPosition()
    {
        PythiaQueryException ex = Assert.Throws<PythiaQueryException>(
            () => _builder.Build(new SearchRequest
            {
                Query = "[value=\"a\"] NEAR(m=1,x=2) [value=\"b\"]"
            }));
        Assert.Equal(1, ex.Line);
        Assert.Equal(21, ex.Column);
        Assert.Equal(21, ex.Index);
        Assert.Equal(1, ex.Length);
    }
    #endregion
}
