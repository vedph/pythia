using Pythia.Core.Plugin.Analysis;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public sealed class XmlEntityResolverTextFilterTest
{
    private static XmlEntityResolverTextFilter GetFilter(bool padding = false)
    {
        XmlEntityResolverTextFilter filter = new();
        filter.Configure(new XmlEntityResolverTextFilterOptions
        {
            IsPaddingEnabled = padding
        });
        return filter;
    }

    [Fact]
    public async Task Apply_NoEntities_Unchanged()
    {
        XmlEntityResolverTextFilter filter = GetFilter();

        TextReader result = await filter.ApplyAsync(new StringReader("hello world"));

        Assert.Equal("hello world", await result.ReadToEndAsync());
    }

    [Fact]
    public async Task Apply_AmpEntity_Resolved()
    {
        XmlEntityResolverTextFilter filter = GetFilter();

        TextReader result = await filter.ApplyAsync(new StringReader("a &amp; b"));

        Assert.Equal("a & b", await result.ReadToEndAsync());
    }

    [Fact]
    public async Task Apply_XmlNamedEntities_Resolved()
    {
        XmlEntityResolverTextFilter filter = GetFilter();

        TextReader result = await filter.ApplyAsync(
            new StringReader("&lt;tag&gt; &quot;x&quot; &apos;y&apos;"));

        Assert.Equal("<tag> \"x\" 'y'", await result.ReadToEndAsync());
    }

    [Fact]
    public async Task Apply_HtmlNamedEntity_Resolved()
    {
        XmlEntityResolverTextFilter filter = GetFilter();

        TextReader result = await filter.ApplyAsync(new StringReader("&copy; 2024"));

        Assert.Equal("© 2024", await result.ReadToEndAsync());
    }

    [Fact]
    public async Task Apply_DecimalNumericEntity_Resolved()
    {
        XmlEntityResolverTextFilter filter = GetFilter();

        // &#65; = 'A'
        TextReader result = await filter.ApplyAsync(new StringReader("&#65;"));

        Assert.Equal("A", await result.ReadToEndAsync());
    }

    [Fact]
    public async Task Apply_HexNumericEntityLower_Resolved()
    {
        XmlEntityResolverTextFilter filter = GetFilter();

        // &#x41; = 'A'
        TextReader result = await filter.ApplyAsync(new StringReader("&#x41;"));

        Assert.Equal("A", await result.ReadToEndAsync());
    }

    [Fact]
    public async Task Apply_HexNumericEntityUpper_Resolved()
    {
        XmlEntityResolverTextFilter filter = GetFilter();

        // &#X41; = 'A'
        TextReader result = await filter.ApplyAsync(new StringReader("&#X41;"));

        Assert.Equal("A", await result.ReadToEndAsync());
    }

    [Fact]
    public async Task Apply_UnknownEntity_Unchanged()
    {
        XmlEntityResolverTextFilter filter = GetFilter();

        TextReader result = await filter.ApplyAsync(new StringReader("&unknown123;"));

        Assert.Equal("&unknown123;", await result.ReadToEndAsync());
    }

    [Fact]
    public async Task Apply_MultipleEntities_AllResolved()
    {
        XmlEntityResolverTextFilter filter = GetFilter();

        TextReader result = await filter.ApplyAsync(
            new StringReader("&lt;p&gt;Hello &amp; world&lt;/p&gt;"));

        Assert.Equal("<p>Hello & world</p>", await result.ReadToEndAsync());
    }

    [Fact]
    public async Task Apply_PaddingEnabled_AmpLengthPreserved()
    {
        XmlEntityResolverTextFilter filter = GetFilter(padding: true);

        // &amp; is 5 chars -> '&' + 4 spaces = 5 chars
        string input = "a &amp; b";
        TextReader result = await filter.ApplyAsync(new StringReader(input));
        string output = await result.ReadToEndAsync();

        Assert.Equal(input.Length, output.Length);
        Assert.Equal("a &     b", output);
    }

    [Fact]
    public async Task Apply_PaddingEnabled_DecimalLengthPreserved()
    {
        XmlEntityResolverTextFilter filter = GetFilter(padding: true);

        // &#65; is 5 chars -> 'A' + 4 spaces = 5 chars
        string input = "x&#65;y";
        TextReader result = await filter.ApplyAsync(new StringReader(input));
        string output = await result.ReadToEndAsync();

        Assert.Equal(input.Length, output.Length);
        Assert.Equal("xA    y", output);
    }

    [Fact]
    public async Task Apply_PaddingEnabled_HexLengthPreserved()
    {
        XmlEntityResolverTextFilter filter = GetFilter(padding: true);

        // &#x41; is 6 chars -> 'A' + 5 spaces = 6 chars
        string input = "x&#x41;y";
        TextReader result = await filter.ApplyAsync(new StringReader(input));
        string output = await result.ReadToEndAsync();

        Assert.Equal(input.Length, output.Length);
        Assert.Equal("xA     y", output);
    }

    [Fact]
    public async Task Apply_PaddingEnabled_UnknownEntityUnchanged()
    {
        XmlEntityResolverTextFilter filter = GetFilter(padding: true);

        string input = "&unknown;";
        TextReader result = await filter.ApplyAsync(new StringReader(input));
        string output = await result.ReadToEndAsync();

        Assert.Equal(input, output);
    }
}
