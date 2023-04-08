using Fusi.DbManager.PgSql;
using Fusi.DbManager;
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
    private const string CST =
        "User ID=postgres;Password=postgres;Host=localhost;Port=5432;Database={0}";
    private const string DB_NAME = "pythia-test";

    public IDbConnection Connection { get; }

    public static string ConnectionString => string.Format(CST, DB_NAME);

    public DatabaseFixture()
    {
        // setup database
        IDbManager manager = new PgSqlDbManager(CST);

        if (!manager.Exists(DB_NAME))
        {
            manager.CreateDatabase(DB_NAME,
                new PgSqlIndexRepository().GetSchema(), null);
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
