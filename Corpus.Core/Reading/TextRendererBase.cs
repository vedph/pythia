using System.Collections.Generic;

namespace Corpus.Core.Reading;

/// <summary>
/// Base class for text renderers.
/// </summary>
/// <seealso cref="ITextRenderer" />
public abstract class TextRendererBase : ITextRenderer
{
    /// <summary>
    /// Gets or sets the optional filters to be applied to the input text
    /// before rendering.
    /// </summary>
    protected IList<IRendererTextFilter>? PreFilters { get; set; }

    /// <summary>
    /// Gets or sets the optional filters to be applied to the output text
    /// after rendering.
    /// </summary>
    protected IList<IRendererTextFilter>? PostFilters { get; set; }

    /// <summary>
    /// Does the rendering.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="text">The text, eventually filtered.</param>
    /// <returns>rendered output</returns>
    protected abstract string DoRender(IDocument document, string text);

    /// <inheritdoc />
    /// <summary>
    /// Renders the specified text.
    /// </summary>
    /// <param name="document">The document the text belongs to. This can
    /// be used by renderers to change their behavior according to the
    /// document's metadata.</param>
    /// <param name="text">The input text.</param>
    /// <returns>rendered text</returns>
    public string Render(IDocument document, string text)
    {
        if (PreFilters?.Count > 0)
        {
            foreach (IRendererTextFilter filter in PreFilters)
                text = filter.Apply(text);
        }

        string result = DoRender(document, text);

        if (PostFilters?.Count > 0)
        {
            foreach (IRendererTextFilter filter in PostFilters)
                result = filter.Apply(result);
        }

        return result;
    }
}

/// <summary>
/// A text filterer used by <see cref="TextRendererBase"/>.
/// </summary>
public interface IRendererTextFilter
{
    /// <summary>
    /// Applies this filter to the specified text.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <returns>The outpit text.</returns>
    string Apply(string text);
}
