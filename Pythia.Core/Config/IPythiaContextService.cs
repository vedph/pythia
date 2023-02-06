namespace Pythia.Core.Config;

/// <summary>
/// Pythia context service.
/// </summary>
public interface IPythiaContextService
{
    /// <summary>
    /// Gets the index repository.
    /// </summary>
    /// <param name="name">The index database name.</param>
    /// <returns>The repository.</returns>
    IIndexRepository GetIndexRepository(string name);

    /// <summary>
    /// Gets the Pythia factory.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="profile">The profile content.</param>
    /// <param name="name">The index database name.</param>
    /// <returns>The factory.</returns>
    PythiaFactory GetPythiaFactory(string profileId, string profile,
        string name);
}
