using Pythia.Core;
using System.Data;
using Xunit;

namespace Pythia.Sql.PgSql.Test;

// https://github.com/xunit/xunit/issues/1999

[CollectionDefinition(nameof(NonParallelResourceCollection),
    DisableParallelization = true)]
public class NonParallelResourceCollection { }

[Collection(nameof(NonParallelResourceCollection))]
public sealed class FunctionsTest : TestBase
{
    [Theory]
    [InlineData(2, 2, 2)]
    [InlineData(2, 5, 2)]
    [InlineData(5, 2, 2)]
    public void PytMin_Ok(int a, int b, int expected)
    {
        using IDbConnection conn = GetConnection();
        conn.Open();
        IDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT pyt_min({a},{b});";
        int result = (cmd.ExecuteScalar() as int?) ?? 0;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(2, 2, 2)]
    [InlineData(2, 5, 5)]
    [InlineData(5, 2, 5)]
    public void PytMax_Ok(int a, int b, int expected)
    {
        using IDbConnection conn = GetConnection();
        conn.Open();
        IDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT pyt_max({a},{b});";
        int result = (cmd.ExecuteScalar() as int?) ?? 0;
        Assert.Equal(expected, result);
    }

    // pyt_is_overlap
    [Theory]
    [InlineData(2, 2, 2, 2, true)]
    [InlineData(2, 2, 3, 3, false)]
    [InlineData(2, 2, 1, 1, false)]
    [InlineData(2, 4, 2, 4, true)]
    [InlineData(2, 4, 3, 5, true)]
    [InlineData(2, 4, 1, 2, true)]
    public void PytIsOverlap_Ok(int a1, int a2, int b1, int b2, bool expected)
    {
        using IDbConnection conn = GetConnection();
        conn.Open();
        IDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT pyt_is_overlap({a1},{a2},{b1},{b2});";

        // this additional C# test is used to allow debugging the fn logic,
        // as all the PLpg/SQL functions are just translations from C#
        bool r = SpanDistanceCalculator.IsOverlap(a1, a2, b1, b2);
        Assert.Equal(expected, r);

        bool result = (cmd.ExecuteScalar() as bool?) ?? true;
        Assert.Equal(expected, result);
    }

    // pyt_get_overlap_count
    [Theory]
    [InlineData(2, 2, 2, 2, 1)]
    [InlineData(2, 2, 3, 3, 0)]
    [InlineData(2, 2, 1, 1, 0)]
    [InlineData(2, 4, 2, 4, 3)]
    [InlineData(2, 4, 3, 5, 2)]
    [InlineData(2, 4, 1, 2, 1)]
    public void PytGetOverlapCount_Ok(int a1, int a2, int b1, int b2, int expected)
    {
        using IDbConnection conn = GetConnection();
        conn.Open();
        IDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT pyt_get_overlap_count({a1},{a2},{b1},{b2});";

        int r = SpanDistanceCalculator.GetOverlapCount(a1, a2, b1, b2);
        Assert.Equal(expected, r);

        int result = (cmd.ExecuteScalar() as int?) ?? 0;
        Assert.Equal(expected, result);
    }

    // pyt_is_overlap_within
    // pyt_is_overlap
    [Theory]
    [InlineData(2, 2, 2, 2, 1, 10, true)]
    [InlineData(2, 2, 3, 3, 1, 10, false)]
    [InlineData(2, 2, 1, 1, 1, 10, false)]
    [InlineData(2, 4, 2, 4, 1, 10, true)]
    // 123456
    //  +++
    //   ***
    [InlineData(2, 4, 3, 5, 1, 10, true)]
    [InlineData(2, 4, 3, 5, 2, 10, true)]
    // min not satisfied
    [InlineData(2, 4, 3, 5, 3, 10, false)]
    // max not satisfied
    [InlineData(2, 4, 3, 5, 1, 1, false)]
    // 123456
    //  +++
    // ***
    [InlineData(2, 4, 1, 3, 1, 10, true)]
    [InlineData(2, 4, 1, 3, 2, 10, true)]
    // min not satisfied
    [InlineData(2, 4, 1, 3, 3, 10, false)]
    // max not satisfied
    [InlineData(2, 4, 1, 3, 1, 1, false)]
    public void PytIsOverlapWithin_Ok(int a1, int a2, int b1, int b2,
        int n, int m, bool expected)
    {
        using IDbConnection conn = GetConnection();
        conn.Open();
        IDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT pyt_is_overlap_within(" +
            $"{a1},{a2},{b1},{b2},{n},{m});";

        bool r = SpanDistanceCalculator.IsOverlapWithin(a1, a2, b1, b2, n, m);
        Assert.Equal(expected, r);

        bool result = (cmd.ExecuteScalar() as bool?) ?? true;
        Assert.Equal(expected, result);
    }

    // pyt_is_inside_within
    [Theory]
    // equal
    [InlineData(2, 2, 2, 2, 0, 0, 0, 0, true)]
    [InlineData(2, 3, 2, 3, 0, 0, 0, 0, true)]
    // 123456
    //  ++
    //  ***
    [InlineData(2, 3, 2, 4, 0, 0, 0, 10, true)]
    // min start-distance (1) not satisfied
    [InlineData(2, 3, 2, 4, 1, 10, 0, 10, false)]
    // max end-distance (0) not satisfied
    [InlineData(2, 3, 2, 4, 0, 0, 0, 0, false)]
    // 123456
    //   ++
    //  ***
    [InlineData(3, 4, 2, 4, 0, 10, 0, 0, true)]
    // max-start-distance (0) not satisfied
    [InlineData(3, 4, 2, 4, 0, 0, 0, 10, false)]
    // min end-distance (1) not satisfied
    [InlineData(3, 4, 2, 4, 0, 10, 1, 10, false)]
    // 123456
    //  +
    //  ***
    [InlineData(2, 2, 2, 4, 0, 10, 0, 10, true)]
    // min start-distance (1) not satisfied
    [InlineData(2, 2, 2, 4, 1, 10, 0, 10, false)]
    // max end-distance (0) not satisfied
    [InlineData(2, 2, 2, 4, 0, 10, 0, 0, false)]
    // 123456
    //   +
    //  ***
    [InlineData(3, 3, 2, 4, 0, 10, 0, 10, true)]
    [InlineData(3, 3, 2, 4, 1, 10, 1, 10, true)]
    // min start-distance (2) not satisfied
    [InlineData(3, 3, 2, 4, 2, 10, 0, 10, false)]
    // min end-distance (2) not satisfied
    [InlineData(3, 3, 2, 4, 0, 10, 2, 10, false)]
    // max start-distance (0) not satisfied
    [InlineData(3, 3, 2, 4, 0, 0, 0, 10, false)]
    // max end-distance (0) not satisfied
    [InlineData(3, 3, 2, 4, 0, 10, 0, 0, false)]
    // 123456
    //    +
    //  ***
    [InlineData(4, 4, 2, 4, 0, 10, 0, 10, true)]
    // max start-distance (0) not satisfied
    [InlineData(4, 4, 2, 4, 0, 0, 0, 10, false)]
    // max start-distance (1) not satisfied
    [InlineData(4, 4, 2, 4, 0, 1, 0, 10, false)]
    // max start-distance (2) satisfied
    [InlineData(4, 4, 2, 4, 0, 2, 0, 10, true)]
    // min end-distance (1) not satisfied
    [InlineData(4, 4, 2, 4, 0, 10, 1, 10, false)]
    public void PytIsInsideWithin_Ok(int a1, int a2, int b1, int b2,
        int ns, int ms, int ne, int me, bool expected)
    {
        using IDbConnection conn = GetConnection();
        conn.Open();
        IDbCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT pyt_is_inside_within(" +
            $"{a1},{a2},{b1},{b2},{ns},{ms},{ne},{me});";

        bool r = SpanDistanceCalculator.IsInsideWithin(a1, a2, b1, b2,
            ns, ms, ne, me);
        Assert.Equal(expected, r);

        bool result = (cmd.ExecuteScalar() as bool?) ?? true;
        Assert.Equal(expected, result);
    }

    // pyt_is_before_within
    // pyt_is_after_within
    // pyt_is_near_within
    // pyt_is_left_aligned
    // pyt_is_right_aligned
}
