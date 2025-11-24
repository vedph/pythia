using Fusi.Tools.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Corpus.Core.Test;

/// <summary>
/// Base class for corpus repositories tests.
/// </summary>
public abstract class CorpusRepositoryTest
{
    protected abstract string GetSchema();

    protected abstract void Init();

    protected abstract ICorpusRepository GetRepository();

    protected void DoCreateRepository_Ok()
    {
        Assert.NotNull(GetRepository());
    }

    protected static void AssertDocumentDataEqual(IDocument expected,
        IDocument actual)
    {
        Assert.Equal(expected.Author, actual.Author);
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.DateValue, actual.DateValue);
        Assert.Equal(expected.SortKey, actual.SortKey);
        Assert.Equal(expected.Source, actual.Source);
        Assert.Equal(expected.ProfileId, actual.ProfileId);
        Assert.Equal(expected.Content, actual.Content);
    }

    protected static void SeedProfiles(ICorpusRepository repository)
    {
        repository.AddProfile(new Profile
        {
            Id = "fake",
            Content = "f"
        });
        repository.AddProfile(new Profile
        {
            Id = "alpha",
            Content = "a"
        });
        repository.AddProfile(new Profile
        {
            Id = "beta",
            Content = "b"
        });
    }

    protected static void SeedDocuments(int count, ICorpusRepository repository)
    {
        for (int n = 1; n <= count; n++)
        {
            bool even = n % 2 == 0;
            IDocument document = new Document
            {
                Author = even ? "Even" : "Odd",
                Title = $"Document {n}",
                DateValue = 1900 + n,
                SortKey = $"tester-testdocument-{1900 + n}",
                Source = $"z:\\corpus\\test{n}.txt",
                ProfileId = "fake",
                Content = $"content {n}"
            };
            document.Attributes!.Add(new Attribute
            {
                Name = "date-value",
                Value = $"{1900 + n}",
                Type = AttributeType.Number
            });
            repository.AddDocument(document, true, true);
        }
    }

    #region Corpus
    protected void DoGetCorpora_Empty_Empty()
    {
        Init();
        ICorpusRepository repository = GetRepository();

        DataPage<ICorpus> page =
            repository.GetCorpora(new CorpusFilter(), false);
        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    protected void DoGetCorpora_Unfiltered_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        repository.AddCorpus(new Corpus
        {
            Id = "c1",
            Title = "Corpus 1",
            Description = "The first corpus."
        });

        DataPage<ICorpus> page =
            repository.GetCorpora(new CorpusFilter(), false);
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal("c1", page.Items[0].Id);
    }

    protected void DoGetCorpora_FilteredById_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        repository.AddCorpus(new Corpus
        {
            Id = "alpha",
            Title = "Corpus A",
            Description = "The alpha corpus."
        });
        repository.AddCorpus(new Corpus
        {
            Id = "beta",
            Title = "Corpus B",
            Description = "The beta corpus."
        });

        DataPage<ICorpus> page =
            repository.GetCorpora(new CorpusFilter
            {
                Id = "h"
            }, false);
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal("alpha", page.Items[0].Id);
    }

    protected void DoGetCorpora_FilteredByPrefix_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        repository.AddCorpus(new Corpus
        {
            Id = "alpha",
            Title = "Corpus A",
            Description = "The alpha corpus."
        });
        repository.AddCorpus(new Corpus
        {
            Id = "beta",
            Title = "Corpus B",
            Description = "The beta corpus."
        });

        DataPage<ICorpus> page =
            repository.GetCorpora(new CorpusFilter
            {
                Prefix = "a"
            }, false);
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal("alpha", page.Items[0].Id);
    }

    protected void DoGetCorpora_FilteredByIdAndPrefix_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        repository.AddCorpus(new Corpus
        {
            Id = "alpha",
            Title = "Corpus A",
            Description = "The alpha corpus."
        });
        repository.AddCorpus(new Corpus
        {
            Id = "beta",
            Title = "Corpus B",
            Description = "The beta corpus."
        });

        DataPage<ICorpus> page =
            repository.GetCorpora(new CorpusFilter
            {
                Id = "a",
                Prefix = "a"
            }, false);
        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal("alpha", page.Items[0].Id);
    }

    protected void DoAddCorpus_NotExisting_Added()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        Corpus corpus = new()
        {
            Id = "alpha",
            Title = "Corpus A",
            Description = "The alpha corpus."
        };
        repository.AddCorpus(corpus);

        ICorpus? corpus2 = repository.GetCorpus("alpha");
        Assert.NotNull(corpus2);
        Assert.Equal(corpus.Id, corpus2!.Id);
        Assert.Equal(corpus.Title, corpus2!.Title);
        Assert.Equal(corpus.Description, corpus2!.Description);
    }

    protected void DoAddCorpus_Existing_Updated()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        Corpus corpus = new()
        {
            Id = "alpha",
            Title = "Corpus A",
            Description = "The alpha corpus."
        };
        repository.AddCorpus(corpus);
        corpus.Title = "Corpus alpha";

        repository.AddCorpus(corpus);

        ICorpus? corpus2 = repository.GetCorpus("alpha");
        Assert.NotNull(corpus2);
        Assert.Equal(corpus.Id, corpus2!.Id);
        Assert.Equal(corpus.Title, corpus2!.Title);
        Assert.Equal(corpus.Description, corpus2!.Description);
    }

    protected void DoDeleteCorpus_NotExisting_Nope()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        Corpus corpus = new()
        {
            Id = "alpha",
            Title = "Corpus A",
            Description = "The alpha corpus."
        };
        repository.AddCorpus(corpus);

        repository.DeleteCorpus("not-existing");

        ICorpus? corpus2 = repository.GetCorpus(corpus.Id);
        Assert.NotNull(corpus2);
        Assert.Equal(corpus.Id, corpus2!.Id);
        Assert.Equal(corpus.Title, corpus2!.Title);
        Assert.Equal(corpus.Description, corpus2!.Description);
    }

    protected void DoDeleteCorpus_Existing_Deleted()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        Corpus corpus = new()
        {
            Id = "alpha",
            Title = "Corpus A",
            Description = "The alpha corpus."
        };
        repository.DeleteCorpus(corpus.Id);

        Assert.Null(repository.GetCorpus("alpha"));
    }

    protected void DoIsDocumentInCorpus_Outside_False()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(2, repository);
        repository.AddCorpus(new Corpus
        {
            Id = "alpha",
            Title = "Corpus A",
            Description = "The alpha corpus."
        });

        Assert.False(repository.IsDocumentInCorpus(123, "alpha", false));
        Assert.False(repository.IsDocumentInCorpus(123, "alpha", true));
        Assert.False(repository.IsDocumentInCorpus(1, "beta", false));
        Assert.False(repository.IsDocumentInCorpus(1, "beta", true));
    }

    protected void DoIsDocumentInCorpus_Inside_True()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(2, repository);
        repository.AddCorpus(new Corpus
        {
            Id = "alpha",
            Title = "Corpus A",
            Description = "The alpha corpus."
        });
        repository.AddDocumentsToCorpus("alpha", null, 1);

        Assert.True(repository.IsDocumentInCorpus(1, "alpha", false));
        Assert.True(repository.IsDocumentInCorpus(1, "alpha", true));
        Assert.True(repository.IsDocumentInCorpus(1, "al", true));
    }

    protected void DoChangeCorpusByFilter_AddNotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        repository.ChangeCorpusByFilter("test", "zeus", new DocumentFilter
        {
            Author = "Odd"
        }, true);

        DataPage<ICorpus> page =
            repository.GetCorpora(new CorpusFilter(), true);

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(2, page.Items[0].DocumentIds!.Count);
        Assert.True(page.Items[0].DocumentIds!.Contains(1));
        Assert.True(page.Items[0].DocumentIds!.Contains(3));
    }

    protected void DoChangeCorpusByFilter_AddExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        repository.ChangeCorpusByFilter("test", "zeus", new DocumentFilter
        {
            Author = "Odd"
        }, true);
        repository.ChangeCorpusByFilter("test", "zeus", new DocumentFilter
        {
            Author = "Even"
        }, true);

        DataPage<ICorpus> page =
            repository.GetCorpora(new CorpusFilter(), true);

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(3, page.Items[0].DocumentIds!.Count);
        Assert.True(page.Items[0].DocumentIds!.Contains(1));
        Assert.True(page.Items[0].DocumentIds!.Contains(2));
        Assert.True(page.Items[0].DocumentIds!.Contains(3));
    }

    protected void DoChangeCorpusByFilter_RemoveNotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        repository.ChangeCorpusByFilter("test", "zeus", new DocumentFilter
        {
            Title = "Document"
        }, true);
        repository.ChangeCorpusByFilter("test", "zeus", new DocumentFilter
        {
            Author = "X"
        }, false);

        DataPage<ICorpus> page =
            repository.GetCorpora(new CorpusFilter(), true);

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(3, page.Items[0].DocumentIds!.Count);
        Assert.True(page.Items[0].DocumentIds!.Contains(1));
        Assert.True(page.Items[0].DocumentIds!.Contains(2));
        Assert.True(page.Items[0].DocumentIds!.Contains(3));
    }

    protected void DoChangeCorpusByFilter_RemoveExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        repository.ChangeCorpusByFilter("test", "zeus", new DocumentFilter
        {
            Title = "Document"
        }, true);
        repository.ChangeCorpusByFilter("test", "zeus", new DocumentFilter
        {
            Author = "Even"
        }, false);

        DataPage<ICorpus> page =
            repository.GetCorpora(new CorpusFilter(), true);

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(2, page.Items[0].DocumentIds!.Count);
        Assert.True(page.Items[0].DocumentIds!.Contains(1));
        Assert.True(page.Items[0].DocumentIds!.Contains(3));
    }
    #endregion

    #region AddDocument
    private static IDocument GetSingleDocument()
    {
        IDocument document = new Document
        {
            Author = "Homer",
            Title = "Ilias",
            DateValue = -750,
            SortKey = "tester-testdocument--750",
            Source = "texts/homer/ilias.txt",
            ProfileId = "fake",
            Content = "Menin aeide..."
        };
        document.Attributes!.Add(new Attribute
        {
            Name = "date-value",
            Value = "-750",
            Type = AttributeType.Number
        });
        return document;
    }

    protected void DoAddDocument_NoAttrNoContent_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);

        IDocument document = GetSingleDocument();
        document.Content = null;
        repository.AddDocument(document, false, false);

        IDocument? document2 = repository.GetDocument(document.Id, true);
        Assert.NotNull(document2);
        Assert.Equal(document.Author, document2!.Author);
        Assert.Equal(document.Title, document2.Title);
        Assert.Equal(document.DateValue, document2.DateValue);
        Assert.Equal(document.SortKey, document2.SortKey);
        Assert.Equal(document.Source, document2.Source);
        Assert.Equal(document.ProfileId, document2.ProfileId);
        Assert.Null(document2.Content);
        Assert.Empty(document2.Attributes!);
    }

    protected void DoAddDocument_NoAttrContent_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);

        IDocument document = GetSingleDocument();
        repository.AddDocument(document, true, false);

        IDocument? document2 = repository.GetDocument(document.Id, true);
        Assert.NotNull(document2);
        AssertDocumentDataEqual(document, document2!);
        Assert.Empty(document2.Attributes!);
    }

    protected void DoAddDocument_AttrContent_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);

        IDocument document = GetSingleDocument();
        repository.AddDocument(document, true, true);

        IDocument? document2 = repository.GetDocument(document.Id, true);
        Assert.NotNull(document2);
        AssertDocumentDataEqual(document, document2!);
        Assert.Equal(document.Attributes!.Count, document2!.Attributes!.Count);
    }
    #endregion

    #region GetDocuments
    protected void DoGetDocuments_Empty_Empty()
    {
        Init();
        ICorpusRepository repository = GetRepository();

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 20
        });

        Assert.Equal(0, page.Total);
    }

    protected void DoGetDocuments_Page1Of2_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2
        });

        Assert.Equal(3, page.Total);
        Assert.Equal(2, page.Items.Count);
        foreach (IDocument document in page.Items)
            Assert.Single(document.Attributes!);
    }

    protected void DoGetDocuments_CustomSort_Ok(DocumentSortOrder sort)
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            SortOrder = sort
        });

        Assert.Equal(3, page.Total);
        Assert.Equal(2, page.Items.Count);
        foreach (IDocument document in page.Items)
            Assert.Single(document.Attributes!);
    }

    protected void DoGetDocuments_Page2Of2_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 2,
            PageSize = 2
        });

        Assert.Equal(3, page.Total);
        Assert.Single(page.Items);
        foreach (IDocument document in page.Items)
            Assert.Single(document.Attributes!);
    }

    protected void DoGetDocuments_CorpusId_NotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            CorpusId = "not-existing"
        });

        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    protected void DoGetDocuments_CorpusId_Existing_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        repository.AddCorpus(new Corpus
        {
            Id = "test",
            Title = "test",
            Description = "A test corpus",
            DocumentIds = new[] { 1 }
        }, null);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            CorpusId = "test"
        });

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(1, page.Items[0].Id);
    }

    protected void DoGetDocuments_Author_NotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            Author = "not-existing"
        });

        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    protected void DoGetDocuments_Author_Existing_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            Author = "Odd"
        });

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        foreach (IDocument document in page.Items)
            Assert.Single(document.Attributes!);
    }

    protected void DoGetDocuments_Title_NotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            Title = "not-existing"
        });

        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    protected void DoGetDocuments_Title_Existing_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            Title = " 1"
        });

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(1, page.Items[0].Id);
    }

    protected void DoGetDocuments_Source_NotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            Source = "not-existing"
        });

        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    protected void DoGetDocuments_Source_Existing_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            Source = "test1"
        });

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(1, page.Items[0].Id);
    }

    protected void DoGetDocuments_ProfileId_NotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            ProfileId = "not-existing"
        });

        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    protected void DoGetDocuments_ProfileId_Existing_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            ProfileId = "fake"
        });

        Assert.Equal(3, page.Total);
        Assert.Equal(3, page.Items.Count);
    }

    protected void DoGetDocuments_Attributes_NotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            Attributes = new List<Tuple<string, string>>(new[]
            {
                Tuple.Create("not-existing", "hello")
            })
        });

        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    protected void DoGetDocuments_Attributes_Existing_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            Attributes = new List<Tuple<string, string>>(new[]
            {
                Tuple.Create("date-value", "1901")
            })
        });

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(1, page.Items[0].Id);
    }

    protected void DoGetDocuments_MinDateValue_NotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            MinDateValue = 2000
        });

        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    protected void DoGetDocuments_MinDateValue_Existing_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            MinDateValue = 1902
        });

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.Items[0].Id);
        Assert.Equal(3, page.Items[1].Id);
    }

    protected void DoGetDocuments_MaxDateValue_NotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            MaxDateValue = 1800
        });

        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    protected void DoGetDocuments_MaxDateValue_Existing_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            MaxDateValue = 1902
        });

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(1, page.Items[0].Id);
        Assert.Equal(2, page.Items[1].Id);
    }

    // TODO GetDocuments with other filters

    protected void DoGetDocuments_Combined_NotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            Author = "Odd",
            MinDateValue = 1902,
            Title = "not-existing",
            Source = "test"
        });

        Assert.Equal(0, page.Total);
        Assert.Empty(page.Items);
    }

    protected void DoGetDocuments_Combined_Existing_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            PageNumber = 1,
            PageSize = 2,
            Author = "Odd",
            MinDateValue = 1902,
            Title = "Document",
            Source = "test"
        });

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(3, page.Items[0].Id);
    }
    #endregion

    #region AddDocument
    protected void DoAddDocument_NotExisting_Added()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);

        Document document = new()
        {
            Author = "Tester",
            Title = "Test document",
            DateValue = 1900,
            SortKey = "tester-testdocument-1900",
            Source = @"z:\corpus\test.txt",
            ProfileId = "fake",
            Content = "content"
        };
        repository.AddDocument(document, true, true);

        IDocument? document2 = repository.GetDocument(document.Id, true);

        Assert.NotNull(document2);
        AssertDocumentDataEqual(document, document2!);
    }

    protected void DoAddDocument_Existing_Updated()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);

        Document document = new()
        {
            Author = "Tester",
            Title = "Test document",
            DateValue = 1900,
            SortKey = "tester-testdocument-1900",
            Source = @"z:\corpus\test.txt",
            ProfileId = "fake",
            Content = "content"
        };
        document.Attributes!.Add(new Attribute
        {
            Name = "date",
            Value = "II AD"
        });
        document.Attributes!.Add(new Attribute
        {
            Name = "fake",
            Value = "killme"
        });
        repository.AddDocument(document, true, true);

        // update
        document.Content = "another content";
        document.Attributes[0].Value = "I AD";
        document.Attributes.RemoveAt(1);
        document.Attributes!.Add(new Attribute
        {
            Name = "date-value",
            Value = "94"
        });
        repository.AddDocument(document, true, true);

        IDocument? document2 = repository.GetDocument(document.Id, true);

        Assert.NotNull(document2);
        AssertDocumentDataEqual(document, document2!);
    }
    #endregion

    #region GetDocumentBySource
    public void DoGetDocumentBySource_NotExisting_Null()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);

        IDocument? document =
            repository.GetDocumentBySource("not-existing", false);
        Assert.Null(document);
    }

    public void DoGetDocumentBySource_ExistingNoContent_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        IDocument? document =
            repository.GetDocumentBySource(@"z:\corpus\test1.txt", false);
        Assert.NotNull(document);
        Assert.Equal(1, document!.Id);
        Assert.Null(document.Content);
    }

    public void DoGetDocumentBySource_ExistingContent_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        IDocument? document =
            repository.GetDocumentBySource(@"z:\corpus\test1.txt", true);
        Assert.NotNull(document);
        Assert.Equal(1, document!.Id);
        Assert.NotNull(document.Content);
    }
    #endregion

    #region AddDocumentsToCorpus
    public void DoAddDocumentsToCorpus_NotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(2, repository);

        repository.AddDocumentsToCorpus("test-corpus", null, 1);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            CorpusId = "test-corpus"
        });
        Assert.Equal(1, page.Total);
    }

    public void DoAddDocumentsToCorpus_SomeExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(2, repository);

        repository.AddDocumentsToCorpus("test-corpus", null, 1);
        repository.AddDocumentsToCorpus("test-corpus", null, 1, 2);

        DataPage<IDocument> page = repository.GetDocuments(new DocumentFilter
        {
            CorpusId = "test-corpus"
        });
        Assert.Equal(2, page.Total);
    }
    #endregion

    #region GetAttributeNames
    public void DoGetAttributeNames_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(3, repository);

        DataPage<string> page = repository.GetAttributeNames(new AttributeFilter
        {
            PageNumber = 1,
            PageSize = 0,
            Target = "document_attribute"
        });

        Assert.Equal(1, page.Total);
        Assert.Single(page.Items);
        Assert.Equal("date-value", page.Items[0]);
    }
    #endregion

    #region AddAttribute
    public void DoAddAttribute_NotExisting_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(1, repository);

        Attribute attr = new()
        {
            Name = "test",
            Value = "123",
            TargetId = 1,
            Type = AttributeType.Number
        };
        repository.AddAttribute(attr, "document", false);

        IDocument? doc = repository.GetDocument(1, false);
        Assert.NotNull(doc);
        Assert.Equal(2, doc!.Attributes!.Count);

        Attribute? attr2 = doc.Attributes.FirstOrDefault(a => a.Name == "test");
        Assert.NotNull(attr2);
        Assert.Equal(attr.Name, attr2!.Name);
        Assert.Equal(attr.Value, attr2.Value);
        Assert.Equal(attr.TargetId, attr2.TargetId);
        Assert.Equal(attr.Type, attr2.Type);
    }

    public void DoAddAttribute_Existing_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);
        SeedDocuments(1, repository);

        Attribute attr = new()
        {
            Name = "test",
            Value = "123",
            TargetId = 1,
            Type = AttributeType.Number
        };
        repository.AddAttribute(attr, "document", false);

        attr.Value = "456";
        repository.AddAttribute(attr, "document", false);

        IDocument? doc = repository.GetDocument(1, false);
        Assert.NotNull(doc);
        Assert.Equal(2, doc!.Attributes!.Count);

        Attribute? attr2 = doc.Attributes.FirstOrDefault(a => a.Name == "test");
        Assert.NotNull(attr2);
        Assert.Equal(attr.Name, attr2!.Name);
        Assert.Equal("456", attr2.Value);
        Assert.Equal(attr.TargetId, attr2.TargetId);
        Assert.Equal(attr.Type, attr2.Type);
    }
    #endregion

    #region GetProfiles
    public void DoGetProfiles_Any_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);

        DataPage<IProfile> page = repository.GetProfiles(new ProfileFilter());

        Assert.Equal(3, page.Total);
        Assert.Equal("alpha", page.Items[0].Id);
        Assert.Equal("beta", page.Items[1].Id);
        Assert.Equal("fake", page.Items[2].Id);
    }

    public void DoGetProfiles_ById_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);

        DataPage<IProfile> page = repository.GetProfiles(new ProfileFilter
        {
            Id = "ph"
        });

        Assert.Equal(1, page.Total);
        Assert.Equal("alpha", page.Items[0].Id);
    }

    public void DoGetProfiles_ByPrefix_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);

        DataPage<IProfile> page = repository.GetProfiles(new ProfileFilter
        {
            Prefix = "al"
        });

        Assert.Equal(1, page.Total);
        Assert.Equal("alpha", page.Items[0].Id);
    }
    #endregion

    #region GetProfile
    public void DoGetProfile_NotExisting_Null()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);

        IProfile? profile = repository.GetProfile("not-existing");

        Assert.Null(profile);
    }

    public void DoGetProfile_Existing_Ok()
    {
        Init();
        ICorpusRepository repository = GetRepository();
        SeedProfiles(repository);

        IProfile? profile = repository.GetProfile("beta");

        Assert.NotNull(profile);
        Assert.Equal("b", profile!.Content);
    }
    #endregion

    // TODO other tests
}
