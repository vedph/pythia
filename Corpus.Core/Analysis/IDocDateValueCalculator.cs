using System.Collections.Generic;

namespace Corpus.Core.Analysis;

/// <summary>
/// Document date value calculator.
/// </summary>
public interface IDocDateValueCalculator
{
    /// <summary>
    /// Calculates the date value from the specified document's attributes.
    /// </summary>
    /// <param name="attributes">The attributes parsed from a document.</param>
    /// <returns>date value</returns>
    double Calculate(IList<Attribute> attributes);
}
