using System.Xml.Linq;
using Corpus.Core.Reading;
using Xunit;

namespace Pythia.Core.Test.Reading
{
    public sealed class XmlElementNumbererTest
    {
        [Fact]
        public void Filter_NoNumerable_Nope()
        {
            XmlElementNumberer numberer =
                new XmlElementNumberer(new[] { "gc:A", "sns:1" });
            const string xml = "<item><lemma>hello</lemma></item>";
            XElement xe = XElement.Parse(xml, LoadOptions.PreserveWhitespace);

            numberer.Number(xe);

            Assert.Equal(xml, xe.ToString(SaveOptions.DisableFormatting));
        }

        [Fact]
        public void Filter_GcSnsSingle_Nope()
        {
            XmlElementNumberer numberer =
                new XmlElementNumberer(new[] { "gc:A", "sns:1" });
            const string xml = "<item><lemma>hello</lemma><gc><sns>A1</sns></gc></item>";
            XElement element = XElement.Parse(xml, LoadOptions.PreserveWhitespace);

            numberer.Number(element);

            Assert.Equal(xml, element.ToString(SaveOptions.DisableFormatting));
        }

        [Fact]
        public void Filter_Gc1Sns2_Applied()
        {
            XmlElementNumberer numberer =
                new XmlElementNumberer(new[] { "gc:A", "sns:1" });
            XElement element = XElement.Parse("<item><lemma>hello</lemma><gc><sns>A1</sns>" +
                                         "<sns>A2</sns></gc></item>", LoadOptions.PreserveWhitespace);

            numberer.Number(element);

            Assert.Equal("<item><lemma>hello</lemma><gc>" +
                            "<sns _r-sns=\"1\">A1</sns>" +
                            "<sns _r-sns=\"2\">A2</sns>" +
                            "</gc></item>",
                element.ToString(SaveOptions.DisableFormatting));
        }

        [Fact]
        public void Filter_Gc2Sns2_Applied()
        {
            XmlElementNumberer numberer =
                new XmlElementNumberer(new[] { "gc:A", "sns:1" });
            XElement element = XElement.Parse("<item><lemma>hello</lemma>" +
                "<gc><sns>A1</sns><sns>A2</sns></gc>" +
                "<gc><sns>B1</sns><sns>B2</sns></gc>" +
                "</item>", LoadOptions.PreserveWhitespace);

            numberer.Number(element);

            Assert.Equal("<item><lemma>hello</lemma>" +
                            "<gc _r-gc=\"A\">" +
                            "<sns _r-sns=\"1\">A1</sns>" +
                            "<sns _r-sns=\"2\">A2</sns>" +
                            "</gc>" +
                            "<gc _r-gc=\"B\">" +
                            "<sns _r-sns=\"1\">B1</sns>" +
                            "<sns _r-sns=\"2\">B2</sns>" +
                            "</gc>" +
                            "</item>",
                element.ToString(SaveOptions.DisableFormatting));
        }

        [Fact]
        public void Filter_LgLineStep5_Applied()
        {
            XmlElementNumberer numberer =
                new XmlElementNumberer(new[] { "lg:a", "l:1:5" });
            const string xml = "<body>" +
                                "<lg><l>one</l>" +
                                "<l>two</l>" +
                                "<l>three</l>" +
                                "<l>four</l>" +
                                "<l>five</l></lg>" +
                                "<lg><l>one</l>" +
                                "<l>two</l></lg>" +
                                "</body>";
            XElement element = XElement.Parse(xml, LoadOptions.PreserveWhitespace);

            numberer.Number(element);

            Assert.Equal("<body>" +
                         "<lg _r-lg=\"a\"><l _r-l=\"1\">one</l>" +
                         "<l>two</l>" +
                         "<l>three</l>" +
                         "<l>four</l>" +
                         "<l _r-l=\"5\">five</l></lg>" +
                         "<lg _r-lg=\"b\"><l _r-l=\"1\">one</l>" +
                         "<l>two</l></lg>" +
                         "</body>", element.ToString(SaveOptions.DisableFormatting));
        }
    }
}
