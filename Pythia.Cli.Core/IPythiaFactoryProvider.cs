using Pythia.Core.Config;

namespace Pythia.Cli.Core
{
    /// <summary>
    /// Plugin interface for <see cref="PythiaFactory"/> providers.
    /// </summary>
    public interface IPythiaFactoryProvider
    {
        /// <summary>
        /// Gets the factory.
        /// </summary>
        /// <param name="profileId">The profile identifier.</param>
        /// <param name="profile">The profile content.</param>
        /// <param name="connString">The connection string.</param>
        /// <returns>The factory.</returns>
        PythiaFactory GetFactory(string profileId, string profile, string connString);
    }
}
