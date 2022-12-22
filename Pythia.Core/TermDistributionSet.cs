using System.Collections.Generic;

namespace Pythia.Core;

/// <summary>
/// A set of <see cref="TermDistribution"/>'s produced as the result of a
/// <see cref="TermDistributionRequest"/>.
/// </summary>
public class TermDistributionSet
{
    /// <summary>
    /// Gets the term's identifier.
    /// </summary>
    public int TermId { get; }

    /// <summary>
    /// Gets or sets the total frequency of the term.
    /// </summary>
    public long TermFrequency { get; set; }

    /// <summary>
    /// Gets the frequencies related to documents attributes.
    /// </summary>
    public IDictionary<string, TermDistribution> DocFrequencies { get; }

    /// <summary>
    /// Gets the frequencies related to occurrences attributes.
    /// </summary>
    public IDictionary<string, TermDistribution> OccFrequencies { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TermDistributionSet"/> class.
    /// </summary>
    /// <param name="termId">The term identifier.</param>
    public TermDistributionSet(int termId)
    {
        TermId = termId;
        DocFrequencies = new Dictionary<string, TermDistribution>();
        OccFrequencies = new Dictionary<string, TermDistribution>();
    }
}
