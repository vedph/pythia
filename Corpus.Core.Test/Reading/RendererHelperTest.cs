using Corpus.Core.Reading;
using Xunit;

namespace Corpus.Core.Test.Reading;

public sealed class RendererHelperTest
{
    [Fact]
    public void WrapHitsInTags_HitIncludingCloseTag()
    {
        const string input = "<div><p>abc{{de</p>fg}}h</div>";

        string output = RendererHelper.WrapHitsInTags(input, "{{", "}}",
            "<span class=\"hit\">",
            "</span>");

        Assert.Equal("<div><p>abc<span class=\"hit\">de</span></p>" +
                     "<span class=\"hit\">fg</span>h</div>",
            output);
    }

    [Fact]
    public void WrapHitsInTags_HitIncludingCloseAndOpenTag()
    {
        const string input = "<div><p>abc{{de</p><p>fg}}</p>h</div>";

        string output = RendererHelper.WrapHitsInTags(input, "{{", "}}",
                                                       "<span class=\"hit\">",
                                                       "</span>");

        Assert.Equal("<div><p>abc<span class=\"hit\">de</span></p>" +
                     "<p><span class=\"hit\">fg</span></p>h</div>",
            output);
    }
}
