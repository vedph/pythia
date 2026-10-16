using Corpus.Core;
using System.ComponentModel.DataAnnotations;

namespace Pythia.Api.Models;

/// <summary>
/// Attributes filter model.
/// </summary>
public sealed class AttributeFilterBindingModel
{
    /// <summary>
    /// The page number.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; }

    /// <summary>
    /// The size of the page (0=get all the attributes at once).
    /// </summary>
    [Range(0, 100)]
    public int PageSize { get; set; }

    /// <summary>
    /// The attribute type.
    /// </summary>
    [EnumDataType(typeof(AttributeFilterType))]
    public AttributeFilterType Type { get; set; }

    /// <summary>
    /// The optional attribute's name filter.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AttributeFilterBindingModel"/>
    /// class.
    /// </summary>
    public AttributeFilterBindingModel()
    {
        PageNumber = 1;
        PageSize = 20;
    }

    /// <summary>
    /// Converts this model to the corresponding Pythia filter.
    /// </summary>
    /// <returns>The filter.</returns>
    public AttributeFilter ToFilter()
    {
        return new AttributeFilter
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            // in Pythia, structures and tokens are both spans, whose
            // attributes are all stored in the span_attribute table
            Target = Type == AttributeFilterType.Document
                ? "document_attribute"
                : "span_attribute",
            Name = Name
        };
    }
}

/// <summary>
/// The type of attribute specified in <see cref="AttributeFilterBindingModel"/>.
/// </summary>
public enum AttributeFilterType
{
    /// <summary>
    /// Document attribute.
    /// </summary>
    Document = 0,
    /// <summary>
    /// Structure attribute.
    /// </summary>
    Structure,
    /// <summary>
    /// Occurrence attribute.
    /// </summary>
    Occurrence
}
