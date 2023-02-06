using Corpus.Core.Reading;
using Fusi.Tools.Configuration;
using Npgsql;
using System.Data;

namespace Pythia.Sql.PgSql;

/// <summary>
/// PostgreSQL text retriever.
/// Tag: <c>text-retriever.sql.pg</c>.
/// </summary>
/// <seealso cref="SqlTextRetriever" />
[Tag("text-retriever.sql.pg")]
public sealed class PgSqlTextRetriever : SqlTextRetriever, ITextRetriever,
    IConfigurable<SqlTextRetrieverOptions>
{
    /// <summary>
    /// Gets the database connection.
    /// </summary>
    /// <returns>
    /// The connection object.
    /// </returns>
    protected override IDbConnection GetConnection() =>
        new NpgsqlConnection(ConnectionString);
}
