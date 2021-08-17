using Corpus.Core.Reading;
using Pythia.Core.Plugin.Analysis;
using System.Xml.Linq;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis
{
    public sealed class XmlFillerTest
    {
        [Fact]
        public void GetFilledXml_Ok()
        {
            XDocument doc = new XDocument(
                new XElement("TEI",
                    new XElement("teiHeader"),
                    new XElement("text",
                        new XElement("body",
                            new XElement("p", "Hello!")))));

            string xml = doc.ToString(SaveOptions.DisableFormatting);
            string filled = XmlFiller.GetFilledXml(xml, XmlPath.Parse("/TEI//body"));
            Assert.Equal(xml.Length, filled.Length);
            Assert.Equal("                        " +
                "<body><p>Hello!</p></body>" +
                "             ",
                filled);
        }
    }
}
