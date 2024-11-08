using System.Collections.Generic;
using Pythia.Core.Query;
using Xunit;

namespace Pythia.Core.Test.Query;

public sealed class DocumentPairTests
{
    [Fact]
    public void GenerateBinPairs_IntegerBins_ShouldGenerateCorrectPairs()
    {
        IList<DocumentPair> pairs = DocumentPair.GenerateBinPairs("test", false,
            true, 0, 10, 5);

        Assert.Equal(5, pairs.Count);
        Assert.Equal("test 0:2", pairs[0].ToString());
        Assert.Equal("test 2:4", pairs[1].ToString());
        Assert.Equal("test 4:6", pairs[2].ToString());
        Assert.Equal("test 6:8", pairs[3].ToString());
        Assert.Equal("test 8:10", pairs[4].ToString());
    }

    [Fact]
    public void GenerateBinPairs_DecimalBins_ShouldGenerateCorrectPairs()
    {
        IList<DocumentPair> pairs = DocumentPair.GenerateBinPairs(
            "test", false, false, 1.2, 10.7, 3);

        Assert.Equal(3, pairs.Count);
        Assert.Equal("test 1.2:4.37", pairs[0].ToString());
        Assert.Equal("test 4.37:7.53", pairs[1].ToString());
        Assert.Equal("test 7.53:10.7", pairs[2].ToString());
    }

    [Fact]
    public void GenerateBinPairs_DecimalBinsWithInts_ShouldGenerateCorrectPairs()
    {
        IList<DocumentPair> pairs = DocumentPair.GenerateBinPairs(
            "test", false, false, 0.0, 10.0, 5);

        Assert.Equal(5, pairs.Count);
        Assert.Equal("test 0:2", pairs[0].ToString());
        Assert.Equal("test 2:4", pairs[1].ToString());
        Assert.Equal("test 4:6", pairs[2].ToString());
        Assert.Equal("test 6:8", pairs[3].ToString());
        Assert.Equal("test 8:10", pairs[4].ToString());
    }
}
