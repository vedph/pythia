using Fusi.Tools;
using Pythia.Core.Plugin.Analysis;
using Xunit;

namespace Pythia.Udp.Plugin.Test;

public sealed class UdpChunkBuilderTest
{
    [Fact]
    public void Build_Empty_NoChunks()
    {
        UdpChunkBuilder builder = new();

        IList<UdpChunk> chunks = builder.Build("");

        Assert.Empty(chunks);
    }

    [Fact]
    public void Build_LessThanMax_Single()
    {
        UdpChunkBuilder builder = new()
        {
            MaxLength = 50
        };

        IList<UdpChunk> chunks = builder.Build("Hello, world!");

        Assert.Single(chunks);
        UdpChunk chunk = chunks[0];
        Assert.Equal(0, chunk.Range.Start);
        Assert.Equal(13, chunk.Range.Length);
        Assert.False(chunk.IsOversized);
        Assert.False(chunk.HasNoAlpha);
    }

    [Fact]
    public void Build_EqualToMax_Single()
    {
        UdpChunkBuilder builder = new()
        {
            MaxLength = 13
        };

        IList<UdpChunk> chunks = builder.Build("Hello, world!");

        Assert.Single(chunks);
        UdpChunk chunk = chunks[0];
        Assert.Equal(0, chunk.Range.Start);
        Assert.Equal(13, chunk.Range.Length);
        Assert.False(chunk.IsOversized);
        Assert.False(chunk.HasNoAlpha);
    }

    [Fact]
    public void Build_WithNonAlpha_Ok()
    {
        UdpChunkBuilder builder = new()
        {
            MaxLength = 10
        };

        IList<UdpChunk> chunks = builder.Build("Hi, world!      ");

        Assert.Single(chunks);
        UdpChunk chunk = chunks[0];
        Assert.Equal(0, chunk.Range.Start);
        Assert.Equal(10, chunk.Range.Length);
        Assert.False(chunk.IsOversized);
        Assert.False(chunk.HasNoAlpha);
    }

    [Fact]
    public void Build_VariousLengths_Ok()
    {
        UdpChunkBuilder builder = new()
        {
            MaxLength = 10
        };

        IList<UdpChunk> chunks = builder.Build(
          // 0         10        20        30        40
          // 0123456789-123456789-123456789-123456789-12345678
            "Hi, world! This is an oversize chunk. Third. Last");

        Assert.Equal(4, chunks.Count);
        // [0]
        UdpChunk chunk = chunks[0];
        Assert.Equal(0, chunk.Range.Start);
        Assert.Equal(10, chunk.Range.Length);
        Assert.False(chunk.IsOversized);
        Assert.False(chunk.HasNoAlpha);
        // [1]
        chunk = chunks[1];
        Assert.Equal(11, chunk.Range.Start);
        Assert.Equal(26, chunk.Range.Length);
        Assert.True(chunk.IsOversized);
        Assert.False(chunk.HasNoAlpha);
        // [2]
        chunk = chunks[2];
        Assert.Equal(38, chunk.Range.Start);
        Assert.Equal(6, chunk.Range.Length);
        Assert.False(chunk.IsOversized);
        Assert.False(chunk.HasNoAlpha);
        // [3]
        chunk = chunks[3];
        Assert.Equal(45, chunk.Range.Start);
        Assert.Equal(4, chunk.Range.Length);
        Assert.False(chunk.IsOversized);
        Assert.False(chunk.HasNoAlpha);
    }

    [Fact]
    public async Task Build_InBlack_Single()
    {
        const string text = "Hello <abbr id=\"x\">h.m.s.</abbr> world!";
        UdpChunkBuilder builder = new()
        {
            MaxLength = 50,
            BlackTags = new HashSet<string>(new[]
            {
                "abbr"
            })
        };

        XmlLocalTagListTextFilter filter = new();
        DataDictionary d = new();
        filter.Configure(new XmlLocalTagListTextFilterOptions
        {
            Names = new HashSet<string> { "abbr" }
        });
        await filter.ApplyAsync(new StringReader(text), d);

        IList<UdpChunk> chunks = builder.Build(text,
            (IList<XmlTagListEntry>)d.Data
            [XmlLocalTagListTextFilter.XML_LOCAL_TAG_LIST_KEY]!);

        Assert.Single(chunks);
        UdpChunk chunk = chunks[0];
        Assert.Equal(0, chunk.Range.Start);
        Assert.Equal(39, chunk.Range.Length);
        Assert.False(chunk.IsOversized);
        Assert.False(chunk.HasNoAlpha);
    }
}
