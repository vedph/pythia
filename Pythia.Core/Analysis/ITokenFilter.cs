using Fusi.Tools;
using System.Threading.Tasks;

namespace Pythia.Core.Analysis;

/// <summary>
/// Interface for token filters.
/// </summary>
public interface ITokenFilter
{
    /// <summary>
    /// Apply the filter to the specified token.
    /// </summary>
    /// <param name="token">The token span.</param>
    /// <param name="position">The position which will be assigned to
    /// the resulting span, provided that it's not empty. Some filters
    /// may use this value, e.g. to identify spans like in deferred
    /// POS tagging.</param>
    /// <param name="context">The optional context.</param>
    Task ApplyAsync(TextSpan token, int position, IHasDataDictionary? context = null);
}
