using MessagePack;
using System;
using System.IO;

namespace Pythia.Tagger.Lookup;

/// <summary>
/// MessagePack-based serializer for <see cref="LookupEntry"/> objects.
/// This is a fast, compact binary serialization format that's widely used
/// and actively maintained.
/// </summary>
public sealed class MessagePackLookupEntrySerializer : ILookupEntrySerializer
{
    /// <summary>
    /// Options for MessagePack serialization.
    /// </summary>
    private readonly MessagePackSerializerOptions _options;

    /// <summary>
    /// Creates a new instance of the <see cref="MessagePackLookupEntrySerializer"/>
    /// class with default options.
    /// </summary>
    public MessagePackLookupEntrySerializer()
        : this(MessagePackSerializerOptions.Standard)
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="MessagePackLookupEntrySerializer"/>
    /// class with the specified options.
    /// </summary>
    /// <param name="options">Custom MessagePack serialization options.</param>
    public MessagePackLookupEntrySerializer(MessagePackSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Serializes the specified entry to the specified stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="entry">The entry.</param>
    /// <exception cref="ArgumentNullException">stream or entry is null</exception>
    public void Serialize(Stream stream, LookupEntry entry)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(entry);

        MessagePackSerializer.Serialize(stream, entry, _options);
    }

    /// <summary>
    /// Deserializes the next entry (if any) from the specified stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>entry or null if end of stream</returns>
    /// <exception cref="ArgumentNullException">stream is null</exception>
    public LookupEntry? Deserialize(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            if (stream.Position >= stream.Length) return null;

            return MessagePackSerializer.Deserialize<LookupEntry>(stream, _options);
        }
        catch (EndOfStreamException)
        {
            return null;
        }
    }
}
