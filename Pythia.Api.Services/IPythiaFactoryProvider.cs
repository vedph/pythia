using Pythia.Core.Config;

namespace Pythia.Api.Services;

/// <summary>
/// Provides a Pythia factory for the specified profile.
/// </summary>
public interface IPythiaFactoryProvider
{
    /// <summary>
    /// Gets the Pythia factory for the specified profile.
    /// </summary>
    /// <param name="profile">The profile name.</param>
    /// <returns>The Pythia factory.</returns>
    PythiaFactory GetFactory(string profile);
}