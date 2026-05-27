using Pythia.Core.Config;

namespace Pythia.Api.Services;

/// <summary>
/// Provides a Pythia factory for querying.
/// </summary>
public interface IQueryPythiaFactoryProvider
{
    /// <summary>
    /// Gets the Pythia factory for querying.
    /// </summary>
    /// <returns>The Pythia factory.</returns>
    PythiaFactory GetFactory();
}