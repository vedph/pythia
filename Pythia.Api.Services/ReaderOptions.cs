namespace Pythia.Api.Services;

/// <summary>
/// Options for reading documents via the API. These are loaded from the
/// <c>Reader</c> section of the API configuration.
/// </summary>
public class ReaderOptions
{
    /// <summary>
    /// The name of the configuration section for these options.
    /// </summary>
    public const string SECTION_NAME = "Reader";

    /// <summary>
    /// Gets or sets a value indicating whether reading the full text of
    /// documents is disabled. When true, the API never serves the whole
    /// text of a document, but only the context of a range (e.g. a search
    /// result): the endpoints returning a document's text, map, or text by
    /// map path are forbidden; documents are always returned without their
    /// content; and the context of a range is forbidden when it would
    /// include the whole map of the document. This is useful when users
    /// must not be able to read whole documents, e.g. for copyright reasons.
    /// </summary>
    public bool IsFullTextDisabled { get; set; }
}
