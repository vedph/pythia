using System.IO;
using Pythia.Core.Analysis;
using Xunit;

namespace Pythia.Core.Test.Analysis
{
    public sealed class CharIndexCalculatorTest
    {
        [Fact]
        public void GetIndex_NoCrLf_Ok()
        {
            CharIndexCalculator calculator = new(new StringReader("abc"));

            int i = calculator.GetIndex(1, 1);

            Assert.Equal(0, i);
        }

        [Fact]
        public void GetIndex_CrOnly_Ok()
        {
            CharIndexCalculator calculator = new(new StringReader("abc\rde"));

            int i = calculator.GetIndex(2, 1);

            Assert.Equal(4, i);
        }

        [Fact]
        public void GetIndex_LfOnly_Ok()
        {
            CharIndexCalculator calculator = new(new StringReader("abc\nde"));

            int i = calculator.GetIndex(2, 1);

            Assert.Equal(4, i);
        }

        [Fact]
        public void GetIndex_CrLf_Ok()
        {
            CharIndexCalculator calculator = new(new StringReader("abc\r\nde"));

            int i = calculator.GetIndex(2, 1);

            Assert.Equal(5, i);
        }
    }
}
