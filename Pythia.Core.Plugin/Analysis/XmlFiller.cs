using Corpus.Core.Reading;
using Fusi.Xml;
using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// XML document filler. This is a utility class used to isolate an
    /// XML element with all its descendants in the context of its document,
    /// which gets space-filled except for the target XML element.
    /// </summary>
    public static class XmlFiller
    {
        /// <summary>
        /// Gets an XML document from the specified XML code, where only
        /// the target element and all its descendants have been kept, while
        /// the rest of the document has been space-filled.
        /// </summary>
        /// <param name="xml">The XML document.</param>
        /// <param name="targetPath">The path to the target element, which
        /// will be kept unchanged, while the rest of the file will be filled.
        /// </param>
        /// <returns>
        /// The filled XML document, or null if the target element was not found.
        /// </returns>
        /// <exception cref="ArgumentNullException">xml or targetPath</exception>
        public static string GetFilledXml(string xml, XmlPath targetPath)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            if (targetPath == null)
                throw new ArgumentNullException(nameof(targetPath));

            XDocument doc = XDocument.Parse(xml,
                LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
            if (doc.Root == null) return null;

            XElement target = targetPath.WalkDown(doc.Root);
            if (target == null) return null;

            IXmlLineInfo info = target;
            int start = OffsetHelper.GetOffset(xml, info.LineNumber,
                info.LinePosition - 1);
            int end = OffsetHelper.GetElementEndOffset(xml, start);

            StringBuilder sb = new StringBuilder(xml);
            for (int i = 0; i < start; i++) sb[i] = ' ';
            for (int i = end; i < xml.Length; i++) sb[i] = ' ';
            return sb.ToString();
        }
    }
}
