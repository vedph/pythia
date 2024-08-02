namespace Pythia.Core;

/// <summary>
/// Count of occurrences for a word or lemma in a subset of documents having
/// the specified name=value attribute pair.
/// </summary>
public class TokenCount(int sourceId, string attrName, string attrValue, int value)
{
    /// <summary>
    /// Gets the source (word or lemma) identifier.
    /// </summary>
    public int SourceId { get; } = sourceId;

    /// <summary>
    /// Gets the attribute name.
    /// </summary>
    public string AttributeName { get; } = attrName;

    /// <summary>
    /// Gets the attribute value.
    /// </summary>
    public string AttributeValue { get; } = attrValue;

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Value { get; } = value;

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"#{SourceId} {AttributeName}:{AttributeValue} = {Value}";
    }
}
