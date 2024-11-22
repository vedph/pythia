using Corpus.Core;
using Corpus.Core.Test;
using Fusi.DbManager;
using Fusi.DbManager.PgSql;
using Xunit;

#pragma warning disable S2699 // Tests should include assertions

namespace Corpus.Sql.PgSql.Test;

// https://github.com/xunit/xunit/issues/1999

[CollectionDefinition(nameof(NonParallelResourceCollection),
    DisableParallelization = true)]
public class NonParallelResourceCollection { }

[Collection(nameof(NonParallelResourceCollection))]
public sealed class PgSqlCorpusRepositoryTest : CorpusRepositoryTest
{
    private const string CST =
        "User ID=postgres;Password=postgres;Host=localhost;Port=5432;Database={0}";
    private const string DB_NAME = "corpus-test";
    static private readonly string CS = string.Format(CST, DB_NAME);

    private readonly IDbManager _manager;

    public PgSqlCorpusRepositoryTest()
    {
        _manager = new PgSqlDbManager(CST);
    }

    protected override string GetSchema() => GetRepository().GetSchema();

    protected override void Init()
    {
        if (_manager.Exists(DB_NAME)) _manager.ClearDatabase(DB_NAME);
        else _manager.CreateDatabase(DB_NAME, GetSchema(), null);
    }

    protected override ICorpusRepository GetRepository()
    {
        PgSqlCorpusRepository repository = new PgSqlCorpusRepository();
        repository.Configure(new SqlRepositoryOptions
        {
            ConnectionString = CS
        });
        return repository;
    }

    [Fact]
    public void CreateRepository_Ok() => DoCreateRepository_Ok();

    [Fact]
    public void GetCorpora_Empty_Empty() => DoGetCorpora_Empty_Empty();

    [Fact]
    public void GetCorpora_Unfiltered_Ok() => DoGetCorpora_Unfiltered_Ok();

    [Fact]
    public void GetCorpora_FilteredById_Ok() => DoGetCorpora_FilteredById_Ok();

    [Fact]
    public void GetCorpora_FilteredByPrefix_Ok()
        => DoGetCorpora_FilteredByPrefix_Ok();

    [Fact]
    public void GetCorpora_FilteredByIdAndPrefix_Ok()
        => DoGetCorpora_FilteredByIdAndPrefix_Ok();

    [Fact]
    public void AddCorpus_NotExisting_Added()
        => DoAddCorpus_NotExisting_Added();

    [Fact]
    public void AddCorpus_Existing_Updated()
        => DoAddCorpus_Existing_Updated();

    [Fact]
    public void DeleteCorpus_NotExisting_Nope()
        => DoDeleteCorpus_NotExisting_Nope();

    [Fact]
    public void DeleteCorpus_Existing_Deleted()
        => DoDeleteCorpus_Existing_Deleted();

    [Fact]
    public void ChangeCorpusByFilter_AddNotExisting_Ok()
        => DoChangeCorpusByFilter_AddNotExisting_Ok();

    [Fact]
    public void ChangeCorpusByFilter_AddExisting_Ok()
        => DoChangeCorpusByFilter_AddExisting_Ok();

    [Fact]
    public void ChangeCorpusByFilter_RemoveNotExisting_Ok()
        => DoChangeCorpusByFilter_RemoveNotExisting_Ok();

    [Fact]
    public void ChangeCorpusByFilter_RemoveExisting_Ok()
        => DoChangeCorpusByFilter_RemoveExisting_Ok();

    [Fact]
    public void IsDocumentInCorpus_Outside_False()
        => DoIsDocumentInCorpus_Outside_False();

    [Fact]
    public void IsDocumentInCorpus_Inside_True()
        => DoIsDocumentInCorpus_Inside_True();

    [Fact]
    public void GetDocuments_Empty_Empty() => DoGetDocuments_Empty_Empty();

    [Fact]
    public void GetDocuments_Page1Of2_Ok() => DoGetDocuments_Page1Of2_Ok();

    [Theory]
    [InlineData(DocumentSortOrder.Author)]
    [InlineData(DocumentSortOrder.Date)]
    [InlineData(DocumentSortOrder.Title)]
    [InlineData(DocumentSortOrder.Default)]
    public void GetDocuments_CustomSort_Ok(DocumentSortOrder sort) =>
        DoGetDocuments_CustomSort_Ok(sort);

    [Fact]
    public void GetDocuments_Page2Of2_Ok() => DoGetDocuments_Page2Of2_Ok();

    [Fact]
    public void GetDocuments_CorpusId_NotExisting_Ok()
        => DoGetDocuments_CorpusId_NotExisting_Ok();

    [Fact]
    public void GetDocuments_CorpusId_Existing_Ok()
        => DoGetDocuments_CorpusId_Existing_Ok();

    [Fact]
    public void GetDocuments_Author_NotExisting_Ok()
        => DoGetDocuments_Author_NotExisting_Ok();

    [Fact]
    public void GetDocuments_Author_Existing_Ok()
        => DoGetDocuments_Author_Existing_Ok();

    [Fact]
    public void GetDocuments_Title_NotExisting_Ok()
        => DoGetDocuments_Title_NotExisting_Ok();

    [Fact]
    public void GetDocuments_Title_Existing_Ok()
        => DoGetDocuments_Title_Existing_Ok();

    [Fact]
    public void GetDocuments_Source_NotExisting_Ok()
        => DoGetDocuments_Source_NotExisting_Ok();

    [Fact]
    public void GetDocuments_Source_Existing_Ok()
        => DoGetDocuments_Source_Existing_Ok();

    [Fact]
    public void GetDocuments_Attributes_NotExisting_Ok()
        => DoGetDocuments_Attributes_NotExisting_Ok();

    [Fact]
    public void GetDocuments_Attributes_Existing_Ok()
        => DoGetDocuments_Attributes_Existing_Ok();

    [Fact]
    public void GetDocuments_MinDateValue_NotExisting_Ok()
        => DoGetDocuments_MinDateValue_NotExisting_Ok();

    [Fact]
    public void GetDocuments_MinDateValue_Existing_Ok()
        => DoGetDocuments_MinDateValue_Existing_Ok();

    [Fact]
    public void GetDocuments_MaxDateValue_NotExisting_Ok()
        => DoGetDocuments_MaxDateValue_NotExisting_Ok();

    [Fact]
    public void GetDocuments_MaxDateValue_Existing_Ok()
        => DoGetDocuments_MaxDateValue_Existing_Ok();

    [Fact]
    public void GetDocuments_Combined_NotExisting_Ok()
        => DoGetDocuments_Combined_NotExisting_Ok();

    [Fact]
    public void GetDocuments_Combined_Existing_Ok()
        => DoGetDocuments_Combined_Existing_Ok();

    [Fact]
    public void GetDocumentBySource_ExistingNoContent_Ok()
        => DoGetDocumentBySource_ExistingNoContent_Ok();

    [Fact]
    public void GetDocumentBySource_ExistingContent_Ok()
        => DoGetDocumentBySource_ExistingContent_Ok();

    [Fact]
    public void AddDocument_NoAttrNoContent_Ok()
        => DoAddDocument_NoAttrNoContent_Ok();

    [Fact]
    public void AddDocument_NoAttrContent_Ok()
        => DoAddDocument_NoAttrContent_Ok();

    [Fact]
    public void AddDocument_AttrContent_Ok()
        => DoAddDocument_AttrContent_Ok();

    [Fact]
    public void AddDocument_Existing_Updated()
        => DoAddDocument_Existing_Updated();

    [Fact]
    public void AddDocumentsToCorpus_NotExisting_Ok()
        => DoAddDocumentsToCorpus_NotExisting_Ok();

    [Fact]
    public void AddDocumentsToCorpus_SomeExisting_Ok()
        => DoAddDocumentsToCorpus_SomeExisting_Ok();

    [Fact]
    public void GetAttributeNames_Ok() => DoGetAttributeNames_Ok();

    [Fact]
    public void AddAttribute_NotExisting_Ok() => DoAddAttribute_NotExisting_Ok();

    [Fact]
    public void AddAttribute_Existing_Ok() => DoAddAttribute_Existing_Ok();

    [Fact]
    public void GetProfiles_Any_Ok() => DoGetProfiles_Any_Ok();

    [Fact]
    public void GetProfiles_ById_Ok() => DoGetProfiles_ById_Ok();

    [Fact]
    public void GetProfiles_ByPrefix_Ok() => DoGetProfiles_ByPrefix_Ok();

    [Fact]
    public void GetProfile_NotExisting_Null() => DoGetProfile_NotExisting_Null();

    [Fact]
    public void GetProfile_Existing_Ok() => DoGetProfile_Existing_Ok();
}
#pragma warning restore S2699 // Tests should include assertions
