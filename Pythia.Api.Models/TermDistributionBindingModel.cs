using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pythia.Api.Models;

/// <summary>
/// Model for requesting the distribution of a term with reference to one or
/// more document/occurrence attributes.
/// </summary>
public class TermDistributionBindingModel
{
    /// <summary>
    /// The term identifier.
    /// </summary>
    [Required]
    public int TermId { get; set; }

    /// <summary>
    /// The maximum count of results to return for each attribute.
    /// </summary>
    [Required]
    [Range(1, 100)]
    public int Limit { get; set; }

    /// <summary>
    /// Gets or sets the optional interval, used for numeric attributes
    /// to group their values into ranges. Set to a value greater than 1 to
    /// enable ranges.
    /// </summary>
    public int Interval { get; set; }

    /// <summary>
    /// The reference document attributes.
    /// </summary>
    public IList<string> DocAttributes { get; set; }

    /// <summary>
    /// Gets or sets the reference occurrence attributes.
    /// </summary>
    public IList<string> OccAttributes { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TermDistributionBindingModel"/>
    /// class.
    /// </summary>
    public TermDistributionBindingModel()
    {
        DocAttributes = new List<string>();
        OccAttributes = new List<string>();
    }
}
