namespace Pythia.Core;

/// <summary>
/// Information about an attribute.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AttributeInfo"/> class.
/// </remarks>
/// <param name="name">The name.</param>
/// <param name="type">The type.</param>
public class AttributeInfo(string name, int type)
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the type.
    /// </summary>
    public int Type { get; } = type;

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Name} ({Type})";
    }
}
