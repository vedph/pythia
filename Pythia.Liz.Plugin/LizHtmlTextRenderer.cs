using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Corpus.Core.Reading;
using Fusi.Tools;
using Fusi.Tools.Configuration;

namespace Pythia.Liz.Plugin
{
    /// <summary>
    /// LIZ TEI XML to HTML renderer.
    /// </summary>
    /// <seealso cref="ITextRenderer" />
    [Tag("text-renderer.liz-html")]
    public sealed class LizHtmlTextRenderer : XmlToHtmlTextRendererBase
    {
        private readonly HashSet<XName> _allowedInlineElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="LizHtmlTextRenderer"/> class.
        /// </summary>
        public LizHtmlTextRenderer()
        {
            _allowedInlineElements = new HashSet<XName>(
                new XName[]
                {
                    "hi", "quote", "persName", "geogName"
                }
            );
        }

        private static string LoadResourceText(string name)
        {
            Assembly asm = typeof(LizHtmlTextRenderer).GetTypeInfo().Assembly;
            using StreamReader reader = new(
                asm.GetManifestResourceStream($"Pythia.Liz.Plugin.Assets.{name}")!,
                Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static void RenderTeiHeader(XDocument doc, StringBuilder sb)
        {
            if (doc.Root?.Element("teiHeader") == null) return;

            // TEI/teiHeader/fileDesc/titleStmt/author
            // TEI/teiHeader/fileDesc/titleStmt/title
            // TEI/teiHeader/fileDesc/titleStmt/date
            XElement? titleElem = doc.Root.Element("teiHeader")
                ?.Element("fileDesc")?.Element("titleStmt");
            string? author = titleElem?.Element("author")?.Value;
            string? title = titleElem?.Element("title")?.Value;
            string? date = titleElem?.Element("date")?.Value;

            if (author != null || title != null)
            {
                sb.Append("<h1>");
                if (author != null)
                {
                    sb.Append(author);
                    if (title != null) sb.Append(" - ");
                }
                if (title != null) sb.Append(title);
                sb.Append("</h1>");
            }
            if (date != null)
                sb.Append("<p class=\"subh\">").Append(date).Append("</p>");
        }

        /// <summary>
        /// Render the document's header.
        /// </summary>
        /// <param name="doc">source XML document</param>
        /// <param name="sb">Target string builder</param>
        protected override void RenderHeader(XDocument doc, StringBuilder sb)
        {
            sb.Append(LoadResourceText("Header.txt"));
            string? title =
                doc.Root?.Element("teiHeader")?.Element("fileDesc")?
                    .Element("titleStmt")?.Element("title")?.Value;
            sb.Replace("{{title}}", title ?? "");

            RenderTeiHeader(doc, sb);
        }

        /// <summary>
        /// Render the specified XML element to the specified Target element.
        /// </summary>
        /// <param name="element">source XML element</param>
        protected override void RenderElement(XElement element)
        {
            // div => div
            // head => hN (up to h6)
            // lg => div class="lg"
            // l => tr inside 2-cells table (left cell for @n numbers)
            // p => p with class="ind" if @rend=indent 
            // speaker => span class="spk"
            // hi rend="ibu" => span class="r-..." where ... is the rend value
            // note => div class="note"

            XElement targetToRestore = Target!;

            // any element other than hi/l closes an opened table, as tables
            // are used only to render l elements with their numbers
            if (!_allowedInlineElements.Contains(element.Name)
                && element.Name.LocalName != "l")
            {
                XElement? table = Target!.AncestorsAndSelf("table").FirstOrDefault();
                if (table != null) Target = table.Parent;
            }

            // any element other than allowed inline closes an opened hN
            if (!_allowedInlineElements.Contains(element.Name))
                SetTargetToParentOf(e => Regex.IsMatch(e.Name.LocalName, @"^h\d$"));

            // defensive
            if (Target == null) return;

            XElement xe;
            switch (element.Name.LocalName)
            {
                case "div":
                    AddAndSetAsTarget("div");
                    break;

                case "head":
                    int n = Target.AncestorsAndSelf("div").Count() + 1;
                    xe = new XElement($"h{(n < 7 ? n : 6)}");
                    AddAndSetAsTarget(xe);
                    break;

                case "lg":
                    // ensure that we close a previous div@class=lg
                    SetTargetToParentOf(e => e.Name.LocalName == "div" &&
                        e.ReadOptionalAttribute("class", null) == "lg");
                    AddAndSetAsTarget(new XElement("div",
                        new XAttribute("class", "lg")));
                    break;

                case "l":
                    XElement? xeTBody = Target.AncestorsAndSelf("tbody")
                        .FirstOrDefault();
                    if (xeTBody == null)
                    {
                        xe = new XElement("table",
                            new XElement("thead",
                                new XElement("tr",
                                    new XElement("th",
                                        new XAttribute("class", "lncol")),
                                    new XElement("th"))),
                            new XElement("tbody"));
                        Target.Add(xe);
                        targetToRestore = Target = xe.Element("tbody")!;
                    }
                    else
                    {
                        Target = xeTBody;
                    }

                    XElement xeTr = new("tr");
                    if (element.Attribute("n") != null)
                    {
                        xeTr.Add(new XElement("td",
                            new XAttribute("class", "ln"),
                            element.Attribute("n")!.Value));
                    }
                    else
                        xeTr.Add(new XElement("td"));
                    xeTr.Add(xe = new XElement("td"));
                    Target?.Add(xeTr);
                    Target = xe;
                    break;

                case "p":
                    xe = new XElement("p");
                    if (element.Attribute("rend") != null)
                        xe.SetAttributeValue("class", "ind");
                    AddAndSetAsTarget(xe);
                    break;

                case "speaker":
                    xe = new XElement("span", new XAttribute("class", "spk"));
                    AddAndSetAsTarget(xe);
                    break;

                case "hi":
                    xe = new XElement("span");
                    string? rend = element.ReadOptionalAttribute("rend", null);
                    if (rend != null)
                    {
                        // ensure that rend value sorts its chars alphabetically,
                        // as it might contain combinations (e.g. bi, and not ib)
                        rend = new string(rend.OrderBy(c => c).ToArray());
                        xe.SetAttributeValue("class", "r-" + rend);
                    }
                    AddAndSetAsTarget(xe);
                    break;

                case "quote":
                    AddAndSetAsTarget(new XElement("em"));
                    break;

                case "note":
                    xe = new XElement("div",
                        new XAttribute("class", "note"));
                    AddAndSetAsTarget(xe);
                    break;
            }

            foreach (XNode child in element.Nodes())
                RenderNode(child);

            if (targetToRestore != null) Target = targetToRestore;
        }
    }
}
