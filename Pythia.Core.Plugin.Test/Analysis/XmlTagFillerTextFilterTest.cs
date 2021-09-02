using Pythia.Core.Plugin.Analysis;
using System.IO;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis
{
    public sealed class XmlTagFillerTextFilterTest
    {
        private static XmlTagFillerTextFilter GetFilter()
        {
            XmlTagFillerTextFilter filter = new XmlTagFillerTextFilter();
            filter.Configure(new XmlTagFillerTextFilterOptions
            {
                Tags = new[] { "expan" }
            });
            return filter;
        }

        [Fact]
        public void Apply_Expan_Filled()
        {
            XmlTagFillerTextFilter filter = GetFilter();
            const string xml = "<p>Take <choice><abbr>e.g.</abbr>\n" +
                "<expan>exempli gratia</expan></choice> this:</p>";

            string filtered = filter.Apply(new StringReader(xml)).ReadToEnd();

            Assert.Equal("<p>Take <choice><abbr>e.g.</abbr>\n" +
                "                             </choice> this:</p>", filtered);
        }

        [Fact]
        public void Apply_All_Filled()
        {
            XmlTagFillerTextFilter filter = new XmlTagFillerTextFilter();
            const string xml = "<p>Take <choice><abbr>e.g.</abbr>\n" +
                "<expan>exempli gratia</expan></choice> this:</p>";

            string filtered = filter.Apply(new StringReader(xml)).ReadToEnd();

            Assert.Equal("   Take               e.g.       \n" +
                "       exempli gratia                  this:    ", filtered);
        }
    }
}
