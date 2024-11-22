using Corpus.Core.Plugin.Reading;
using Xunit;

namespace Corpus.Core.Plugin.Test.Reading;

public sealed class XmlTextRendererTest
{
    private static XsltTextRenderer GetRenderer()
    {
        XsltTextRenderer renderer = new();
        renderer.Configure(new XsltTextRendererOptions
        {
            Script = TestHelper.LoadResourceText("Sample.xslt"),
            ScriptRootElement = "{http://www.tei-c.org/ns/1.0}body"
        });
        return renderer;
    }

    [Fact]
    public void Render_Complete_NoError()
    {
        string xml = TestHelper.LoadResourceText("TeiSampleDoc.xml");
        XsltTextRenderer renderer = GetRenderer();

        string html = renderer.Render(new Document(), xml);

        Assert.NotEqual(-1, html.IndexOf("<html"));
    }

    [Fact]
    public void Render_Partial_NoError()
    {
        string xml = TestHelper.LoadResourceText("TeiSamplePartialDoc.xml");
        XsltTextRenderer renderer = GetRenderer();

        string html = renderer.Render(new Document(), xml);

        Assert.NotEqual(-1, html.IndexOf("<html"));
    }

    [Fact]
    public void Render_PartialWithDefaultNs_NoError()
    {
        const string xml = "<p xmlns=\"http://www.tei-c.org/ns/1.0\">" +
            "This is a paragraph</p>";
        XsltTextRenderer renderer = GetRenderer();

        string html = renderer.Render(new Document(), xml);

        Assert.NotEqual(-1, html.IndexOf("<html"));
    }

    [Fact]
    public void Render_RootLess_NoError()
    {
        const string xml = "<p>Hello</p><p>world!</p>";
        XsltTextRenderer renderer = GetRenderer();

        string html = renderer.Render(new Document(), xml);

        Assert.NotEqual(-1, html.IndexOf("<html"));
    }
}
