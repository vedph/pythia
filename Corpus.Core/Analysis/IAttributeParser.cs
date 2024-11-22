using System.Collections.Generic;
using System.IO;

namespace Corpus.Core.Analysis;

/// <summary>
/// Attribute parser. An attribute parser extracts document attributes
/// from its content.
/// </summary>
public interface IAttributeParser
{
    /// <summary>
    /// Parses the text from the specified reader.
    /// </summary>
    /// <param name="reader">The text reader.</param>
    /// <param name="document">The document being parsed.</param>
    /// <returns>List of attributes extracted from the text.</returns>
    IList<Attribute> Parse(TextReader reader, IDocument document);
}
