using Corpus.Core.Plugin.Reading;

namespace Pythia.Core.Plugin.Analysis
{
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
        public string TokenTargetName { get; set; }
    }
}
