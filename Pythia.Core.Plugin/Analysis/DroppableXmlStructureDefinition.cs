using Corpus.Core.Plugin.Reading;
using System.Collections.Generic;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// The definition of an XML-based structure for an
/// <see cref="XmlStructureParser"/>. This is equal to a general
/// <see cref="XmlStructureDefinition"/>, with an additional property
/// to represent ghost structures (which get dropped after indexing).
/// </summary>
public sealed class DroppableXmlStructureDefinition : XmlStructureDefinition
{
    /// <summary>
    /// Gets or sets the name of the token target. When this is not null,
    /// it means that the structure definition targets a token rather than
    /// a structure.
    /// This happens when you want to add attributes to those tokens which
    /// appear inside specific structures, but you do not want these
    /// structures to be stored as such, as their only purpose is marking
    /// the included tokens.
    /// For instance, a structure like "foreign" in TEI marks a token as
    /// a foreign word, but should not be stored among structures.
    /// </summary>
    public string? TokenTargetName { get; set; }

    /// <summary>
    /// Gets or sets the overridden POS value. When this is set, the token
    /// targeted by this ghost structure will have its POS value overridden.
    /// So, this option is meaningful only when <see cref="TokenTargetName"/>
    /// is not null.
    /// For instance, you might want to set POS to "ABBR" for all tokens
    /// representing an abbreviation, thus overriding the POS value which often
    /// is wrongly set by the POS tagger.
    /// </summary>
    public string? OverriddenPos { get; set; }

    /// <summary>
    /// Gets or sets the attributes to be overridden when
    /// <see cref="TokenTargetName"/> is not null. This usually is a set of POS
    /// tags which should be removed or overridden from the token because they were
    /// assigned by the POS tagger under the wrong assumption of a different POS.
    /// Each item can be in two formats:
    /// <list type="bullet">
    /// <item>
    /// <description><c>attributeName</c>: removes all attributes with this name.
    /// For instance: <c>clitic</c>, <c>definite</c>, <c>degree</c>, etc.</description>
    /// </item>
    /// <item>
    /// <description><c>attributeName=value</c>: removes all attributes with this name,
    /// then adds a new attribute with the specified name and value (text type).
    /// For instance: <c>gender=masc</c>, <c>number=sing</c>.</description>
    /// </item>
    /// <item>
    /// <description><c>attributeName==value</c>: removes all attributes with this name,
    /// then adds a new attribute with the specified name and value (numeric type).
    /// For instance: <c>count==5</c>.</description>
    /// </item>
    /// </list>
    /// </summary>
    public HashSet<string>? OverriddenAttributes { get; set; }
}
