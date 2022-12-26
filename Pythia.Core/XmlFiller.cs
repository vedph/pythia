using Fusi.Xml;
using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Pythia.Core;

/// <summary>
/// XML document filler. This is a utility class used to isolate an
/// XML element with all its descendants in the context of its document,
/// which gets space-filled except for the target XML element.
/// </summary>
public static class XmlFiller
{
    /// <summary>
    /// Fills all the tags (i.e. text between <c>&lt;</c> and <c>&gt;</c>)
    /// with spaces.
    /// </summary>
    /// <param name="xml">The XML code.</param>
    /// <exception cref="ArgumentNullException">xml</exception>
    public static void FillTags(StringBuilder xml)
    {
        if (xml == null) throw new ArgumentNullException(nameof(xml));

        int i = 0;
        while (i < xml.Length)
        {
            if (xml[i] == '<')
            {
                int j = i;
                while (j < xml.Length && xml[j] != '>') xml[j++] = ' ';
                if (j < xml.Length) xml[j++] = ' ';
                i = j;
            }
            else
            {
                i++;
            }
        }
    }

    /// <summary>
    /// Gets an XML document from the specified XML code, where only
    /// the target element and all its descendants have been kept, while
    /// the rest of the document has been space-filled.
    /// </summary>
    /// <param name="xml">The XML document.</param>
    /// <param name="targetXPath">The XPath to the target element, which
    /// will be kept unchanged, while the rest of the file will be filled.
    /// Please notice that this expression should refer to a single matching
    /// element node.</param>
    /// <param name="nsmgr">The optional namespaces manager.</param>
    /// <returns>
    /// The filled XML document, or null if the target element was not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">xml or targetPath</exception>
    public static string? GetFilledXml(string xml, string targetXPath,
        XmlNamespaceManager? nsmgr = null)
    {
        if (xml == null) throw new ArgumentNullException(nameof(xml));
        if (targetXPath == null)
            throw new ArgumentNullException(nameof(targetXPath));

        XDocument doc = XDocument.Parse(xml,
            LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        if (doc.Root == null) return null;

        XElement? target = nsmgr != null
            ? doc.XPathSelectElement(targetXPath, nsmgr)
            : doc.XPathSelectElement(targetXPath);

        if (target == null) return null;

        IXmlLineInfo info = target;
        int start = OffsetHelper.GetOffset(xml, info.LineNumber,
            info.LinePosition - 1);
        int end = OffsetHelper.GetElementEndOffset(xml, start);

        StringBuilder sb = new(xml);
        for (int i = 0; i < start; i++) sb[i] = ' ';
        for (int i = end; i < xml.Length; i++) sb[i] = ' ';
        return sb.ToString();
    }
}
