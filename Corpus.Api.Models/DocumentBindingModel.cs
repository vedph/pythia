using Corpus.Core;
using System.ComponentModel.DataAnnotations;

namespace Corpus.Api.Models;

/// <summary>
/// Document binding model.
/// </summary>
public class DocumentBindingModel
{
    /// <summary>
    /// The document's identifier (when editing an existing document;
    /// 0 for a new document).
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the document's profile identifier. This profile
    /// is only used to calculate the document's metadata, like sort ID
    /// and date value. It is not used when analyzing the document, as
    /// in this case the profile is a user-dependent choice which can
    /// vary for the same document.
    /// </summary>
    [Required]
    public string? ProfileId { get; set; }

    /// <summary>
    /// The author(s).
    /// </summary>
    [MaxLength(100)]
    public string? Author { get; set; }

    /// <summary>
    /// The title.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string? Title { get; set; }

    /// <summary>
    /// The source to be used to retrieve this document's content;
    /// this might be a file path, an URI, etc.
    /// </summary>
    [Required]
    [MaxLength(300)]
    public string? Source { get; set; }

    /// <summary>
    /// The date value. This is a numeric value calculated
    /// according to the source documents metadata, and representing a numeric
    /// value which can be used to sort documents by their datation,
    /// even when approximate.
    /// </summary>
    public double DateValue { get; set; }

    /// <summary>
    /// The sort key.
    /// </summary>
    [MaxLength(500)]
    public string? SortKey { get; set; }

    /// <summary>
    /// The attributes.
    /// </summary>
    public AttributeBindingModel[]? Attributes { get; set; }

    /// <summary>
    /// Gets a document from this model.
    /// </summary>
    /// <param name="userId">The user identifier to assign to the document.
    /// </param>
    /// <returns>Document.</returns>
    public Document GetDocument(string userId)
    {
        Document document = new()
        {
            Id = Id,
            ProfileId = ProfileId,
            UserId = userId,
            Author = Author ?? "",
            Title = Title,
            Source = Source,
            DateValue = DateValue,
            SortKey = SortKey,
        };
        if (Attributes?.Length > 0)
        {
            foreach (var attribute in Attributes)
            {
                document.Attributes!.Add(new Attribute
                {
                    TargetId = document.Id,
                    Name = attribute.Name,
                    Value = attribute.Value,
                    Type = attribute.Type
                });
            }
        }
        return document;
    }
}
