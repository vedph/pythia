using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Fusi.Tools;

namespace Corpus.Core.Reading;

/// <summary>
/// Base class for XML to HTML text renderers.
/// </summary>
public abstract class XmlToHtmlTextRendererBase : ITextRenderer
{
    /// <summary>
    /// Gets the source document.
    /// </summary>
    protected IDocument? Document { get; private set; }

    /// <summary>
    /// Gets or sets the current target HTML element.
    /// </summary>
    protected XElement? Target { get; set; }

    /// <summary>
    /// Renders a TEI-like XML hi element to the target element.
    /// </summary>
    /// <param name="hi">The XML hi element</param>
    /// <param name="rendAttrName">The name of the XML hi element
    /// containing its value</param>
    /// <param name="builder">The optional hi class value builder to use</param>
    protected void RenderHi(XElement hi,
        string rendAttrName = "rend", HiClassValueBuilder? builder = null)
    {
        XElement element = new("span");

        string? rend = hi.ReadOptionalAttribute(rendAttrName, null);
        if (builder != null) rend = builder.Build(rend);
        if (!string.IsNullOrEmpty(rend))
            element.SetAttributeValue("class", rend);

        Target!.Add(element);
        Target = element;
    }

    /// <summary>
    /// Render the document's header.
    /// </summary>
    /// <param name="doc">source XML document</param>
    /// <param name="sb">target string builder</param>
    protected abstract void RenderHeader(XDocument doc, StringBuilder sb);

    /// <summary>
    /// Render the specified XML element to the specified target element.
    /// </summary>
    /// <param name="element">source XML element</param>
    protected abstract void RenderElement(XElement element);

    /// <summary>
    /// Render the specified node into the specified target element.
    /// </summary>
    /// <param name="node">source node</param>
    protected void RenderNode(XNode node)
    {
        if (node == null) return;

        switch (node.NodeType)
        {
            case XmlNodeType.Element:
                RenderElement((XElement)node);
                break;
            case XmlNodeType.Text:
                Target!.Add(node);
                break;
            case XmlNodeType.CDATA:
                Target!.Add(new XText((XCData)node).Value);
                break;
        }
    }

    /// <summary>
    /// Render the XML document's body to the specified string builder.
    /// </summary>
    /// <param name="doc">source XML document</param>
    /// <param name="sb">target string builder</param>
    protected virtual void RenderBody(XDocument doc, StringBuilder sb)
    {
        // root
        Target = new XElement("article", new XAttribute("class", "rendition"));
        RenderNode(doc.Root!);

        sb.Append(Target.AncestorsAndSelf().Last().ToString(SaveOptions.DisableFormatting));
    }

    /// <summary>
    /// Adds to the target element a new element with the specified name,
    /// and set it as the new target element.
    /// </summary>
    /// <param name="name">The element's name.</param>
    protected void AddAndSetAsTarget(XName name)
    {
        XElement element = new(name);
        Target!.Add(element);
        Target = element;
    }

    /// <summary>
    /// Adds to the target element a new element, and set it as the new
    /// target element.
    /// </summary>
    /// <param name="element">The element to add.</param>
    /// <exception cref="ArgumentNullException">null element</exception>
    protected void AddAndSetAsTarget(XElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        Target!.Add(element);
        Target = element;
    }

    /// <summary>
    /// Sets the target to the XML element which is the parent of the nearest
    /// ancestor/self XML element with the specified tag name.
    /// </summary>
    /// <param name="name">The tag name.</param>
    /// <exception cref="ArgumentNullException">null name</exception>
    protected void SetTargetToParentOf(XName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        XElement? childElement = Target?.AncestorsAndSelf(name)
            .FirstOrDefault(e => e.Parent != null);
        if (childElement != null) Target = childElement.Parent;
    }

    /// <summary>
    /// Sets the target to the XML element which is the parent of the nearest
    /// ancestor/self XML element matching the specified filter function.
    /// </summary>
    /// <param name="filter">The filter callback function.</param>
    /// <exception cref="ArgumentNullException">null name</exception>
    protected void SetTargetToParentOf(Func<XElement,bool> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        XElement? childElement = Target?.AncestorsAndSelf()
            .FirstOrDefault(e => e.Parent != null && filter(e));
        if (childElement != null) Target = childElement.Parent;
    }

    /// <summary>
    /// Renders the specified text.
    /// </summary>
    /// <param name="document">The document the text belongs to. This can
    /// be used by renderers to change their behavior according to the
    /// document's metadata.</param>
    /// <param name="text">The input text.</param>
    /// <returns>rendered text</returns>
    /// <exception cref="ArgumentNullException">null text</exception>
    public string Render(IDocument document, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Document = document;
        XDocument doc = XDocument.Parse(text, LoadOptions.PreserveWhitespace);

        StringBuilder sb = new();
        RenderHeader(doc, sb);

        RenderBody(doc, sb);

        sb.Append("</body></html>");

        // hits
        string result = sb.ToString();
        if (result.IndexOf("{{", StringComparison.OrdinalIgnoreCase) > -1)
        {
            result = RendererHelper.WrapHitsInTags(result,
               "{{", "}}", "<span class=\"hit\">", "</span>");
        }

        return result;
    }
}
