using Corpus.Core;
using Corpus.Core.Reading;
using Fusi.Tools.Configuration;
using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace Corpus.Sql.MsSql;

/// <summary>
/// SqlServer-based text retriever.
/// Tag: <c>text-retriever.sql.ms</c>.
/// </summary>
/// <seealso cref="ITextRetriever" />
[Tag("text-retriever.sql.ms")]
public sealed class MsSqlTextRetriever : ITextRetriever,
    IConfigurable<MsSqlTextRetrieverOptions>
{
    private string? _connStr;

    /// <summary>
    /// Configures the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(MsSqlTextRetrieverOptions options)
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

        using SqlConnection conn = new SqlConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT content FROM document WHERE id=@id;";
        cmd.Parameters.Add(new SqlParameter("@id", document.Id));
        return Task.FromResult(cmd.ExecuteScalar() as string);
    }
}

/// <summary>
/// Options for <see cref="MsSqlTextRetriever"/>.
/// </summary>
public class MsSqlTextRetrieverOptions
{
    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string? ConnectionString { get; set; }
}
