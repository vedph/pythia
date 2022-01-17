using Corpus.Core;
using Corpus.Sql.PgSql;
using Npgsql;
using Pythia.Core;
using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pythia.Sql.PgSql
{
    /// <summary>
    /// PostgreSQL-based Pythia index repository.
    /// </summary>
    /// <seealso cref="SqlIndexRepository" />
    public sealed class PgSqlIndexRepository : SqlIndexRepository
    {
        private readonly PgSqlCorpusRepository _corpus;

        /// <summary>
        /// Initializes a new instance of the <see cref="PgSqlIndexRepository"/>
        /// class.
        /// </summary>
        public PgSqlIndexRepository() :
            base(new PgSqlHelper(), new PgSqlCorpusRepository())
        {
            _corpus = (PgSqlCorpusRepository)CorpusRepository;
        }

        private static string LoadResourceText(string name)
        {
            using StreamReader reader = new(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    $"Pythia.Sql.PgSql.Assets.{name}"), Encoding.UTF8);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Gets the DDL SQL code for the database schema. This is the sum
        /// of the corpus schema plus the index schema.
        /// </summary>
        /// <returns>SQL code.</returns>
        public override string GetSchema()
        {
            StringBuilder sql = new();

            // corpus
            sql.AppendLine(CorpusRepository.GetSchema());

            // pythia
            sql.AppendLine(LoadResourceText("Schema.pgsql"));

            // functions
            sql.AppendLine(LoadResourceText("Functions.pgsql"));

            return sql.ToString();
        }

        /// <summary>
        /// Gets a new connection object.
        /// </summary>
        /// <returns>The connection.</returns>
        protected override IDbConnection GetConnection()
            => new NpgsqlConnection(ConnectionString);

        /// <summary>
        /// Builds the paging expression with the specified values.
        /// </summary>
        /// <param name="offset">The offset count.</param>
        /// <param name="limit">The limit count.</param>
        /// <returns>SQL code.</returns>
        protected override string GetPagingSql(int offset, int limit)
            => $"OFFSET {offset} LIMIT {limit}";

        /// <summary>
        /// Upserts the specified structure.
        /// </summary>
        /// <param name="structure">The structure.</param>
        /// <param name="connection">The connection.</param>
        /// <exception cref="ArgumentNullException">structure or connection</exception>
        public override void UpsertStructure(Structure structure,
            IDbConnection connection)
        {
            if (structure == null)
                throw new ArgumentNullException(nameof(structure));
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            IDbCommand cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO structure" +
                "(document_id, start_position, end_position, name) " +
                "VALUES(@document_id, @start_position, @end_position, @name)\n" +
                "ON CONFLICT(id) DO UPDATE\n" +
                "SET document_id=@document_id, start_position=@start_position, " +
                "end_position=@end_position, name=@name\n" +
                "RETURNING id;";
            AddParameter(cmd, "@document_id", DbType.Int32, structure.DocumentId);
            AddParameter(cmd, "@start_position", DbType.Int32, structure.StartPosition);
            AddParameter(cmd, "@end_position", DbType.Int32, structure.EndPosition);
            AddParameter(cmd, "@name", DbType.String, structure.Name);

            int n = (int)cmd.ExecuteScalar();
            if (structure.Id == 0) structure.Id = n;
            foreach (Corpus.Core.Attribute attribute in structure?.Attributes)
                attribute.TargetId = n;
        }

        /// <summary>
        /// Upserts the profile.
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <param name="connection">The connection.</param>
        public override void UpsertProfile(IProfile profile, IDbConnection connection)
        {
            _corpus.UpsertProfile(profile, connection);
        }

        /// <summary>
        /// Upserts the attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="target">The target.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="tr">The tr.</param>
        public override void UpsertAttribute(Corpus.Core.Attribute attribute,
            string target, IDbConnection connection, IDbTransaction tr = null)
            => _corpus.UpsertAttribute(attribute, target, connection, tr);

        /// <summary>
        /// Upserts the document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="hasContent">if set to <c>true</c> [has content].</param>
        /// <param name="connection">The connection.</param>
        /// <param name="tr">The tr.</param>
        public override void UpsertDocument(IDocument document, bool hasContent,
            IDbConnection connection, IDbTransaction tr = null)
            => _corpus.UpsertDocument(document, hasContent, connection, tr);
    }
}
