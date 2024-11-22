using Corpus.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Corpus.Api.Models;

/// <summary>
/// Filter for a document, either single or in a set.
/// </summary>
public class DocumentFilterBindingModel
{
    /// <summary>
    /// Corpus ID.
    /// </summary>
    [MaxLength(50)]
    public string? CorpusId { get; set; }

    /// <summary>
    /// Corpus ID prefix.
    /// </summary>
    [MaxLength(50)]
    public string? CorpusIdPrefix { get; set; }

    /// <summary>
    /// Text to be found inside the authors field.
    /// </summary>
    [MaxLength(200)]
    public string? Author { get; set; }

    /// <summary>
    /// Text to be found inside the title field.
    /// </summary>
    [MaxLength(500)]
    public string? Title { get; set; }

    /// <summary>
    /// Text to be found inside the source field.
    /// </summary>
    [MaxLength(500)]
    public string? Source { get; set; }

    /// <summary>
    /// Profile ID.
    /// </summary>
    [MaxLength(50)]
    public string? ProfileId { get; set; }

    /// <summary>
    /// Profile ID prefix.
    /// </summary>
    [MaxLength(50)]
    public string? ProfileIdPrefix { get; set; }

    /// <summary>
    /// User ID.
    /// </summary>
    [MaxLength(256)]
    public string? UserId { get; set; }

    /// <summary>
    /// The minimum date value.
    /// </summary>
    public double? MinDateValue { get; set; }

    /// <summary>
    /// The maximum date value.
    /// </summary>
    public double? MaxDateValue { get; set; }

    /// <summary>
    /// The minimum modified date and time value.
    /// </summary>
    public DateTime? MinTimeModified { get; set; }

    /// <summary>
    /// The maximum modified date and time value.
    /// </summary>
    public DateTime? MaxTimeModified { get; set; }

    /// <summary>
    /// The document attributes, with format name=value, each separated by
    /// comma.
    /// </summary>
    public string? Attributes { get; set; }

    public virtual DocumentFilter ToFilter()
    {
        DocumentFilter filter = new()
        {
            CorpusId = CorpusId,
            CorpusIdPrefix = CorpusIdPrefix,
            Author = Author,
            Title = Title,
            Source = Source,
            ProfileId = ProfileId,
            ProfileIdPrefix = ProfileIdPrefix,
            UserId = UserId,
            MinDateValue = MinDateValue ?? 0,
            MaxDateValue = MaxDateValue ?? 0,
            MinTimeModified = MinTimeModified,
            MaxTimeModified = MaxTimeModified
        };

        if (!string.IsNullOrEmpty(Attributes))
        {
            filter.Attributes = new List<Tuple<string, string>>();
            Regex r = new("(^[^=]+)=(.+)$");
            foreach (string s in Attributes.Split(new[] { ',' },
                StringSplitOptions.RemoveEmptyEntries))
            {
                Match m = r.Match(s);
                if (m.Success)
                {
                    filter.Attributes.Add(
                       Tuple.Create(m.Groups[1].Value, m.Groups[2].Value));
                }
            }
        }

        return filter;
    }
}
