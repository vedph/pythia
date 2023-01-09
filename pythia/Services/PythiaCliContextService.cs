namespace Pythia.Cli.Services;

/// <summary>
/// CLI context service.
/// </summary>
public class PythiaCliContextService
{
    public PythiaCliContextServiceConfig Configuration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PythiaCliContextService"/>
    /// class.
    /// </summary>
    /// <param name="config">The configuration.</param>
    public PythiaCliContextService(PythiaCliContextServiceConfig config)
    {
        Configuration = config;
    }
}

/// <summary>
/// Configuration for <see cref="PythiaCliContextService"/>.
/// </summary>
public class PythiaCliContextServiceConfig
{
    /// <summary>
    /// Gets or sets the connection string to the database.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the local directory to use when loading resources
    /// from the local file system.
    /// </summary>
    public string? LocalDirectory { get; set; }
}
