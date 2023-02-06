using Pythia.Core.Analysis;

namespace Pythia.Core.Config;

/// <summary>
/// Interface implemented by configurable components using an inner
/// configurable tokenizer component.
/// </summary>
public interface IHasInnerTokenizer
{
    /// <summary>
    /// Sets the inner tokenizer.
    /// </summary>
    /// <param name="tokenizer">The tokenizer.</param>
    void SetInnerTokenizer(ITokenizer tokenizer);
}
