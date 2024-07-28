using System;

namespace Pythia.Core;

/// <summary>
/// A search result with additional data defining the result's context.
/// </summary>
/// <seealso cref="T:Pythia.Core.SearchResult" />
public class KwicSearchResult : SearchResult
{
    /// <summary>
    /// Gets or sets the original text corresponding to the target token.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the left context, including also empty values
    /// when the context length is less than the available surrounding
    /// tokens.
    /// </summary>
    public string[] LeftContext { get; set; }

    /// <summary>
    /// Gets or sets the right context, including also empty values
    /// when the context length is less than the available surrounding
    /// tokens.
    /// </summary>
    public string[] RightContext { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KwicSearchResult"/>
    /// class.
    /// </summary>
    /// <param name="result">The source result to copy data from.</param>
    /// <exception cref="System.ArgumentNullException">result</exception>
    public KwicSearchResult(SearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        Text = "";
        LeftContext = RightContext = [];

        Id = result.Id;
        DocumentId = result.DocumentId;
        P1 = result.P1;
        P2 = result.P2;
        Index = result.Index;
        Length = result.Length;
        Type = result.Type;
        Value = result.Value;
        Author = result.Author;
        Title = result.Title;
        SortKey = result.SortKey;
    }
}
