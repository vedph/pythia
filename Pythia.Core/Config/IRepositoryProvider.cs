namespace Pythia.Core.Config;

/// <summary>
/// Repository provider. A repository requires a connection string
/// to be instantiated, so this provider is a level of indirection
/// which gets configured with <see cref="RepositoryOptions"/>.
/// </summary>
/// <remarks>
/// See https://stackoverflow.com/questions/50890514/how-to-change-registered-simple-injector-dbcontexts-connection-string-after-u
/// </remarks>
public interface IRepositoryProvider
{
    /// <summary>
    /// Gets the repository.
    /// </summary>
    object GetRepository();
}
