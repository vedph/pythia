using Fusi.Tools;
using Fusi.Tools.Text;
using Pythia.Core.Plugin.Analysis;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class ItalianTaggedTokenFilterTest
{
    [Fact]
    public async Task Apply_NonSpecial_Ok()
    {
        ItalianTaggedTokenFilter filter = new();
        TextSpan token = new()
        {
            DocumentId = 1,
            Index = 100,
            Length = 9,
            P1 = 10,
            P2 = 10,
            Value = "(Esémpio!",
        };
        DataDictionary data = new();
        data.Data[XmlLocalTagListTextFilter.XML_LOCAL_TAG_LIST_KEY] =
            new List<XmlTagListEntry>
            {
                new("num", new TextRange(90, 2)),
            };

        await filter.ApplyAsync(token, token.P1, data);

        Assert.Equal("esempio", token.Value);
    }

    [Fact]
    public async Task Apply_Num_Ok()
    {
        ItalianTaggedTokenFilter filter = new();
        TextSpan token = new()
        {
            DocumentId = 1,
            Index = 100,
            Length = 3,
            P1 = 10,
            P2 = 10,
            Value = "12%",
        };
        DataDictionary data = new();
        data.Data[XmlLocalTagListTextFilter.XML_LOCAL_TAG_LIST_KEY] =
            new List<XmlTagListEntry>
            {
                new("num", new TextRange(90, 2)),
                new("x", new TextRange(93, 10)),
                new("num", new TextRange(95, 14))
            };

        await filter.ApplyAsync(token, token.P1, data);

        Assert.Equal("12%", token.Value);
    }

    [Fact]
    public async Task Apply_NumWithPunctuations_Ok()
    {
        ItalianTaggedTokenFilter filter = new();
        TextSpan token = new()
        {
            DocumentId = 1,
            Index = 100,
            Length = 5,
            P1 = 10,
            P2 = 10,
            Value = "(12%,",
        };
        DataDictionary data = new();
        data.Data[XmlLocalTagListTextFilter.XML_LOCAL_TAG_LIST_KEY] =
            new List<XmlTagListEntry>
            {
                new("num", new TextRange(90, 2)),
                new("x", new TextRange(93, 10)),
                new("num", new TextRange(95, 14))
            };

        await filter.ApplyAsync(token, token.P1, data);

        Assert.Equal("12%", token.Value);
    }
}
