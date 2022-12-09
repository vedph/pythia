using Conllu;
using Fusi.Tools.Text;
using System.Collections.Generic;

namespace Pythia.Udp.Plugin;

/// <summary>
/// The definition of a chunk of text to submit to UDP analysis,
/// with the resulting sentences, set once the analysis has been completed.
/// </summary>
public sealed class UdpChunk
{
    /// <summary>
    /// Gets or sets the range of text representing the chunk.
    /// </summary>
    public TextRange Range { get; }

    /// <summary>
    /// Gets the sentences.
    /// </summary>
    public IList<Sentence> Sentences { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this chunk's text is oversized.
    /// Such chunks won't be processed with UDPipe, as they do not meet the
    /// max size requirements.
    /// </summary>
    public bool IsOversized { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UdpChunk"/> class.
    /// </summary>
    /// <param name="range">The range.</param>
    /// <param name="tooLong">True if the chunk delimited by the range is too
    /// long with reference to the maximum length set.</param>
    public UdpChunk(TextRange range, bool tooLong = false)
    {
        Range = range;
        Sentences = new List<Sentence>();
        IsOversized = tooLong;
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Range}: {Sentences.Count}";
    }
}
