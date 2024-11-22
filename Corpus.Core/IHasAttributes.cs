using System.Collections.Generic;

namespace Corpus.Core;

/// <summary>
/// Interface applied to components with attributes.
/// </summary>
public interface IHasAttributes
{
    /// <summary>
    /// Gets the attributes.
    /// </summary>
    IList<Attribute>? Attributes { get; set; }
}
