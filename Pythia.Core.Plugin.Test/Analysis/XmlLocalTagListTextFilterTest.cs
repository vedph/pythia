using Fusi.Tools;
using Pythia.Core.Plugin.Analysis;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class XmlLocalTagListTextFilterTest
{
    [Fact]
    public async Task Apply_Ok()
    {
        //                   0123456789-12345
        const string text = "<root><p>Hello, " +
          //     20         30         40        50        60        70
          // 6789-123456 789-1 23456789-123456789-123456789-123456780-123456
            "<span rend=\"bold\">my</span> world!</p><br/><p>End.</p></root>";

        DataDictionary context = new();
        XmlLocalTagListTextFilter filter = new();

        filter.Configure(new XmlLocalTagListTextFilterOptions
        {
            Names = new HashSet<string> { "p", "span", "br" }
        });

        string result = (await filter.ApplyAsync(
            new StringReader(text), context)).ReadToEnd();

        Assert.Equal(text, result);
        Assert.True(context.Data.ContainsKey(
            XmlLocalTagListTextFilter.XML_LOCAL_TAG_LIST_KEY));

        IList<XmlTagListEntry> entries = (IList<XmlTagListEntry>)
            context.Data[XmlLocalTagListTextFilter.XML_LOCAL_TAG_LIST_KEY];

        Assert.Equal(4, entries.Count);
        // p
        XmlTagListEntry entry = entries[0];
        Assert.Equal("p", entry.Name);
        Assert.Equal(6, entry.Range.Start);
        Assert.Equal(48, entry.Range.Length);
        // span
        entry = entries[1];
        Assert.Equal("span", entry.Name);
        Assert.Equal(16, entry.Range.Start);
        Assert.Equal(27, entry.Range.Length);
        // br
        entry = entries[2];
        Assert.Equal("br", entry.Name);
        Assert.Equal(54, entry.Range.Start);
        Assert.Equal(5, entry.Range.Length);
        // p
        entry = entries[3];
        Assert.Equal("p", entry.Name);
        Assert.Equal(59, entry.Range.Start);
        Assert.Equal(11, entry.Range.Length);
    }
}
