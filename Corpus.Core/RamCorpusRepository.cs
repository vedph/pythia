using Fusi.Tools.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Corpus.Core;

/// <summary>
/// RAM-based repository, essentially used for testing purposes.
/// </summary>
public class RamCorpusRepository : ICorpusRepository
{
    /// <summary>The documents dictionary.</summary>
    protected readonly ConcurrentDictionary<int, IDocument> Documents;

    /// <summary>The profiles dictionary.</summary>
    protected readonly ConcurrentDictionary<string, IProfile> Profiles;

    /// <summary>The corpora dictionary.</summary>
    protected readonly ConcurrentDictionary<string, ICorpus> Corpora;

    private int _nextDocId;
    private int _nextAttrId;

    /// <summary>
    /// Initializes a new instance of the <see cref="RamCorpusRepository"/>
    /// class.
    /// </summary>
    public RamCorpusRepository()
    {
        _nextDocId = 1;
        _nextAttrId = 1;
        Documents = new ConcurrentDictionary<int, IDocument>();
        Profiles = new ConcurrentDictionary<string, IProfile>();
        Corpora = new ConcurrentDictionary<string, ICorpus>();
    }

    /// <summary>
    /// Gets the schema representing the model for the corpus repository data.
    /// </summary>
    /// <returns>Schema.</returns>
    public string GetSchema() => "";

    /// <summary>
    /// Gets the corpus with the specified ID.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>corpus or null if not found</returns>
    /// <exception cref="T:System.ArgumentNullException">null ID</exception>
    public ICorpus? GetCorpus(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        return Corpora.ContainsKey(id) ? Corpora[id] : null;
    }

    private static IQueryable<ICorpus> ApplyCorpusFilter(
        IQueryable<ICorpus> corpora, CorpusFilter filter)
    {
        if (!string.IsNullOrEmpty(filter.Id))
        {
            corpora = corpora.Where(c => c.Id!.IndexOf(filter.Id,
                StringComparison.Ordinal) > -1);
        }

        if (!string.IsNullOrEmpty(filter.Title))
        {
            corpora = corpora.Where(c => c.Title!.IndexOf(filter.Title,
                StringComparison.OrdinalIgnoreCase) > -1);
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            corpora = corpora.Where(c => c.UserId == filter.UserId);
        }

        return corpora;
    }

    /// <summary>
    /// Gets the specified page of corpora matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="includeDocIds">if set to <c>true</c>, include
    /// documents IDs for each corpus; if <c>false</c>, just add a single
    /// number to each corpus documents array representing the total count
    /// of the documents included in that corpus.</param>
    /// <returns>page</returns>
    /// <exception cref="T:System.ArgumentNullException">filter</exception>
    public DataPage<ICorpus> GetCorpora(CorpusFilter filter, bool includeDocIds)
    {
        ArgumentNullException.ThrowIfNull(filter);

        IQueryable<ICorpus> corpora = Corpora.Values.AsQueryable();

        corpora = ApplyCorpusFilter(corpora, filter);

        int total = corpora.Count();
        corpora = corpora.OrderBy(c => c.Title);

        return new DataPage<ICorpus>(
            filter.PageNumber, filter.PageSize,
            total,
            corpora.Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize).ToList());
    }

    /// <summary>
    /// Adds or updates the specified corpus.
    /// </summary>
    /// <param name="corpus">The corpus.</param>
    /// <param name="sourceId">Not used.</param>
    /// <exception cref="ArgumentNullException">corpus or corpus.Id</exception>
    public void AddCorpus(ICorpus corpus, string? sourceId = null)
    {
        if (corpus?.Id == null) throw new ArgumentNullException(nameof(corpus));

        Corpora[corpus.Id] = corpus;
    }

    /// <summary>
    /// Deletes the corpus with the specified ID.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <exception cref="T:System.ArgumentNullException">null ID</exception>
    public void DeleteCorpus(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (Corpora.ContainsKey(id)) Corpora.TryRemove(id, out ICorpus _);
    }

    /// <summary>
    /// Adds the specified documents to the specified corpus. If the corpus
    /// does not exist, it will be created.
    /// </summary>
    /// <param name="corpusId">The corpus identifier.</param>
    /// <param name="userId">The optional user ID to assign to a new corpus.
    /// </param>
    /// <param name="documentIds">The document(s) ID(s).</param>
    /// <exception cref="ArgumentNullException">corpusId</exception>
    public void AddDocumentsToCorpus(string corpusId, string? userId,
        params int[] documentIds)
    {
        ArgumentNullException.ThrowIfNull(corpusId);

        if (!Corpora.ContainsKey(corpusId))
        {
            Corpora[corpusId] = new Corpus
            {
                UserId = userId,
            };
        }

        ICorpus corpus = Corpora[corpusId];
        foreach (int id in documentIds)
        {
            if (corpus.DocumentIds?.Contains(id) == false)
                corpus.DocumentIds!.Add(id);
        }
    }

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
    /// <exception cref="NotImplementedException"></exception>
    public void ChangeCorpusByFilter(string corpusId, string? userId,
        DocumentFilter filter,
        bool add)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// True if the document with the specified ID is included in the corpus
    /// with the specified ID.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="corpusId">The corpus ID.</param>
    /// <param name="matchAsPrefix">True to treat <paramref name="corpusId"/> as
    /// a prefix, so that any corpus ID starting with it is a match.</param>
    /// <returns>True if included; otherwise, false.</returns>
    public bool IsDocumentInCorpus(int documentId, string corpusId,
        bool matchAsPrefix)
    {
        return Corpora.Any(p => (matchAsPrefix
            ? p.Key.StartsWith(corpusId)
            : p.Key == corpusId) &&
            p.Value.DocumentIds?.Contains(documentId) == true);
    }

    /// <summary>
    /// Gets the document with the specified ID.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="includeContent">If set to <c>true</c>, include the
    /// document's content.</param>
    /// <returns>document or null if not found</returns>
    public IDocument? GetDocument(int id, bool includeContent)
    {
        IDocument? document = Documents.ContainsKey(id) ?
            Documents[id] : null;
        if (document == null) return null;

        if (!includeContent) document.Content = null;
        return document;
    }

    /// <summary>
    /// Gets the document with the specified source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="includeContent">If set to <c>true</c>, include the
    /// document's content.</param>
    /// <returns>document or null if not found</returns>
    /// <exception cref="ArgumentNullException">source</exception>
    public IDocument? GetDocumentBySource(string source, bool includeContent)
    {
        ArgumentNullException.ThrowIfNull(source);

        IDocument? document = Documents.Values
            .FirstOrDefault(d => d.Source == source);
        if (!includeContent && document?.Content != null)
            document.Content = null;
        return document;
    }

    private static IQueryable<IDocument> ApplyDocumentFilters(
        IQueryable<IDocument> documents,
        DocumentFilter filter)
    {
        if (!string.IsNullOrEmpty(filter.Author))
        {
            documents = documents
               .Where(d => d.Author!.Contains(filter.Author));
        }

        if (!string.IsNullOrEmpty(filter.Title))
        {
            documents = documents
               .Where(d => d.Title!.Contains(filter.Title));
        }

        if (!string.IsNullOrEmpty(filter.Source))
        {
            documents = documents
               .Where(d => d.Source!.Contains(filter.Source));
        }

        if (!string.IsNullOrEmpty(filter.ProfileId))
        {
            documents = documents
               .Where(d => d.ProfileId == filter.ProfileId);
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            documents = documents
               .Where(d => d.UserId == filter.UserId);
        }

        if (!string.IsNullOrEmpty(filter.ProfileIdPrefix))
        {
            documents = documents
               .Where(d => d.ProfileId!.StartsWith(filter.ProfileIdPrefix));
        }

        if (filter.MinDateValue != 0.0)
        {
            documents = documents.Where(
               d => d.DateValue >= filter.MinDateValue);
        }

        if (filter.MaxDateValue != 0.0)
        {
            documents = documents.Where(
               d => d.DateValue <= filter.MaxDateValue);
        }

        if (filter.MinTimeModified.HasValue)
        {
            documents = documents.Where(
               d => d.LastModified >= filter.MinTimeModified.Value);
        }

        if (filter.MaxTimeModified.HasValue)
        {
            documents = documents.Where(
               d => d.LastModified <= filter.MaxTimeModified.Value);
        }

        if (filter.Attributes?.Count > 0)
        {
            documents = documents.Where(d => d.Attributes!.Any
            (a => filter.Attributes.Any
                (t => t.Item1 == a.Name && a.Value!.Contains(t.Item2))));
        }

        return documents;
    }

    /// <summary>
    /// Gets the specified page of documents matching the specified filter.
    /// Note that the documents content is never returned, even if present.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>page</returns>
    /// <exception cref="T:System.ArgumentNullException">null filter</exception>
    public DataPage<IDocument> GetDocuments(DocumentFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        IQueryable<IDocument> documents = Documents.Values.AsQueryable();
        documents = ApplyDocumentFilters(documents, filter);
        int total = documents.Count();
        documents = documents.OrderBy(d => d.SortKey);

        var results = documents
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();
        results.ForEach(r => r.Content = null);
        return new DataPage<IDocument>(filter.PageNumber, filter.PageSize,
            total, results);
    }

    private static void UpdateAttribute(Attribute old, Attribute added)
    {
        old.Id = added.Id;
        old.TargetId = added.TargetId;
        old.Name = added.Name;
        old.Value = added.Value;
        old.Type = added.Type;
    }

    private static void UpdateAttributes(IDocument old, IDocument added)
    {
        if (added.Attributes == null || added.Attributes.Count == 0) return;

        // corner case if no old attributes: just add all
        if (old.Attributes == null || old.Attributes.Count == 0)
        {
            if (old.Attributes == null) old.Attributes = new List<Attribute>();
            foreach (var a in added.Attributes) old.Attributes.Add(a);
            return;
        }

        // no more existing in added = removed
        var removedEntities = old.Attributes!
            .Where(o => added.Attributes.All(a => a.Id != o.Id))
            .ToList();

        // not existing in old = added
        var addedEntities = added.Attributes
            .Where(a => old.Attributes.All(o => o.Id != a.Id))
            .ToList();

        // still existing in added = kept
        var keptEntities = old.Attributes
            .Where(o => added.Attributes.Any(a => a.Id == o.Id))
            .ToList();

        // remove, add and update
        foreach (Attribute attribute in removedEntities)
            old.Attributes.Remove(attribute);

        foreach (Attribute attribute in addedEntities)
            old.Attributes.Add(attribute);

        foreach (Attribute attribute in keptEntities)
        {
            UpdateAttribute(attribute,
               added.Attributes.First(a => a.Id == attribute.Id));
        }
    }

    /// <summary>
    /// Adds the specified document. If a document with the same ID already
    /// exists, it will be deleted before adding this one.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="hasContent">If set to <c>true</c>, the document being
    /// passed has a content. Otherwise, the <see cref="Document.Content" />
    /// property will not be updated.</param>
    /// <param name="hasAttributes">If set to <c>true</c>, the attributes
    /// of an existing document should be updated.</param>
    /// <exception cref="T:System.ArgumentNullException">null document
    /// </exception>
    public void AddDocument(IDocument document, bool hasContent,
        bool hasAttributes)
    {
        ArgumentNullException.ThrowIfNull(document);

        // update an existing document
        if (document.Id > 0
            && Documents.TryGetValue(document.Id, out IDocument? old))
        {
            old.Author = document.Author;
            old.Title = document.Title;
            old.DateValue = document.DateValue;
            old.SortKey = document.SortKey;
            old.Source = document.Source;
            old.ProfileId = document.ProfileId;
            old.UserId = document.UserId;
            old.LastModified = DateTime.UtcNow;
            // update content only if required
            if (hasContent) old.Content = document.Content;
            else document.Content = old.Content;

            // update attributes
            if (old.Attributes != null)
            {
                // if updating attrs, merge new with old
                if (hasAttributes)
                {
                    UpdateAttributes(old, document);
                }
                // if not updating attrs, just copy the existing ones
                else
                {
                    if (document.Attributes == null)
                        document.Attributes = new List<Attribute>();

                    foreach (Attribute oldAttr in old.Attributes)
                        document.Attributes.Add(oldAttr);
                }
            }
        }
        // just add a new document, assigning a new ID to it
        else
        {
            document.Id = _nextDocId;
            Interlocked.Increment(ref _nextDocId);
            Documents[document.Id] = document;
        }
    }

    /// <summary>
    /// Delete the document with the specified identifier with all its
    /// related data.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    public void DeleteDocument(int id)
    {
        Documents.TryRemove(id, out IDocument _);
    }

    /// <summary>
    /// Sets the content of the document with the specified identifier.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="content">The content, or null.</param>
    public void SetDocumentContent(int id, string content)
    {
        IDocument? document = GetDocument(id, true);
        if (document != null) document.Content = content;
    }

    /// <summary>
    /// Adds the specified attribute to the specified target.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    /// <param name="unique">if set to <c>true</c>, ensure that the target
    /// has only 1 attribute with the name equal to the attribute being
    /// added, by replacing an existing attribute with the new one.</param>
    /// <param name="target">The target attributes container.</param>
    protected void AddAttribute(Attribute attribute, bool unique,
        IList<Attribute> target)
    {
        if (attribute.Id == 0)
        {
            attribute.Id = _nextAttrId;
            Interlocked.Increment(ref _nextAttrId);
        }

        if (unique)
        {
            foreach (Attribute a in target.Where(a => a.Name == attribute.Name)
                .ToList())
            {
                target.Remove(a);
            }
        }

        target.Add(attribute);
    }

    /// <summary>
    /// Adds the specified attribute.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    /// <param name="targetType">The target type for this attribute.</param>
    /// <param name="unique">If set to <c>true</c>, replace any other
    /// attribute from the same document and with the same type with the
    /// new one.</param>
    /// <exception cref="ArgumentNullException">null attribute</exception>
    public virtual void AddAttribute(Attribute attribute, string targetType,
        bool unique)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        switch (targetType)
        {
            case Document.ATTR_TARGET_ID:
                if (Documents[attribute.TargetId].Attributes == null)
                    Documents[attribute.TargetId].Attributes = new List<Attribute>();

                AddAttribute(attribute, unique,
                    Documents[attribute.TargetId].Attributes!);
                break;
        }
    }

    /// <summary>
    /// Gets the names of the attributes matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>page of names, or all the names when page size is 0.</returns>
    public DataPage<string> GetAttributeNames(AttributeFilter filter)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the content of the profile with the specified ID.
    /// </summary>
    /// <param name="id">The profile identifier.</param>
    /// <param name="noContent">True to retrieve only the profile metadata,
    /// without its content.</param>
    /// <returns>content or null if not found</returns>
    /// <exception cref="T:System.ArgumentNullException">null ID</exception>
    public IProfile? GetProfile(string id, bool noContent = false)
    {
        ArgumentNullException.ThrowIfNull(id);
        IProfile? profile = Profiles.ContainsKey(id) ? Profiles[id] : null;
        if (noContent && profile != null) profile.Content = "";
        return profile;
    }

    private static IQueryable<IProfile> ApplyProfileFilter(
        IQueryable<IProfile> profiles,
        ProfileFilter filter)
    {
        if (!string.IsNullOrEmpty(filter.Prefix))
        {
            profiles = profiles.Where(p => p.Id!.StartsWith(
                filter.Prefix, StringComparison.Ordinal));
        }

        if (!string.IsNullOrEmpty(filter.Id))
        {
            profiles = profiles.Where(p => p.Id!.IndexOf(filter.Id,
                StringComparison.Ordinal) > -1);
        }

        if (!string.IsNullOrEmpty(filter.Type))
        {
            profiles = profiles.Where(p => string.Compare(
                p.Type, filter.Type, true) == 0);
        }

        if (!string.IsNullOrEmpty(filter.UserId))
        {
            profiles = profiles.Where(p => p.UserId == filter.UserId);
        }

        return profiles;
    }

    /// <summary>
    /// Gets the specified page of profiles.
    /// </summary>
    /// <param name="filter">The profiles filter. Set page size to 0
    /// to retrieve all the matching profiles at once.</param>
    /// <param name="noContent">True to retrieve only the profile ID,
    /// without its content.</param>
    /// <returns>The page.</returns>
    /// <exception cref="ArgumentNullException">filter</exception>
    public DataPage<IProfile> GetProfiles(ProfileFilter filter,
        bool noContent = false)
    {
        ArgumentNullException.ThrowIfNull(filter);

        IQueryable<IProfile> profiles = ApplyProfileFilter(
            Profiles.Values.AsQueryable(), filter);
        int total = profiles.Count();

        profiles = profiles.OrderBy(p => p.Id);
        if (filter.PageSize < 1)
        {
            return new DataPage<IProfile>(
                filter.PageNumber, filter.PageSize,
                total,
                profiles.Select(p => noContent
                    ? new Profile { Id = p.Id } : p).ToArray());
        }

        return new DataPage<IProfile>(
            filter.PageNumber, filter.PageSize,
            total,
            profiles.Skip(filter.GetSkipCount())
                    .Take(filter.PageSize)
                    .Select(p => noContent
                        ? new Profile { Id = p.Id } : p)
                    .ToArray());
    }

    /// <summary>
    /// Adds or updates the specified profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <exception cref="ArgumentNullException">profile or profile.Id</exception>
    public void AddProfile(IProfile profile)
    {
        if (profile?.Id == null) throw new ArgumentNullException(nameof(profile));
        Profiles[profile.Id!] = profile;
    }

    /// <summary>
    /// Delete the profile with the specified ID.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <exception cref="ArgumentNullException">id</exception>
    public void DeleteProfile(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        Profiles.TryRemove(id, out IProfile? _);
    }
}
