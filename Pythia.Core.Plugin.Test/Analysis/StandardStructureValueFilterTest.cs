using Fusi.Text.Unicode;
using Pythia.Core.Plugin.Analysis;
using System.Text;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis
{
    public sealed class StandardStructureValueFilterTest
    {
        private static readonly UniData _ud = new UniData();

        [Theory]
        [InlineData("", "")]
        [InlineData("Abc' d1", "abc' d1")]
        [InlineData("Abc! (d1)", "abc d1")]
        [InlineData("\tAbc  d1\n", "abc d1")]
        [InlineData("Città È", "citta e")]
        public void Apply_Ok(string text, string expected)
        {
            StandardStructureValueFilter filter =
                new StandardStructureValueFilter(_ud);
            StringBuilder sb = new StringBuilder(text);

            filter.Apply(sb, null);

            Assert.Equal(expected, sb.ToString());
        }
    }
}
