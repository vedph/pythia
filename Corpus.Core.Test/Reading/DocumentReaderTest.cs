using Corpus.Core.Reading;
using System.Collections.Immutable;
using Xunit;

namespace Corpus.Core.Test.Reading;

public sealed class DocumentReaderTest
{
    [Fact]
    public void GetMapperAttributes_NoAttributes_Empty()
    {
        Document document = new() { Attributes = null };

        ImmutableDictionary<string, string> attributes =
            DocumentReader.GetMapperAttributes(document);

        Assert.Empty(attributes);
    }

    [Fact]
    public void GetMapperAttributes_DuplicateNames_FirstValue()
    {
        Document document = new();
        document.Attributes!.Add(new Attribute("author", "Dante"));
        document.Attributes.Add(new Attribute("author", "Petrarca"));
        document.Attributes.Add(new Attribute("genre", "poetry"));

        ImmutableDictionary<string, string> attributes =
            DocumentReader.GetMapperAttributes(document);

        Assert.Equal(2, attributes.Count);
        Assert.Equal("Dante", attributes["author"]);
        Assert.Equal("poetry", attributes["genre"]);
    }
}
