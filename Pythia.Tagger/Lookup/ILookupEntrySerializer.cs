using System.IO;

namespace Pythia.Tagger.Lookup
{
    /// <summary>
    /// Lookup entry serializer interface.
    /// </summary>
    public interface ILookupEntrySerializer
    {
        /// <summary>
        /// Serializes the specified entry to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="entry">The entry.</param>
        void Serialize(Stream stream, LookupEntry entry);

        /// <summary>
        /// Deserializes the next entry (if any) from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>entry or null if end of stream</returns>
        LookupEntry? Deserialize(Stream stream);
    }
}
