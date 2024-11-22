using Fusi.Tools.Data;

namespace Corpus.Core;

/// <summary>
/// Filter for attributes. This is used to get the list of attributes names
/// used for a specific target.
/// </summary>
/// <seealso cref="T:Corpus.Core.PagingOptions" />
public sealed class AttributeFilter : PagingOptions
{
    /// <summary>
    /// Gets or sets the target attribute set (table name).
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the name filter, matching any attribute's name
    /// which includes it.
    /// </summary>
    public string? Name { get; set; }
}
