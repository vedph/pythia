using Fusi.Tools.Text;
using Pythia.Core.Plugin.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pythia.Udp.Plugin;

/// <summary>
/// UDP chunks builder.
/// </summary>
public sealed class UdpChunkBuilder
{
    /// <summary>
    /// Gets or sets the regular expression used to detect a safe
    /// break point for chunking (default=<c>[.?!](?![.?!])</c>).
    /// </summary>
    public Regex TailRegex { get; set; }

    /// <summary>
    /// Gets or sets the maximum chunk length in characters.
    /// </summary>
    public int MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the black tags.
    /// </summary>
    public HashSet<string>? BlackTags { get; set; }

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

    private static int SkipInitialWs(string text, int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
        return index;
    }

    private bool IsInBlackTag(int index, IList<XmlTagListEntry>? entries)
    {
        if (entries == null || BlackTags == null || BlackTags.Count == 0)
            return false;

        // ignore if inside a blacklisted tag
        foreach (var range in entries.Where(
            e => BlackTags.Contains(e.Name)).Select(e => e.Range))
        {
            // entries are sorted by start so we do not need to go further
            // when the current entry is past index
            if (range.Start > index) break;

            if (index >= range.Start && index <= range.End) return true;
        }
        return false;
    }

    /// <summary>
    /// Builds chunks from the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="entries">The optional list of XML tag entries previously
    /// detected by a <see cref="XmlLocalTagListTextFilter"/>.</param>
    /// <returns>List of chunks.</returns>
    /// <exception cref="ArgumentNullException">text</exception>
    public IList<UdpChunk> Build(string text,
        IList<XmlTagListEntry>? entries = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        List<UdpChunk> chunks = new();
        if (text.Length == 0) return chunks;

        // for each sentence tail:
        TextRange lastSkippedTail = TextRange.Empty;
        int start = 0;

        foreach (Match m in TailRegex.Matches(text))
        {
            // ignore empty tails or tails in blacklisted XML element
            if (m.Index == start ||  IsInBlackTag(m.Index, entries)) continue;

            // calculate chunk length (from start past tail)
            int end = m.Index + m.Length;
            int len = end - start;

            // if len is less than max, just skip ahead to collect more text
            if (len < MaxLength)
            {
                // keep track of the last skipped tail so we can backtrack later
                lastSkippedTail = new TextRange(m.Index, m.Length);
            }

            // if len is equal to max, build max-size chunk
            else if (len == MaxLength)
            {
                chunks.Add(new UdpChunk(new TextRange(start, len)));
                start = SkipInitialWs(text, end);
                lastSkippedTail = TextRange.Empty;
            }

            // else (len > max) try backing to a previous tail, or, if not
            // possible, build an excess-size chunk
            else
            {
                // no backtrack possible, just get an oversized chunk
                if (lastSkippedTail == TextRange.Empty)
                {
                    chunks.Add(new UdpChunk(new TextRange(start, len), true));
                    start = SkipInitialWs(text, start + len);
                }
                // backtrack to latest tail
                else
                {
                    chunks.Add(new UdpChunk(
                        new TextRange(start, lastSkippedTail.End + 1 - start)));
                    start = SkipInitialWs(text, lastSkippedTail.End + 1);
                    lastSkippedTail = TextRange.Empty;
                }
            }
        }

        // check if any non-WS text remains beyond the last chunk:
        start = SkipInitialWs(text, start);

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
                start = SkipInitialWs(text, lastSkippedTail.End + 1);

                int len = text.Length - start;
                chunks.Add(new UdpChunk(new TextRange(start, len),
                    len > MaxLength));
            }
        }

        // mark non-alpha chunks
        CheckAlpha(chunks, text);

        return chunks;
    }
}
