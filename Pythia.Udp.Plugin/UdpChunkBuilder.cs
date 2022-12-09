using Fusi.Tools.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Pythia.Udp.Plugin;

/// <summary>
/// UDP chunks builder.
/// </summary>
public sealed class UdpChunkBuilder
{
    /// <summary>
    /// Gets or sets the regular expression used to detect a safe
    /// break point for chunking.
    /// </summary>
    public Regex TailRegex { get; set; }

    /// <summary>
    /// Gets or sets the maximum chunk length in characters.
    /// </summary>
    public int MaxLength { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UdpChunkBuilder"/> class.
    /// </summary>
    public UdpChunkBuilder()
    {
        TailRegex = new("[.?!](?![.?!])", RegexOptions.Compiled);
        MaxLength = 5000;
    }

    private static void CheckAlpha(IList<UdpChunk> chunks, string text)
    {
        foreach (UdpChunk chunk in chunks)
        {
            bool alpha = false;
            for (int i = chunk.Range.Start; i <= chunk.Range.End && !alpha; i++)
            {
                if (char.IsLetter(text[i])) alpha = true;
            }
            if (!alpha) chunk.HasNoAlpha = true;
        }
    }

    /// <summary>
    /// Builds chunks from the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>List of chunks.</returns>
    /// <exception cref="ArgumentNullException">text</exception>
    public IList<UdpChunk> Build(string text)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));

        List<UdpChunk> chunks = new();
        if (text.Length == 0) return chunks;

        TextRange lastSkippedTail = TextRange.Empty;
        int start = 0;

        foreach (Match m in TailRegex.Matches(text))
        {
            if (m.Index == start) continue;

            int end = m.Index + m.Length;
            int len = end - start;

            // if len is less than max, just skip ahead
            if (len < MaxLength)
            {
                lastSkippedTail = new TextRange(m.Index, m.Length);
            }
            // if len is equal to max, build max-size chunk
            else if (len == MaxLength)
            {
                chunks.Add(new UdpChunk(new TextRange(start, len)));
                start = end;
            }
            // else (len > max) try backing to a previous tail, or, if not
            // possible, build an excess-size chunk
            else
            {
                if (lastSkippedTail == TextRange.Empty)
                {
                    chunks.Add(new UdpChunk(new TextRange(start, len), true));
                }
                else
                {
                    chunks.Add(new UdpChunk(
                        new TextRange(start, lastSkippedTail.End + 1 - start)));
                }
                start = end;
            }
        }

        // if any text remains beyond the last chunk:
        if (start < text.Length)
        {
            // if no skipped tails or <= max, treat as the last chunk
            if (lastSkippedTail == TextRange.Empty ||
                text.Length - start <= MaxLength)
            {
                int len = text.Length - start;
                chunks.Add(new UdpChunk(
                    new TextRange(start, len),
                    len > MaxLength));
            }
            // else add two chunks at the end
            else
            {
                chunks.Add(new UdpChunk(
                    new TextRange(start, lastSkippedTail.End + 1 - start)));
                start = lastSkippedTail.End + 1;

                int len = text.Length - start;
                chunks.Add(new UdpChunk(
                    new TextRange(start, len),
                    len > MaxLength));
            }
        }

        CheckAlpha(chunks, text);

        return chunks;
    }
}
