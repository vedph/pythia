using Pythia.Core.Plugin.Analysis;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class XmlTagFillerTextFilterTest
{
    private static XmlTagFillerTextFilter GetFilter()
    {
        XmlTagFillerTextFilter filter = new();
        filter.Configure(new XmlTagFillerTextFilterOptions
        {
            Tags = new[] { "expan" }
        });
        return filter;
    }

    [Fact]
    public async Task Apply_Expan_Filled()
    {
        XmlTagFillerTextFilter filter = GetFilter();
        const string xml = "<p>Take <choice><abbr>e.g.</abbr>\n" +
            "<expan>exempli gratia</expan></choice> this:</p>";

        TextReader result = await filter.ApplyAsync(new StringReader(xml));
        string filtered = result.ReadToEnd();

        Assert.Equal("<p>Take <choice><abbr>e.g.</abbr>\n" +
            "                             </choice> this:</p>", filtered);
    }

    [Fact]
    public async Task Apply_All_Filled()
    {
        XmlTagFillerTextFilter filter = new();
        const string xml = "<p>Take <choice><abbr>e.g.</abbr>\n" +
            "<expan>exempli gratia</expan></choice> this:</p>";

        TextReader result = await filter.ApplyAsync(new StringReader(xml));
        string filtered = result.ReadToEnd();

        Assert.Equal("   Take               e.g.       \n" +
            "       exempli gratia                  this:    ", filtered);
    }
}
