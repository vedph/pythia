using Corpus.Core;
using Corpus.Core.Reading;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Pythia.Sql
{
    /// <summary>
    /// Base class for SQL-based text retrievers. Use these retrievers when you
    /// store the document's content inline with the document's metadata,
    /// in <c>document.content</c>. This retriever just gets the document from
    /// its source, and reads its content property.
    /// </summary>
    /// <seealso cref="ITextRetriever" />
    public abstract class SqlTextRetriever
    {
        protected string ConnectionString { get; private set; }

        /// <summary>
        /// Configures the object with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">options</exception>
        public void Configure(SqlTextRetrieverOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            ConnectionString = options.ConnectionString;
        }

        /// <summary>
        /// Gets the database connection.
        /// </summary>
        /// <returns>The connection object.</returns>
        protected abstract IDbConnection GetConnection();

        /// <summary>
        /// Retrieve the text from the specified document. This retriever
        /// just gets the document from the database, and returns its text
        /// from its <see cref="Document.Content"/> property.
        /// </summary>
        /// <param name="document">The document to retrieve text for.</param>
        /// <param name="context">Not used.</param>
        /// <returns>Text, or null if not found.</returns>
        public Task<string> GetAsync(Document document, object context)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            using IDbConnection connection = GetConnection();
            connection.Open();
            IDbCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT content FROM document WHERE document_id="
                + document.Id;
            string content = cmd.ExecuteScalar() as string;

            return Task.FromResult(content);
        }
    }

    /// <summary>
    /// Options for <see cref="SqlTextRetriever"/>.
    /// </summary>
    public class SqlTextRetrieverOptions
    {
        /// <summary>
        /// Gets or sets the connection string to the documents SQL database.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}
