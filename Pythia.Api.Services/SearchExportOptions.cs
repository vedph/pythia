namespace Pythia.Api.Services;

/// <summary>
/// Options for exporting search results. These are loaded from the
/// <c>SearchExport</c> section of the configuration.
/// </summary>
public class SearchExportOptions
{
    /// <summary>
    /// The name of the configuration section for these options.
    /// </summary>
    public const string SECTION_NAME = "SearchExport";

    /// <summary>
    /// Gets or sets the maximum number of results exported by a single
    /// request, or 0 for no limit (the default). When the API is public,
    /// this prevents a single request from exporting all the results of a
    /// very frequent term, which would load the database for a long time
    /// (and provide the contexts of all the occurrences of that term).
    /// When the limit is reached, the export just ends.
    /// </summary>
    public int MaxRows { get; set; }
}
