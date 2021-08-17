using System.Linq;
using System.Xml.Linq;
using Corpus.Core.Reading;
using Xunit;

namespace Pythia.Core.Test.Reading
{
    public sealed class XmlPathTest
    {
        [Fact]
        public void Parse_1Step_Ok()
        {
            XmlPath path = XmlPath.Parse("/div");

            Assert.Single(path.Steps);
            Assert.Equal("/div", path.Steps[0].ToString());
            Assert.Null(path.ValueSteps);
        }

        [Fact]
        public void Parse_2Steps_Ok()
        {
            XmlPath path = XmlPath.Parse("/div/p");

            Assert.Equal(2, path.Steps.Length);
            Assert.Equal("/div", path.Steps[0].ToString());
            Assert.Equal("/p", path.Steps[1].ToString());
            Assert.Null(path.ValueSteps);
        }

        [Fact]
        public void Parse_1StepWithValues_Ok()
        {
            XmlPath path = XmlPath.Parse("/div /@n /head$");

            Assert.Single(path.Steps);
            Assert.Equal("/div", path.Steps[0].ToString());
            Assert.NotNull(path.ValueSteps);
            Assert.Equal(2, path.ValueSteps.Length);
            Assert.Single(path.ValueSteps[0]);
            Assert.Single(path.ValueSteps[1]);
            Assert.Equal("/div /@n /head$", path.ToString());
        }

        [Fact]
        public void WalkUp_NoMatch_Null()
        {
            XElement start;
            XElement root = new XElement("div",
                new XElement("div",
                    start = new XElement("p")));
            XmlPath path = XmlPath.Parse("/body/p/");

            XElement target = path.WalkUp(start);

            Assert.Null(target);
        }

        [Fact]
        public void WalkUp_Match_Ok()
        {
            XElement start;
            XElement root = new XElement("div",
                new XElement("div",
                    start = new XElement("p")));
            XmlPath path = XmlPath.Parse("/div/p/");

            XElement target = path.WalkUp(start);

            Assert.NotNull(target);
            Assert.Same(root.Element("div"), target);
        }

        [Fact]
        public void WalkUp_MatchWithIndirect_Ok()
        {
            XElement start;
            XElement root = new XElement("body",
                new XElement("div",
                    new XElement("lg",
                        start = new XElement("l"))));
            XmlPath path = XmlPath.Parse("/div//l");

            XElement target = path.WalkUp(start);

            Assert.NotNull(target);
            Assert.Same(root.Element("div"), target);
        }

        [Fact]
        public void WalkDown_NoMatch_Null()
        {
            XElement root = new XElement("div",
                new XElement("div",
                    new XElement("p")));
            XmlPath path = XmlPath.Parse("/body/p/");

            XElement target = path.WalkDown(root);

            Assert.Null(target);
        }

        [Fact]
        public void WalkDown_Match_Ok()
        {
            XElement root = new XElement("body",
                new XElement("div",
                    new XElement("lg",
                        new XElement("l"))));
            XmlPath path = XmlPath.Parse("/body/div");

            XElement target = path.WalkDown(root);

            Assert.NotNull(target);
            Assert.Same(root.Element("div"), target);
        }

        [Fact]
        public void WalkDown_MatchWithIndirect_Ok()
        {
            XElement root = new XElement("body",
                new XElement("div",
                    new XElement("lg",
                        new XElement("l"))));
            XmlPath path = XmlPath.Parse("/body/div//l");

            XElement target = path.WalkDown(root);

            Assert.NotNull(target);
            Assert.Same(root.Descendants("l").First(), target);
        }

        [Fact]
        public void GetValue_NoValuePath_Null()
        {
            XElement root = new XElement("body",
                new XElement("div",
                    new XAttribute("n", "1"),
                    new XElement("head", "Chapter 1")));
            XmlPath path = XmlPath.Parse("/body/div");

            string s = path.GetValue(root);

            Assert.Null(s);
        }

        [Fact]
        public void GetValue_ElementValuePath_Ok()
        {
            XElement root = new XElement("body",
                new XElement("div",
                    new XAttribute("n", "1"),
                    new XElement("head", "Chapter 1")));
            XmlPath path = XmlPath.Parse("/body/div /head");

            string s = path.GetValue(root.Element("div"));

            Assert.Equal("Chapter 1", s);
        }

        [Fact]
        public void GetValue_AttributeValuePath_Ok()
        {
            XElement root = new XElement("body",
                new XElement("div",
                    new XAttribute("n", "1"),
                    new XElement("head", "Chapter 1")));
            XmlPath path = XmlPath.Parse("/body/div /@n");

            string s = path.GetValue(root.Element("div"));

            Assert.Equal("1", s);
        }

        [Fact]
        public void GetValue_MultipleValuePaths_Ok()
        {
            XElement root = new XElement("body",
                new XElement("div",
                    new XAttribute("n", "1"),
                    new XElement("head", "Chapter 1")));
            XmlPath path = XmlPath.Parse("/body/div /@n /head");

            string s = path.GetValue(root.Element("div"));

            Assert.Equal("1 - Chapter 1", s);
        }

        [Fact]
        public void GetValue_MultipleValuePathsTerminal_Ok()
        {
            XElement root = new XElement("body",
                new XElement("div",
                    new XAttribute("n", "1"),
                    new XElement("head", "Chapter 1")));
            XmlPath path = XmlPath.Parse("/body/div /@n$ /head");

            string s = path.GetValue(root.Element("div"));

            Assert.Equal("1", s);
        }
    }
}
