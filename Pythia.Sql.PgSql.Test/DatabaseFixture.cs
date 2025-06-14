using Fusi.DbManager.PgSql;
using Npgsql;
using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pythia.Sql.PgSql.Test;

// https://xunit.net/docs/shared-context#class-fixture

public sealed class DatabaseFixture : IDisposable
{
    private const string CST = "User ID=postgres;Password=postgres;" +
        "Host=localhost;Port=5432;Database={0};Include Error Detail=True";
    private const string DB_NAME = "pythia-test";

    public IDbConnection Connection { get; }

    public static string ConnectionString => string.Format(CST, DB_NAME);

    public DatabaseFixture()
    {
        // setup database
        PgSqlDbManager manager = new(CST);

        if (!manager.Exists(DB_NAME))
        {
            string sql = new PgSqlIndexRepository().GetSchema();
            manager.CreateDatabase(DB_NAME, sql, null);
            manager.ExecuteCommands(DB_NAME, LoadResourceText("Data.pgsql"));
        }

        Connection = new NpgsqlConnection(ConnectionString);
    }

    public void Dispose()
    {
        Connection?.Dispose();
    }

    private static string LoadResourceText(string name)
    {
        using Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"Pythia.Sql.PgSql.Test.Assets.{name}")!;
        return new StreamReader(stream, Encoding.UTF8).ReadToEnd();
    }
}
