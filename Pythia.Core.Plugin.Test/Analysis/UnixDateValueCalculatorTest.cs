using Corpus.Core.Analysis;
using Pythia.Core.Plugin.Analysis;
using System;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis
{
    public sealed class UnixDateValueCalculatorTest
    {
        private static IDocDateValueCalculator GetCalculator()
        {
            UnixDateValueCalculator calculator = new UnixDateValueCalculator();
            calculator.Configure(new UnixDateValueCalculatorOptions
            {
                Attribute = "ymd",
                YmdPattern = @"(?<y>\d{4})/(?<m>\d{2})/(?<d>\d{2})"
            });
            return calculator;
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid")]
        [InlineData("127993")]
        public void Calculate_Invalid_0(string value)
        {
            IDocDateValueCalculator calculator = GetCalculator();

            double actual = calculator.Calculate(new Corpus.Core.Attribute[]
            {
                new Corpus.Core.Attribute
                {
                    Name = "ymd",
                    Value = value
                }
            });

            Assert.Equal(0, actual);
        }

        [Theory]
        [InlineData("1970/01/01", 1970, 1, 1)]
        [InlineData("2021/09/02", 2021, 9, 2)]
        public void Calculate_Ymd_Ok(string value, int y, int m, int d)
        {
            IDocDateValueCalculator calculator = GetCalculator();

            double actual = calculator.Calculate(new Corpus.Core.Attribute[]
            {
                new Corpus.Core.Attribute
                {
                    Name = "ymd",
                    Value = value
                }
            });

            DateTimeOffset dto = new DateTimeOffset(new DateTime(y, m, d, 0, 0, 0));
            double expected = dto.ToUnixTimeSeconds();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1970/01", 1970, 1)]
        [InlineData("2021/09", 2021, 9)]
        public void Calculate_Ym_Ok(string value, int y, int m)
        {
            UnixDateValueCalculator calculator = new UnixDateValueCalculator();
            calculator.Configure(new UnixDateValueCalculatorOptions
            {
                Attribute = "ymd",
                YmdPattern = @"(?<y>\d{4})/(?<m>\d{2})"
            });

            double actual = calculator.Calculate(new Corpus.Core.Attribute[]
            {
                new Corpus.Core.Attribute
                {
                    Name = "ymd",
                    Value = value
                }
            });

            DateTimeOffset dto = new DateTimeOffset(new DateTime(y, m, 1, 0, 0, 0));
            double expected = dto.ToUnixTimeSeconds();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1970", 1970)]
        [InlineData("2021", 2021)]
        public void Calculate_Y_Ok(string value, int y)
        {
            UnixDateValueCalculator calculator = new UnixDateValueCalculator();
            calculator.Configure(new UnixDateValueCalculatorOptions
            {
                Attribute = "ymd",
                YmdPattern = @"(?<y>\d{4})(?:/(?<m>\d{2}))?"
            });

            double actual = calculator.Calculate(new Corpus.Core.Attribute[]
            {
                new Corpus.Core.Attribute
                {
                    Name = "ymd",
                    Value = value
                }
            });

            DateTimeOffset dto = new DateTimeOffset(new DateTime(y, 1, 1, 0, 0, 0));
            double expected = dto.ToUnixTimeSeconds();

            Assert.Equal(expected, actual);
        }
    }
}
