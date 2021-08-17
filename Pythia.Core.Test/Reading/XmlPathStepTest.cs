using Corpus.Core.Reading;
using Xunit;

namespace Pythia.Core.Test.Reading
{
    public class XmlPathStepTest
    {
        [Fact]
        public void Parse_Name_Ok()
        {
            XmlPathStep step = XmlPathStep.Parse("/div");

            Assert.Equal("div", step.Name);
            Assert.False(step.IsIndirect);
            Assert.Null(step.Attribute);
            Assert.False(step.IsTerminalValue);
        }

        [Fact]
        public void Parse_NameIndirect_Ok()
        {
            XmlPathStep step = XmlPathStep.Parse("//div");

            Assert.Equal("div", step.Name);
            Assert.True(step.IsIndirect);
            Assert.Null(step.Attribute);
            Assert.False(step.IsTerminalValue);
        }

        [Fact]
        public void Parse_NameAttribute_Ok()
        {
            XmlPathStep step = XmlPathStep.Parse("/div@n");

            Assert.Equal("div", step.Name);
            Assert.False(step.IsIndirect);
            Assert.Equal("n", step.Attribute);
            Assert.False(step.IsTerminalValue);
        }

        [Fact]
        public void Parse_NameAttributeTerminal_Ok()
        {
            XmlPathStep step = XmlPathStep.Parse("/div@n$");

            Assert.Equal("div", step.Name);
            Assert.False(step.IsIndirect);
            Assert.Equal("n", step.Attribute);
            Assert.True(step.IsTerminalValue);
        }

        [Fact]
        public void Parse_Attribute_Ok()
        {
            XmlPathStep step = XmlPathStep.Parse("/@n");

            Assert.Null(step.Name);
            Assert.False(step.IsIndirect);
            Assert.Equal("n", step.Attribute);
            Assert.False(step.IsTerminalValue);
        }

        [Fact]
        public void Parse_AttributeTerminal_Ok()
        {
            XmlPathStep step = XmlPathStep.Parse("/@n$");

            Assert.Null(step.Name);
            Assert.False(step.IsIndirect);
            Assert.Equal("n", step.Attribute);
            Assert.True(step.IsTerminalValue);
        }
    }
}
