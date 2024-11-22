using System.Collections.Generic;

namespace Corpus.Core.Plugin.Reading;

/// <summary>
/// Hierarchical XML structure definition. This is a specialization of
/// <see cref="XmlStructureDefinition"/> adding data for nesting and
/// default value, required for a text map.
/// </summary>
/// <seealso cref="XmlStructureDefinition" />
public class HierarchicXmlStructureDefinition : XmlStructureDefinition
{
    /// <summary>
    /// Gets or sets the name of the parent structure definition.
    /// The root structure has this property equal to null.
    /// </summary>
    public string? ParentName { get; set; }

    /// <summary>
    /// Gets or sets the default value assigned to nodes corresponding
    /// to this definition when the value extracted from text is empty.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the children navigational property. This is set by
    /// the consumer of this definition while building a tree of definitions.
    /// </summary>
    public List<HierarchicXmlStructureDefinition> Children { get; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="HierarchicXmlStructureDefinition"/> class.
    /// </summary>
    public HierarchicXmlStructureDefinition()
    {
        Children = [];
    }
}
