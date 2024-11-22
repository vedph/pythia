using Corpus.Core;
using Corpus.Core.Reading;
using Fusi.Tools.Configuration;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Corpus.Sql.PgSql;

/// <summary>
/// SqlServer-based text retriever.
/// Tag: <c>text-retriever.sql.pg</c>.
/// </summary>
/// <seealso cref="ITextRetriever" />
[Tag("text-retriever.sql.pg")]
public sealed class PgSqlTextRetriever : ITextRetriever,
    IConfigurable<PgSqlTextRetrieverOptions>
{
    private string? _connStr;

    /// <summary>
    /// Configures the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(PgSqlTextRetrieverOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _connStr = options.ConnectionString;
    }

    /// <summary>
    /// Retrieve the text from the specified document.
    /// </summary>
    /// <param name="document">The document to retrieve text for.</param>
    /// <param name="context">The optional context object, whose type
    /// and function depend on the implementor.</param>
    /// <returns>Text, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">document</exception>
    public Task<string?> GetAsync(IDocument document, object? context = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        using NpgsqlConnection conn = new(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT content FROM document WHERE id=@id;";
        cmd.Parameters.Add(new NpgsqlParameter("@id", document.Id));
        return Task.FromResult(cmd.ExecuteScalar() as string);
    }
}

/// <summary>
/// Options for <see cref="PgSqlTextRetriever"/>.
/// </summary>
public class PgSqlTextRetrieverOptions
{
    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string? ConnectionString { get; set; }
}
