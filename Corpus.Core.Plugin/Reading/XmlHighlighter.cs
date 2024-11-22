using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Corpus.Core.Plugin.Reading;

/// <summary>
/// XML highlighter. This gets an XML document with a portion of its text
/// delimited by a couple of escapes like <c>{{</c> and <c>}}</c>, and ensures
/// that they are wrapped in a <c>hi</c> element.
/// </summary>
public sealed class XmlHighlighter
{
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

        foreach (XText? textNode in textNodes)
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
                    .Replace(OpeningEscape, "")
                    .Replace(ClosingEscape, "");

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
        foreach ((int Start, int End) range in highlightRanges)
            WrapHighlightRange(element, range.Start, range.End);
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
        // collect text nodes in this element
        List<XText> textNodes = element.DescendantNodes()
            .OfType<XText>()
            .Where(t => t.Value.Contains(OpeningEscape) ||
                        t.Value.Contains(ClosingEscape))
            .ToList();

        // if no text nodes with escapes, return
        if (textNodes.Count == 0) return;

        // fully flatten the element's text before processing
        string fullText = GetFullText(element);

        // if no escapes found, return
        if (!fullText.Contains(_openEsc) &&
            !fullText.Contains(_closeEsc))
        {
            return;
        }

        // process highlights
        ProcessHighlights(element, fullText);
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

        // recursively process child nodes first
        foreach (XNode? childNode in element.Nodes().ToList())
            ProcessNode(childNode);

        // ensure the node still exists (it might have been modified)
        if (element.Parent == null) return;

        // find and process text nodes with highlights
        FindAndWrapHighlights(element);
    }

    /// <summary>
    /// Wraps the highlighted text.
    /// </summary>
    /// <param name="doc">The document.</param>
    public void WrapHighlightedText(XDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        if (doc.Root == null) return;

        // recursively process all nodes in the document
        ProcessNode(doc.Root);
    }
}
