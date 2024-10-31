using Pythia.Core.Plugin.Analysis;
using System;
using System.Collections.Generic;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class YearDateValueCalculatorTest
{
    [Fact]
    public void Configure_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        YearDateValueCalculator calculator = new();
        Assert.Throws<ArgumentNullException>(() => calculator.Configure(null!));
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenAttributeIsNotFound()
    {
        YearDateValueCalculator calculator = new();
        calculator.Configure(new YearDateValueCalculatorOptions
        {
            Attribute = "year",
            Pattern = "^(?<y>\\d{4})"
        });
        List<Corpus.Core.Attribute> attributes =
        [
            new Corpus.Core.Attribute { Name = "other", Value = "2023" }
        ];

        double result = calculator.Calculate(attributes);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenPatternDoesNotMatch()
    {
        YearDateValueCalculator calculator = new();
        calculator.Configure(new YearDateValueCalculatorOptions
        {
            Attribute = "year",
            Pattern = "^(?<y>\\d{4})"
        });
        List<Corpus.Core.Attribute> attributes =
        [
            new Corpus.Core.Attribute { Name = "year", Value = "abc" }
        ];

        double result = calculator.Calculate(attributes);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Calculate_ShouldReturnYear_WhenPatternMatches()
    {
        YearDateValueCalculator calculator = new();
        calculator.Configure(new YearDateValueCalculatorOptions
        {
            Attribute = "year",
            Pattern = "^(?<y>\\d{4})"
        });
        List<Corpus.Core.Attribute> attributes =
        [
            new Corpus.Core.Attribute { Name = "year", Value = "2023" }
        ];

        double result = calculator.Calculate(attributes);

        Assert.Equal(2023, result);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenYearIsInvalid()
    {
        YearDateValueCalculator calculator = new();
        calculator.Configure(new YearDateValueCalculatorOptions
        {
            Attribute = "year",
            Pattern = "^(?<y>\\d{4})"
        });
        List<Corpus.Core.Attribute> attributes =
        [
            new Corpus.Core.Attribute { Name = "year", Value = "abcd" }
        ];

        double result = calculator.Calculate(attributes);

        Assert.Equal(0, result);
    }
}
