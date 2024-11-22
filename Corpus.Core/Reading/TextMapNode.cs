using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Corpus.Core.Reading;

/// <summary>
/// A text map node.
/// </summary>
/// <remarks>A text map is a generic tree structure representing the map
/// of a text, as arbitrarily defined by a document parser for a specific
/// type of document.
/// For instance, in an XML document the parser might define some of the
/// elements as map nodes, and collect them eventually with their
/// descendants, up to some specific depth. In a plain text document
/// instead, the parser might detect section headers at some offsets and
/// collect them, etc. Each node in the map has a label, which might also
/// be empty or null, a location, whose meaning depends on the parser
/// which mapped the text (e.g. in an XML document it might be an XPATH
/// expression), and an offset.
/// </remarks>
public class TextMapNode
{
    private static readonly Regex _treeLineRegex =
        new(@"^(?<d>\.*)(?<l>[^[]*)\s+(?:\[(?<i1>\d+)-(?<i2>\d+)\])\s*(?<c>.+)?",
            RegexOptions.Compiled);

    #region Properties
    /// <summary>
    /// Gets or sets the optional node label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the location in the text corresponding to this node.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the start character index in the text corresponding
    /// to this node.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Gets or sets the end character index in the text corresponding to
    /// the position after the last character belonging to this node.
    /// </summary>
    public int EndIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is selected.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Gets or sets the parent node. If this is a root node, the parent
    /// is null.
    /// </summary>
    public TextMapNode? Parent { get; set; }

    /// <summary>
    /// Gets the children nodes, if any.
    /// </summary>
    public IList<TextMapNode> Children { get; } = [];

    /// <summary>
    /// Gets the index of this node among its siblings.
    /// </summary>
    public int SiblingIndex => Parent?.Children.IndexOf(this) ?? 0;

    /// <summary>
    /// Gets a value indicating whether this instance has children.
    /// </summary>
    public bool HasChildren => Children.Count > 0;
    #endregion

    /// <summary>
    /// Adds the specified node to the children of this node.
    /// </summary>
    /// <param name="node">The node to add.</param>
    /// <exception cref="ArgumentNullException">null node</exception>
    public void Add(TextMapNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Parent = this;
        Children.Add(node);
    }

    #region Path
    /// <summary>
    /// Gets the path from the root to this node.
    /// </summary>
    /// <returns>A path is a string where each number represents the index of
    /// a node among its siblings. Thus, in a tree where A=root and B and C are
    /// children of A, the path to node A is <c>0</c>, the path to node B is
    /// <c>0.0</c> (=1st child of root), the path to node C is <c>0.1</c>, etc.
    /// Note that this path only refers to text map nodes, and has no relation
    /// with the node location: the map node path is just for locating the nodes
    /// in a map, while the location depends on the text's format, and is used
    /// to locate a piece of text. Further, the map hierarchy itself often
    /// does not correspond to the text hierarchy: for instance, in a TEI
    /// document you might have 5 nested div's, but limit the map to the
    /// first 2 levels.
    /// </returns>
    public string GetPath()
    {
        // the root node is a corner case
        if (Parent == null) return "0";

        StringBuilder sb = new();
        TextMapNode? node = this;
        do
        {
            if (sb.Length > 0) sb.Insert(0, '.');
            sb.Insert(0, node.SiblingIndex);
            node = node.Parent;
        } while (node != null);

        return sb.ToString();
    }

    /// <summary>
    /// Gets the node descending from this node and located by the specified
    /// path.
    /// </summary>
    /// <param name="path">The path (<see cref="GetPath"/>), starting from
    /// the children of this node. For instance, if you have a root node A
    /// with children B and C, and you want to get C, the (relative) path
    /// is <c>1</c>.</param>
    /// <returns>node or null if not found</returns>
    /// <exception cref="ArgumentNullException">null path</exception>
    public TextMapNode? GetDescendant(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        TextMapNode node = this;
        string[] steps = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        foreach (string step in steps)
        {
            if (node.Children.Count == 0) return null;

            int i = int.Parse(step, CultureInfo.InvariantCulture);
            if (i < 0 || i >= node.Children.Count) return null;
            node = node.Children[i];
        }

        return node;
    }
    #endregion

    #region Visit
    private static bool VisitNode(TextMapNode node,
        Func<TextMapNode, bool> visitor)
    {
        if (!visitor(node)) return false;
        foreach (TextMapNode child in node.Children)
            if (!VisitNode(child, visitor)) return false;
        return true;
    }

    /// <summary>
    /// Visits this node and all its descendants, invoking for each visited
    /// node the specified function.
    /// </summary>
    /// <param name="visitor">The visitor function, receiving the node being
    /// visited and returning true to continue or false to stop.</param>
    /// <exception cref="ArgumentNullException">null visitor function</exception>
    public void Visit(Func<TextMapNode, bool> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        VisitNode(this, visitor);
    }

    /// <summary>
    /// Selects or deselects this node and all its descendants.
    /// </summary>
    /// <param name="isSelected">selection state to set</param>
    public void SelectAll(bool isSelected)
    {
        Visit(n =>
        {
            n.IsSelected = isSelected;
            return true;
        });
    }

    /// <summary>
    /// Gets the first selected node starting from this node.
    /// </summary>
    /// <returns>selected node or null</returns>
    public TextMapNode? GetFirstSelected()
    {
        TextMapNode? selected = null;
        Visit(n =>
        {
            if (n.IsSelected)
            {
                selected = n;
                return false;
            }
            return true;
        });

        return selected;
    }
    #endregion

    #region Dump
    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return Label ?? base.ToString()!;
    }

    private static string DumpNode(int depth, TextMapNode node)
    {
        StringBuilder sb = new();
        sb.Append('.', depth);

        sb.Append(node.Label).Append(" [")
          .Append(node.StartIndex)
          .Append('-')
          .Append(node.EndIndex)
          .Append(']');
        if (!string.IsNullOrEmpty(node.Location))
            sb.Append(' ').Append(node.Location);

        sb.AppendLine();
        return sb.ToString();
    }

    private static void DumpNodeWithDescendants(int depth, TextMapNode node,
        StringBuilder sb)
    {
        sb.Append(DumpNode(depth, node));

        depth++;
        foreach (TextMapNode child in node.Children)
            DumpNodeWithDescendants(depth, child, sb);
    }

    /// <summary>
    /// Dumps the tree rooted at this node.
    /// </summary>
    /// <returns>text in the same format used by <see cref="ParseTree"/></returns>
    public string DumpTree()
    {
        StringBuilder sb = new();
        DumpNodeWithDescendants(0, this, sb);
        return sb.ToString();
    }
    #endregion

    #region Parse
    /// <summary>
    /// Parse a text representing a text map nodes tree.
    /// </summary>
    /// <param name="reader">The reader for the tree text.</param>
    /// <returns>root node or null</returns>
    /// <exception cref="ArgumentNullException">null reader</exception>
    /// <remarks>
    /// The text has 1 line for each node, where each node is prefixed
    /// by a number of dots, each representing a deeper level in the tree,
    /// followed by the node label, followed by its location after the <c>@</c>
    /// character, if any. This is mainly used for rapidly creating node trees
    /// for testing.
    /// </remarks>
    public static TextMapNode? ParseTree(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        TextMapNode? nodeRoot = null, nodeParent = null;
        int currentDepth = 0;
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            Match m = _treeLineRegex.Match(line);
            if (!m.Success) continue;

            TextMapNode node = new()
            {
                Label = m.Groups["l"].Value,
                Location = m.Groups["c"].Value,
                StartIndex = int.Parse(m.Groups["i1"].Value,
                    CultureInfo.InvariantCulture),
                EndIndex = int.Parse(m.Groups["i2"].Value,
                    CultureInfo.InvariantCulture)
            };
            if (currentDepth == 0)
            {
                nodeRoot = nodeParent = node;
                currentDepth = 1;
            }
            else
            {
                int depth = m.Groups["d"].Length;
                Debug.Assert(nodeParent != null);
                if (depth == currentDepth)
                {
                    nodeParent.Add(node);
                }
                else
                {
                    nodeParent = depth < currentDepth
                                     ? nodeParent.Parent
                                     : nodeParent.Children[^1];
                    nodeParent!.Add(node);
                    currentDepth = depth;
                }
            }
        }

        return nodeRoot;
    }

    /// <summary>
    /// Gets the nearest common ancestor between this node and the other one.
    /// </summary>
    /// <param name="other">The other node.</param>
    /// <returns>The ancestor or null if not found.</returns>
    public TextMapNode? GetNearestCommonAncestor(TextMapNode? other)
    {
        if (other == null) return null;
        if (this == other) return this;

        TextMapNode? node = this;
        while (node != null)
        {
            TextMapNode? otherNode = other;
            while (otherNode != null)
            {
                if (node == otherNode) return node;
                otherNode = otherNode.Parent;
            }
            node = node.Parent;
        }

        return null;
    }
    #endregion
}
