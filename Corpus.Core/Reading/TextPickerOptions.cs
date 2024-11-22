namespace Corpus.Core.Reading;

/// <summary>
/// Text picker options.
/// </summary>
public class TextPickerOptions
{
    /// <summary>
    /// Gets or sets the string to be inserted before the hit in the picked text.
    /// The default value is <c>{{</c>.
    /// </summary>
    public string? HitOpen { get; set; }

    /// <summary>
    /// Gets or sets the string to be inserted after the hit in the picked text.
    /// The default value is <c>}}</c>.
    /// </summary>
    public string? HitClose { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextPickerOptions"/> class.
    /// </summary>
    public TextPickerOptions()
    {
        HitOpen = "{{";
        HitClose = "}}";
    }
}
