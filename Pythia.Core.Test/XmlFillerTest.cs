using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Pythia.Core.Test
{
    public sealed class XmlFillerTest
    {
        [Fact]
        public void GetFilledXml_Ok()
        {
            XDocument doc = new(
                new XElement("TEI",
                    new XElement("teiHeader"),
                    new XElement("text",
                        new XElement("body",
                            new XElement("p", "Hello!")))));

            string xml = doc.ToString(SaveOptions.DisableFormatting);
            string filled = XmlFiller.GetFilledXml(xml, "/TEI//body")!;
            Assert.Equal(xml.Length, filled.Length);
            Assert.Equal("                        " +
                "<body><p>Hello!</p></body>" +
                "             ",
                filled);
        }

        [Fact]
        public void GetFilledXmlWithNs_Ok()
        {
            XNamespace ns = "http://www.tei-c.org/ns/1.0";

            XDocument doc = new(
                new XElement(ns + "TEI",
                    new XElement(ns + "teiHeader"),
                    new XElement(ns + "text",
                        new XElement(ns + "body",
                            new XElement(ns + "p", "Hello!")))));
            string xml = doc.ToString(SaveOptions.DisableFormatting);

            XmlNamespaceManager nsmgr = new(new NameTable());
            nsmgr.AddNamespace("tei", ns.NamespaceName);

            string filled = XmlFiller.GetFilledXml(xml, "/tei:TEI//tei:body",
                nsmgr)!;
            Assert.Equal(xml.Length, filled.Length);
            Assert.Equal("                                                            " +
                "<body><p>Hello!</p></body>" +
                "             ",
                filled);
        }
    }
}
