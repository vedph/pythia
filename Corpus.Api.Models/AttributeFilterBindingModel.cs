using Corpus.Core;

namespace Corpus.Api.Models;

/// <summary>
/// Attribute filter model.
/// </summary>
public sealed class AttributeFilterBindingModel : PagingFilterBindingModel
{
    /// <summary>
    /// The type of the attribute's target: 0=document, 1=structure, 2=token.
    /// </summary>
    public AttributeFilterType TargetType { get; set; }

    /// <summary>
    /// The name filter, matching any attribute's name which includes it.
    /// </summary>
    public string? Name { get; set; }

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
            Target = new[]
            {
                "document_attribute",
                "structure_attribute",
                "occurrence_attribute"
            }[(int)TargetType],
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
