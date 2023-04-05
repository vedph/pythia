using Fusi.Tools;
using Fusi.Tools.Text;
using Pythia.Core.Plugin.Analysis;
using System.Collections.Generic;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class ItalianTaggedTokenFilterTest
{
    [Fact]
    public void Apply_NonSpecial_Ok()
    {
        ItalianTaggedTokenFilter filter = new();
        Token token = new()
        {
            DocumentId = 1,
            Index = 100,
            Length = 9,
            Position = 10,
            Value = "(Esémpio!",
        };
        DataDictionary data = new();
        data.Data[XmlLocalTagListTextFilter.XML_LOCAL_TAG_LIST_KEY] =
            new List<XmlTagListEntry>
            {
                new XmlTagListEntry("num", new TextRange(90, 2)),
            };

        filter.Apply(token, token.Position, data);

        Assert.Equal("esempio", token.Value);
    }

    [Fact]
    public void Apply_Num_Ok()
    {
        ItalianTaggedTokenFilter filter = new();
        Token token = new()
        {
            DocumentId = 1,
            Index = 100,
            Length = 3,
            Position = 10,
            Value = "12%",
        };
        DataDictionary data = new();
        data.Data[XmlLocalTagListTextFilter.XML_LOCAL_TAG_LIST_KEY] =
            new List<XmlTagListEntry>
            {
                new XmlTagListEntry("num", new TextRange(90, 2)),
                new XmlTagListEntry("x", new TextRange(93, 10)),
                new XmlTagListEntry("num", new TextRange(95, 14))
            };

        filter.Apply(token, token.Position, data);

        Assert.Equal("12%", token.Value);
    }

    [Fact]
    public void Apply_NumWithPunctuations_Ok()
    {
        ItalianTaggedTokenFilter filter = new();
        Token token = new()
        {
            DocumentId = 1,
            Index = 100,
            Length = 5,
            Position = 10,
            Value = "(12%,",
        };
        DataDictionary data = new();
        data.Data[XmlLocalTagListTextFilter.XML_LOCAL_TAG_LIST_KEY] =
            new List<XmlTagListEntry>
            {
                new XmlTagListEntry("num", new TextRange(90, 2)),
                new XmlTagListEntry("x", new TextRange(93, 10)),
                new XmlTagListEntry("num", new TextRange(95, 14))
            };

        filter.Apply(token, token.Position, data);

        Assert.Equal("12%", token.Value);
    }
}
