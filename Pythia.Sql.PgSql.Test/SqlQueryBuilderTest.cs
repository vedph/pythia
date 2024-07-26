using Pythia.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Pythia.Sql.PgSql.Test;

public sealed class SqlQueryBuilderTest
{
    private class TestQuery(string id, string query, string? rowResult,
        string? countResult)
    {
        public string Id { get; } = id.Trim();
        public string Value { get; } = query.Trim();
        public string? RowResult { get; } = rowResult?.Trim();
        public string? CountResult { get; } = countResult?.Trim();

        public override string ToString()
        {
            return $"{Id}: {Value}";
        }
    }

    private static readonly List<TestQuery> _queries = LoadTestQueries();

    private readonly PgSqlHelper _helper = new();

    private static string LoadResource(string name)
    {
        using Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Pythia.Sql.PgSql.Test.Assets." + name)!;
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private static StreamReader GetResourceReader(string name)
    {
        Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Pythia.Sql.PgSql.Test.Assets." + name)!;
        return new(stream);
    }

    private static string NormalizeWS(string? text)
    {
        return text != null ? Regex.Replace(@"\s+", " ", text.Trim()) : "";
    }

    private (string rows, string count) GetSql(string query)
    {
        SqlQueryBuilder builder = new(_helper);
        var rc = builder.Build(new SearchRequest
        {
            Query = query,
        });
        return (rc.Item1, rc.Item2);
    }

    private static (string query, string? pendingLine) ReadQueryBody(
        StreamReader reader)
    {
        string? pendingLine = null;

        StringBuilder sb = new();
        while (!reader.EndOfStream)
        {
            string sqlLine = reader.ReadLine()!;
            if (sqlLine.StartsWith('#') || sqlLine.StartsWith(':'))
            {
                pendingLine = sqlLine;
                break;
            }
            sb.AppendLine(sqlLine);
        }
        return (sb.ToString(), pendingLine);
    }

    private static List<TestQuery> LoadTestQueries(string name = "Queries.txt")
    {
        List<TestQuery> queries = [];
        using StreamReader reader = GetResourceReader(name);
        string? id = null;
        string? query = null;
        string? rowResult = null;
        string? countResult = null;
        string? pendingLine = null;

        while (!reader.EndOfStream)
        {
            string line = pendingLine ?? reader.ReadLine()!;
            pendingLine = null;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('%')) continue;

            if (line.StartsWith('#'))
            {
                if (id != null)
                {
                    queries.Add(new TestQuery(id, query!, rowResult, countResult));
                    query = null;
                    rowResult = null;
                    countResult = null;
                }
                id = line[1..].Trim();
            }
            else if (line.StartsWith(":q"))
            {
                query = reader.ReadLine()!;
            }
            else if (line.StartsWith(":r"))
            {
                (rowResult, pendingLine) = ReadQueryBody(reader);
            }
            else if (line.StartsWith(":c"))
            {
                (countResult, pendingLine) = ReadQueryBody(reader);
            }
        }

        if (id != null)
            queries.Add(new TestQuery(id, query!, rowResult, countResult));

        return queries;
    }

    private void RunTestFor(TestQuery query)
    {
        (string rows, string count) = GetSql(query.Value);
        rows = NormalizeWS(rows);
        count = NormalizeWS(count);

        Assert.Equal(NormalizeWS(query.RowResult), rows);
        Assert.Equal(NormalizeWS(query.CountResult), count);
    }

    [Fact]
    public void SinglePair_Equals()
    {
        TestQuery query = _queries.First(q => q.Id == "single_pair_equals");
        RunTestFor(query);
    }

    [Fact]
    public void SinglePair_Not_Equals()
    {
        TestQuery query = _queries.First(q => q.Id == "single_pair_not_equals");
        RunTestFor(query);
    }

    [Fact]
    public void SinglePair_Non_Privileged()
    {
        TestQuery query = _queries.First(q => q.Id == "single_pair_non_privileged");
        RunTestFor(query);
    }

    [Fact]
    public void Two_Pairs_Or()
    {
        TestQuery query = _queries.First(q => q.Id == "two_pairs_or");
        RunTestFor(query);
    }

    [Fact]
    public void Two_Pairs_And()
    {
        TestQuery query = _queries.First(q => q.Id == "two_pairs_and");
        RunTestFor(query);
    }
}
