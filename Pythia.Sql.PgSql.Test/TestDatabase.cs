using Fusi.DbManager.PgSql;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pythia.Sql.PgSql.Test;

/// <summary>
/// The test database, created from scratch once for each test run, so that
/// it always reflects the current schema, functions, and data.
/// </summary>
internal static class TestDatabase
{
    private const string CST = "User ID=postgres;Password=postgres;" +
        "Host=localhost;Port=5432;Database={0};Include Error Detail=True";

    public const string DB_NAME = "pythia-test";

    public static string ConnectionString => string.Format(CST, DB_NAME);

    private static readonly Lazy<bool> _created = new(() =>
    {
        PgSqlDbManager manager = new(CST);
        if (manager.Exists(DB_NAME)) manager.RemoveDatabase(DB_NAME);

        manager.CreateDatabase(DB_NAME,
            new PgSqlIndexRepository().GetSchema(), null);
        manager.ExecuteCommands(DB_NAME, LoadResourceText("Data.pgsql"));
        return true;
    });

    private static string LoadResourceText(string name)
    {
        using Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"Pythia.Sql.PgSql.Test.Assets.{name}")!;
        return new StreamReader(stream, Encoding.UTF8).ReadToEnd();
    }

    /// <summary>
    /// Ensures that the test database was created in this test run.
    /// </summary>
    public static void EnsureCreated() => _ = _created.Value;
}
