using System;
using System.Collections.Generic;
using System.Linq;
using Corpus.Core.Reading;

namespace Pythia.Api.Models;

/// <summary>
/// Text map node viewmodel.
/// </summary>
public sealed class TextMapNodeModel
{
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
    public int Start { get; set; }

    /// <summary>
    /// Gets or sets the end character index in the text corresponding
    /// to the position after the last character belonging to this node.
    /// </summary>
    public int End { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is selected.
    /// </summary>
    public bool Selected { get; set; }

    /// <summary>
    /// Gets or sets the child nodes.
    /// </summary>
    public IList<TextMapNodeModel>? Children { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextMapNodeModel"/>
    /// class.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <exception cref="ArgumentNullException">node</exception>
    public TextMapNodeModel(TextMapNode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        Label = node.Label;
        Location = node.Location;
        Start = node.StartIndex;
        End = node.EndIndex;
        Selected = node.IsSelected;

        if (node.Children?.Any() == true)
        {
            Children = new List<TextMapNodeModel>();
            foreach (TextMapNode child in node.Children)
                Children.Add(new TextMapNodeModel(child));
        }
    }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return Label ?? base.ToString()!;
    }
}
