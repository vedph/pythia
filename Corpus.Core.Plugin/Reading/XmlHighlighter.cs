using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Corpus.Core.Plugin.Reading;

/// <summary>
/// XML highlighter. This gets an XML document with a portion of its text
/// delimited by a pair of escapes like <c>{{</c> and <c>}}</c>, and ensures
/// that they are wrapped in a <c>hi</c> element.
/// </summary>
public sealed class XmlHighlighter
{
    private const string ESC_NAME = "__esc__";
    private const string ESC_OPEN = "<__esc__>";
    private const string ESC_CLOSE = "</__esc__>";

    private string _openEsc = "{{";
    private string _closeEsc = "}}";
    private XElement _hi = new("hi", new XAttribute("rend", "hit"));

    #region Properties
    /// <summary>
    /// Gets or sets the opening escape.
    /// </summary>
    /// <value>The opening escape.</value>
    /// <exception cref="ArgumentNullException"></exception>
    public string OpeningEscape
    {
        get => _openEsc;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _openEsc = value;
        }
    }

    /// <summary>
    /// Gets or sets the closing escape.
    /// </summary>
    /// <value>The closing escape.</value>
    /// <exception cref="ArgumentNullException"></exception>
    public string ClosingEscape
    {
        get => _closeEsc;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _closeEsc = value;
        }
    }

    /// <summary>
    /// Gets or sets the highlight element model. This will be used to create
    /// as many highlight elements as needed.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public XElement HiElement
    {
        get => _hi;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _hi = value;
        }
    }
    #endregion

    /// <summary>
    /// Finds the ranges of highlighted text.
    /// </summary>
    private List<(int Start, int End)> FindHighlightRanges(string text)
    {
        List<(int, int)> ranges = [];
        int i = 0;
        while (i < text.Length)
        {
            // find opening escape (might not be here if closing only)
            int openIndex = text.IndexOf(_openEsc, i);

            // find closing escape (might not be here if opening only)
            int closeIndex = text.IndexOf(_closeEsc,
                openIndex > -1 ? openIndex + _openEsc.Length : i);

            // if neither found, break
            if (openIndex == -1 && closeIndex == -1) break;

            // add range (including escapes)
            ranges.Insert(0,
                (openIndex > -1
                    ? openIndex
                    : i,
                 closeIndex > -1
                    ? closeIndex + _closeEsc.Length
                    : text.Length));

            // move to next potential highlight
            if (closeIndex == -1) break;
            i = closeIndex + _closeEsc.Length;
        }
        return ranges;
    }

    /// <summary>
    /// Wraps a specific highlight range in the element.
    /// </summary>
    private void WrapHighlightRange(XElement element, int start, int end)
    {
        // collect all text nodes
        List<XText> textNodes = element.DescendantNodes().OfType<XText>().ToList();

        // track progress through text nodes
        int currentPosition = 0;
        List<(XText OriginalNode, List<XNode> NewNodes)> nodesToReplace = [];

        foreach (XText textNode in textNodes)
        {
            int nodeLength = textNode.Value.Length;
            int nodeEnd = currentPosition + nodeLength;

            // check if this node is involved in the highlight
            if (start < nodeEnd && end > currentPosition)
            {
                // calculate local start and end within this node
                int localStart = Math.Max(0, start - currentPosition);
                int localEnd = Math.Min(nodeLength, end - currentPosition);

                // split the text node
                string beforeText = textNode.Value[..localStart];
                string highlightText = textNode.Value[localStart..localEnd];
                string afterText = textNode.Value[localEnd..];

                // remove escapes from highlight text
                highlightText = highlightText
                    .Replace(_openEsc, "")
                    .Replace(_closeEsc, "");

                // create new nodes
                List<XNode> newNodes = [];

                // add before text if exists
                if (!string.IsNullOrEmpty(beforeText))
                    newNodes.Add(new XText(beforeText));

                // add highlight element
                XElement hiElement = new(HiElement.Name,
                    HiElement.Attributes().ToArray(),
                    highlightText);
                newNodes.Add(hiElement);

                // add after text if exists
                if (!string.IsNullOrEmpty(afterText))
                    newNodes.Add(new XText(afterText));

                // mark for replacement
                nodesToReplace.Add((textNode, newNodes));
            }

            currentPosition += nodeLength;
        }

        // replace nodes
        foreach ((XText originalNode, List<XNode> newNodes) in nodesToReplace)
            originalNode.ReplaceWith(newNodes);
    }

    /// <summary>
    /// Processes highlights in the given element.
    /// </summary>
    private void ProcessHighlights(XElement element, string fullText)
    {
        // find all highlight ranges
        List<(int Start, int End)> highlightRanges = FindHighlightRanges(fullText);
        if (highlightRanges.Count == 0) return;

        // reprocess text nodes for each highlight range
        foreach ((int start, int end) in highlightRanges)
            WrapHighlightRange(element, start, end);
    }

    /// <summary>
    /// Retrieves the full text content of an element, preserving XML structure.
    /// </summary>
    private static string GetFullText(XElement element)
    {
        return string.Concat(element.DescendantNodes().OfType<XText>()
            .Select(t => t.Value));
    }

    /// <summary>
    /// Finds and wraps highlights in an element's text nodes.
    /// </summary>
    private void FindAndWrapHighlights(XElement element)
    {
        // Get the root ancestor since highlights might span multiple elements
        XElement root = element.AncestorsAndSelf().Last();

        // Get all text nodes under this root with escapes
        List<XText> textNodes = root.DescendantNodes()
            .OfType<XText>()
            .Where(t => t.Value.Contains(_openEsc) || t.Value.Contains(_closeEsc))
            .ToList();

        if (textNodes.Count == 0) return;

        // Get the full text under this root
        string fullText = GetFullText(root);

        // if no complete escapes found, return
        if (!fullText.Contains(_openEsc) || !fullText.Contains(_closeEsc))
        {
            return;
        }

        // process highlights at the root level to handle cross-element spans
        ProcessHighlights(root, fullText);
    }

    /// <summary>
    /// Processes a node and its descendants, wrapping highlighted text.
    /// Deeper nodes are processed first.
    /// </summary>
    /// <param name="node">The node to process.</param>
    private void ProcessNode(XNode node)
    {
        // only process element nodes
        if (node is not XElement element) return;

        // process child nodes first
        foreach (XNode childNode in element.Nodes().ToList())
        {
            ProcessNode(childNode);
        }

        // ensure the node still exists (it might have been modified)
        if (element.Parent != null)
        {
            FindAndWrapHighlights(element);
        }
    }

    private static bool HasWsSiblingsOnly(XElement element)
    {
        foreach (XNode node in element.Parent!.Nodes())
        {
            if (node is XText text && !string.IsNullOrWhiteSpace(text.Value))
                return false;
            if (node is XElement child && child != element)
                return false;
        }
        return true;
    }

    private static void ProcessWrappedElements(XElement element)
    {
        foreach (XNode node in element.Nodes().ToList())
        {
            if (node is XElement child)
            {
                // recursively process child elements
                ProcessWrappedElements(child);

                // check if the parent is an escape element and this element
                // is the unique child element of it and all the other sibling
                // nodes aren't text nodes or are empty or whitespace-only
                // text nodes
                if (child.Parent?.Name.LocalName == ESC_NAME &&
                    child.Parent.Elements().Count() == 1 &&
                    HasWsSiblingsOnly(child))
                {
                    XName escName = child.Parent.Name;

                    // create a new element with the same name and attributes
                    // but with its nodes inside the escape element
                    XElement newChild = new(child.Name,
                        child.Attributes(),
                        new XElement(escName, child.Nodes()));

                    // replace the child with the new element
                    child.Parent.ReplaceWith(newChild);
                }
            }
        }
    }

    /// <summary>
    /// Preprocess XML text to move escapes inside elements when they fully
    /// wrap them. This removes a corner case in the highlighting process.
    /// </summary>
    private string PreprocessWrappedElements(string xml)
    {
        // create a temporary document where we replace escapes with
        // unique elements to safely parse as XML
        string tempXml = xml
            .Replace(_openEsc, ESC_OPEN)
            .Replace(_closeEsc, ESC_CLOSE);

        XDocument doc;
        try
        {
            doc = XDocument.Parse(tempXml, LoadOptions.PreserveWhitespace);
        }
        catch
        {
            // if parsing fails, return original XML unchanged
            return xml;
        }

        // process all elements recursively
        ProcessWrappedElements(doc.Root!);

        // convert back to string and restore original escapes
        return doc.ToString(SaveOptions.DisableFormatting)
            .Replace(ESC_OPEN, _openEsc)
            .Replace(ESC_CLOSE, _closeEsc);
    }

    /// <summary>
    /// Wraps the highlighted text.
    /// </summary>
    /// <param name="doc">The document.</param>
    public void WrapHighlightedText(XDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        if (doc.Root == null) return;

        // preprocess the XML
        string xml = doc.ToString(SaveOptions.DisableFormatting);
        string prepXml = PreprocessWrappedElements(xml);

        // only reparse if changes were made
        if (prepXml != xml)
        {
            doc.ReplaceNodes(XDocument.Parse(prepXml,
                LoadOptions.PreserveWhitespace).Root);
        }

        // recursively process all nodes in the document
        ProcessNode(doc.Root);
    }
}
