using Npgsql;
using System;
using System.Data;

namespace Pythia.Sql.PgSql.Test;

// https://xunit.net/docs/shared-context#class-fixture

public sealed class DatabaseFixture : IDisposable
{
    public IDbConnection Connection { get; }

    public static string ConnectionString => TestDatabase.ConnectionString;

    public DatabaseFixture()
    {
        TestDatabase.EnsureCreated();
        Connection = new NpgsqlConnection(ConnectionString);
    }

    public void Dispose()
    {
        Connection?.Dispose();
    }
}
