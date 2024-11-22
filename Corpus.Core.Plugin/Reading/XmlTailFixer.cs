using Fusi.Xml.Extras;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Corpus.Core.Plugin.Reading;

/// <summary>
/// XML tail fixer. This utility class builds an XML tail from any
/// malformed XML fragment, which has pending opened tags at its end.
/// </summary>
public static class XmlTailFixer
{
    /// <summary>
    /// Gets the tail required to fix the specified XML code.
    /// </summary>
    /// <param name="xml">The XML code.</param>
    /// <param name="nsmgr">The optional XML namespaces manager to use
    /// to resolve namespace prefixes in <paramref name="xml"/>.</param>
    /// <returns>tail (empty string if no fix required)</returns>
    public static string GetTail(string? xml,
        XmlNamespaceManager? nsmgr = null)
    {
        if (string.IsNullOrWhiteSpace(xml)) return "";

        Stack<XName> tags = new();

        int i = 0;
        while (i < xml.Length)
        {
            i = xml.IndexOf('<', i);
            if (i == -1) break;

            XmlTag? tag = XmlTag.Parse(xml, i, false, nsmgr);
            if (tag != null)
            {
                switch (tag.Type)
                {
                    case TagType.Open:
                        tags.Push(tag.Name!);
                        break;
                    case TagType.Close:
                        if (tags.Peek() == tag.Name) tags.Pop();
                        break;
                }
                i += tag.Length;
            }
            else
            {
                i++;
            }
        }

        StringBuilder sb = new();
        while (tags.Count > 0)
            sb.Append("</").Append(tags.Pop()).Append('>');

        return sb.ToString();
    }
}
