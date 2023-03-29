using Pythia.Core.Plugin.Analysis;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis;

public class ReplacerTextFilterTest
{
    [Fact]
    public async Task Apply_Empty_Nope()
    {
        ReplacerTextFilter filter = new();
        TextReader result = await filter.ApplyAsync(new StringReader(""));
        string text = result.ReadToEnd();
        Assert.Equal("", text);
    }

    [Fact]
    public async Task Apply_NoMatch_Nope()
    {
        ReplacerTextFilter filter = new();
        filter.Configure(new ReplacerTextFilterOptions
        {
            Replacements = new[]
            {
                new ReplacerOptionsEntry
                {
                    Source = "x",
                    Target = "y",
                }
            }
        });

        TextReader result = await filter.ApplyAsync(new StringReader("hello"));

        string text = result.ReadToEnd();
        Assert.Equal("hello", text);
    }

    [Fact]
    public async Task Apply_Match_Replaced()
    {
        ReplacerTextFilter filter = new();
        filter.Configure(new ReplacerTextFilterOptions
        {
            Replacements = new[]
            {
                new ReplacerOptionsEntry
                {
                    Source = "x",
                    Target = "y",
                }
            }
        });

        TextReader result = await filter.ApplyAsync(
            new StringReader("hello x world x!"));

        string actual = result.ReadToEnd();
        Assert.Equal("hello y world y!", actual);
    }

}
