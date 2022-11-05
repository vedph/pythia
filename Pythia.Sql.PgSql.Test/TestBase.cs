using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Npgsql;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pythia.Sql.PgSql.Test
{
    public abstract class TestBase
    {
        protected const string CST =
            "User ID=postgres;Password=postgres;Host=localhost;Port=5432;Database={0}";
        protected const string DB_NAME = "pythia-test";
        static protected readonly string CS = string.Format(CST, DB_NAME);

        protected TestBase()
        {
            // init the DB once, it's used only for reading
            Init();
        }

        private static string LoadResourceText(string name)
        {
            using Stream stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream($"Pythia.Sql.PgSql.Test.Assets.{name}")!;
            return new StreamReader(stream, Encoding.UTF8).ReadToEnd();
        }

        private static void Init()
        {
            IDbManager manager = new PgSqlDbManager(CST);

            if (manager.Exists(DB_NAME)) return;

            manager.CreateDatabase(DB_NAME,
                new PgSqlIndexRepository().GetSchema(), null);
            manager.ExecuteCommands(DB_NAME, LoadResourceText("Data.pgsql"));
        }

        protected static IDbConnection GetConnection() => new NpgsqlConnection(CS);
    }
}
