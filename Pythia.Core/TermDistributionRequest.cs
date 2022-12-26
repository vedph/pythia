using System.Collections.Generic;

namespace Pythia.Core;

/// <summary>
/// A request for inspecting the distribution of a term with reference to
/// a set of document/token attributes.
/// </summary>
public class TermDistributionRequest
{
    /// <summary>
    /// Gets or sets the term identifier.
    /// </summary>
    public int TermId { get; set; }

    /// <summary>
    /// Gets or sets the maximum count of results to return for each
    /// attribute.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Gets or sets the reference document attributes.
    /// </summary>
    public IList<string> DocAttributes { get; set; }

    /// <summary>
    /// Gets or sets the reference occurrence attributes.
    /// </summary>
    public IList<string> OccAttributes { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TermDistributionRequest"/>
    /// class.
    /// </summary>
    public TermDistributionRequest()
    {
        DocAttributes = new List<string>();
        OccAttributes = new List<string>();
    }

    /// <summary>
    /// Determines whether this request has any attributes.
    /// </summary>
    public bool HasAttributes() =>
        DocAttributes?.Count > 0 || OccAttributes?.Count > 0;

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"#{TermId}×{Limit}: D={DocAttributes?.Count ?? 0}, " +
            $"T={OccAttributes?.Count ?? 0}";
    }
}
