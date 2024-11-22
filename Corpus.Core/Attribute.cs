using System;

namespace Corpus.Core;

/// <summary>
/// Attribute.
/// </summary>
public class Attribute
{
    /// <summary>
    /// Gets or sets the attribute identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the target identifier, which usually is the ID of
    /// the attribute's owner (e.g. document ID).
    /// </summary>
    public int TargetId { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public AttributeType Type { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Attribute"/> class.
    /// </summary>
    public Attribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Attribute"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentNullException">null name or value</exception>
    public Attribute(string name, string value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    /// <returns>new instance</returns>
    public Attribute Clone()
    {
        return new Attribute
        {
            Id = Id,
            TargetId = TargetId,
            Name = Name,
            Value = Value,
            Type = Type
        };
    }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"Attribute: {Name}={Value} [{Type}]";
    }
}

/// <summary>
/// Attribute type, used for comparisons.
/// </summary>
public enum AttributeType
{
    /// <summary>Text.</summary>
    Text = 0,

    /// <summary>Number.</summary>
    Number
}
