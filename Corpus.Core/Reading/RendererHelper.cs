using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Corpus.Core.Reading;

/// <summary>
/// Helper methods for renderers.
/// </summary>
public static class RendererHelper
{
    private static readonly Regex _tagRegex =
        new(@"<(?<c>/)?(?<n>[^>\s/]+)[^>/]*(?<e>/)?>");

    /// <summary>
    /// Wrap hits as marked by hit opening and closing sequences into the
    /// specified opening and closing tag, in an XML (or XHTML) text fragment.
    /// </summary>
    /// <param name="xml">XHTML or XML text to process</param>
    /// <param name="hitOpen">The string used in the input text to open a
    /// hit</param>
    /// <param name="hitClose">The string used in the input text to close
    /// a hit</param>
    /// <param name="openTag">The open tag to replace
    /// <paramref name="hitOpen"/></param>
    /// <param name="closeTag">The close tag to replace
    /// <paramref name="hitClose"/></param>
    /// <returns>processed text</returns>
    /// <remarks>
    /// When rendering HTML/XML output, we cannot simply replace the hit
    /// markers with tags, as nothing ensures that they would not overlap
    /// the rendered existing elements. You can use this method to overcome
    /// this issue, as it closes and reopens the required tag wherever
    /// necessary to keep the code well-formed.
    /// Note: this method assumes that the input XML code is well-formed.
    /// </remarks>
    public static string WrapHitsInTags(string xml, string hitOpen,
        string hitClose, string openTag, string closeTag)
    {
        ArgumentNullException.ThrowIfNull(xml);
        ArgumentNullException.ThrowIfNull(hitOpen);
        ArgumentNullException.ThrowIfNull(hitClose);
        ArgumentNullException.ThrowIfNull(openTag);
        ArgumentNullException.ThrowIfNull(closeTag);

        StringBuilder sb = new();
        bool inHit = false, hitReopen = false;
        int i = 0;
        while (i < xml.Length)
        {
            if (xml[i] == '<')
            {
                Match m = _tagRegex.Match(xml, i, xml.Length - i);
                Debug.Assert(m.Success);

                // empty tag: just copy
                if (m.Groups["e"].Length > 0)
                {
                    sb.Append(m.Value);
                    i += m.Length;
                    continue;
                }

                // closing tag: if in a hit, close it before the tag
                // (unless this has already been done; this happens when
                // several closing tags follow each other)
                if (m.Groups["c"].Length > 0)
                {
                    if (inHit && (!hitReopen))
                    {
                        sb.Append(closeTag);
                        hitReopen = true;
                    }
                    // copy the tag
                    sb.Append(m.Value);
                    i += m.Length;
                    continue;
                }

                // opening tag: just copy
                sb.Append(m.Value);
                i += m.Length;
            }
            else
            {
                if (inHit)
                {
                    if (string.CompareOrdinal(
                        hitClose, 0, xml, i, hitClose.Length) == 0)
                    {
                        sb.Append(closeTag);
                        i += hitClose.Length;
                        hitReopen = inHit = false;
                        continue;
                    }
                    if (hitReopen)
                    {
                        sb.Append(openTag);
                        hitReopen = false;
                    }
                }
                else if (string.CompareOrdinal(
                    hitOpen, 0, xml, i, hitOpen.Length) == 0)
                {
                    sb.Append(openTag);
                    inHit = true;
                    i += hitOpen.Length;
                    continue;
                }
                sb.Append(xml[i++]);
            }
        }

        return sb.ToString();
    }
}
