using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Pythia.Core.Plugin.Analysis;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

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

    [Theory]
    // xml: no, ctx: no (equal)
    //                           0123456789-123456
    [InlineData("<a>hello</a>", "Hey <a>hello</a> world", 4, 16)]
    // xml: yes, ctx: no
    //                                          0123456789-123456
    [InlineData("<a xmlns=\"some\">hello</a>", "Hey <a>hello</a> world", 4, 16)]
    // xml: no, ctx: yes
    //                           0123456789-12 34567 89-123456789
    [InlineData("<a>hello</a>", "Hey <a xmlns=\"some\">hello</a> world", 4, 29)]
    // xml: yes, ctx: yes (equal)
    //                                          0123456789-12 34567 89-123456789
    [InlineData("<a xmlns=\"some\">hello</a>", "Hey <a xmlns=\"some\">hello</a> world", 4, 29)]
    // xml: no, ctx: no (not equal)
    [InlineData("<a>hello</a>", "Hey <a>hello!</a> world", 4, -1)]
    // xml: no, ctx: no (not equal)
    [InlineData("<a xmlns=\"x\">hello</a>", "Hey <a xmlns=\"y\">hello</a> world", 4, -1)]
    public void SkipOuterXml_Ok(string xml, string context, int start, int expected)
    {
        int actual = XmlTagFillerTextFilter.SkipOuterXml(xml, context, start);
        Assert.Equal(expected, actual);
    }
}
