using Fusi.Tools.Data;

namespace Corpus.Core;

/// <summary>
/// Corpus repository.
/// </summary>
public interface ICorpusRepository
{
    /// <summary>
    /// Gets the schema representing the model for the corpus repository data.
    /// </summary>
    /// <returns>Schema.</returns>
    string GetSchema();

    /// <summary>
    /// Gets the corpus with the specified ID.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>corpus or null if not found</returns>
    ICorpus? GetCorpus(string id);

    /// <summary>
    /// Gets the specified page of corpora matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="includeDocIds">if set to <c>true</c>, include
    /// documents IDs for each corpus; if <c>false</c>, just add a single
    /// number to each corpus documents array representing the total count
    /// of the documents included in that corpus.</param>
    /// <returns>page</returns>
    DataPage<ICorpus> GetCorpora(CorpusFilter filter, bool includeDocIds);

    /// <summary>
    /// Adds or updates the specified corpus.
    /// </summary>
    /// <param name="corpus">The corpus.</param>
    /// <param name="sourceId">The optional source corpus ID when the new
    /// corpus should get its content. This is useful to clone an existing
    /// corpus into a new one.</param>
    void AddCorpus(ICorpus corpus, string? sourceId = null);

    /// <summary>
    /// Deletes the corpus with the specified ID.
    /// </summary>
    /// <param name="id">The identifier.</param>
    void DeleteCorpus(string id);

    /// <summary>
    /// Adds the specified documents to the specified corpus. If the corpus
    /// does not exist, it will be created.
    /// </summary>
    /// <param name="corpusId">The corpus identifier.</param>
    /// <param name="userId">The optional user ID to assign to a new corpus.
    /// </param>
    /// <param name="documentIds">The document(s) ID(s).</param>
    void AddDocumentsToCorpus(string corpusId, string? userId,
        params int[] documentIds);

    /// <summary>
    /// Changes the corpus by specifying a documents filter.
    /// </summary>
    /// <param name="corpusId">The corpus ID, which can also be a new one.
    /// In this case, the corpus will be created.</param>
    /// <param name="userId">The optional user ID to assign to a new corpus.
    /// </param>
    /// <param name="filter">The documents filter.</param>
    /// <param name="add">if set to <c>true</c>, the matching documents
    /// will be added to the corpus; if set to <c>false</c>, they will be
    /// removed.</param>
    void ChangeCorpusByFilter(string corpusId, string? userId,
        DocumentFilter filter, bool add);

    /// <summary>
    /// True if the document with the specified ID is included in the corpus
    /// with the specified ID.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="corpusId">The corpus ID.</param>
    /// <param name="matchAsPrefix">True to treat <paramref name="corpusId"/> as
    /// a prefix, so that any corpus ID starting with it is a match.</param>
    /// <returns>True if included; otherwise, false.</returns>
    bool IsDocumentInCorpus(int documentId, string corpusId,
        bool matchAsPrefix);

    /// <summary>
    /// Gets the document with the specified ID.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="includeContent">If set to <c>true</c>, include the
    /// document's content.</param>
    /// <returns>document or null if not found</returns>
    IDocument? GetDocument(int id, bool includeContent);

    /// <summary>
    /// Gets the document with the specified source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="includeContent">If set to <c>true</c>, include the
    /// document's content.</param>
    /// <returns>document or null if not found</returns>
    IDocument? GetDocumentBySource(string source, bool includeContent);

    /// <summary>
    /// Gets the specified page of documents matching the specified filter.
    /// Note that the documents content is never returned, even if present.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>page</returns>
    DataPage<IDocument> GetDocuments(DocumentFilter filter);

    /// <summary>
    /// Adds the specified document. If a document with the same ID already
    /// exists, it will be deleted before adding this one.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="hasContent">If set to <c>true</c>, the document being
    /// passed has a content. Otherwise, the <see cref="IDocument.Content"/>
    /// property will not be updated.</param>
    /// <param name="hasAttributes">If set to <c>true</c>, the attributes
    /// of an existing document should be updated.</param>
    void AddDocument(IDocument document, bool hasContent, bool hasAttributes);

    /// <summary>
    /// Delete the document with the specified identifier with all its
    /// related data.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    void DeleteDocument(int id);

    /// <summary>
    /// Sets the content of the document with the specified identifier.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="content">The content, or null.</param>
    void SetDocumentContent(int id, string content);

    /// <summary>
    /// Adds the specified attribute.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    /// <param name="targetType">The type of target for this attribute.
    /// </param>
    /// <param name="unique">If set to <c>true</c>, replace any other
    /// attribute from the same document and with the same type with the
    /// new one.</param>
    void AddAttribute(Attribute attribute, string targetType, bool unique);

    /// <summary>
    /// Gets the names of the attributes matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>page of names, or all the names when page size is 0.
    /// </returns>
    DataPage<string> GetAttributeNames(AttributeFilter filter);

    /// <summary>
    /// Gets the content of the profile with the specified ID.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <param name="noContent">True to retrieve only the profile metadata,
    /// without its content.</param>
    /// <returns>The profile or null if not found.</returns>
    IProfile? GetProfile(string id, bool noContent = false);

    /// <summary>
    /// Gets the specified page of profiles.
    /// </summary>
    /// <param name="filter">The profiles filter. Set page size to 0
    /// to retrieve all the matching profiles at once.</param>
    /// <param name="noContent">True to retrieve only the profile metadata,
    /// without its content.</param>
    /// <returns>The page.</returns>
    DataPage<IProfile> GetProfiles(ProfileFilter filter,
        bool noContent = false);

    /// <summary>
    /// Adds or updates the specified profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    void AddProfile(IProfile profile);

    /// <summary>
    /// Delete the profile with the specified ID.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    void DeleteProfile(string id);
}
