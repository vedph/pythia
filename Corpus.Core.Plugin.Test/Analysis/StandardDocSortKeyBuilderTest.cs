using Corpus.Core.Plugin.Analysis;
using Xunit;

namespace Corpus.Core.Plugin.Test.Analysis;

public sealed class StandardDocSortKeyBuilderTest
{
    [Fact]
    public void Build_SimpleName_Ok()
    {
        StandardDocSortKeyBuilder builder = new();

        string s = builder.Build(new Document
        {
            Author = "Simple Name",
            Title = "Title",
            DateValue = 1320
        });

        Assert.Equal("simplename-title-P1320.00", s);
    }

    [Fact]
    public void Build_SimpleNameWithDiacritics_Ok()
    {
        StandardDocSortKeyBuilder builder = new();

        string s = builder.Build(new Document
        {
            Author = "Fréson",
            Title = "Title",
            DateValue = 1320
        });

        Assert.Equal("freson-title-P1320.00", s);
    }

    [Fact]
    public void Build_LastNameFirstName_Ok()
    {
        StandardDocSortKeyBuilder builder = new();

        string s = builder.Build(new Document
        {
            Author = "Rossi, Luigi Enrico",
            Title = "Title",
            DateValue = 1320
        });

        Assert.Equal("rossi-title-P1320.00", s);
    }
    [Fact]
    public void Build_MultipleNames_Ok()
    {
        StandardDocSortKeyBuilder builder = new();

        string s = builder.Build(new Document
        {
            Author = "Rossi, Luigi Enrico; Verdi; Bianchi, Mario",
            Title = "Title",
            DateValue = 1320
        });

        Assert.Equal("bianchi-title-P1320.00", s);
    }
}
