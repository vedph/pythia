using System.Collections.Generic;

namespace Corpus.Core.Reading;

/// <summary>
/// Interface to be implemented by text sources collectors. A source collector
/// is a component which given a global source scans it looking at all the texts
/// it contains, and returns the source for each of them. You can then use an
/// <see cref="ITextRetriever"/> instance to retrieve each text from its source.
/// </summary>
public interface ISourceCollector
{
    /// <summary>
    /// Collects all the text sources available in the specified source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>text sources</returns>
    IEnumerable<string> Collect(string source);
}
