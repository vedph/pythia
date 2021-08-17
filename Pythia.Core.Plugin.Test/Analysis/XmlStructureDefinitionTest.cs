using Corpus.Core;
using Pythia.Core.Plugin.Analysis;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis
{
    public sealed class XmlStructureDefinitionTest
    {
        [Fact]
        public void Parse_NamePath_Ok()
        {
            XmlStructureDefinition def = XmlStructureDefinition.Parse("name=//div");

            Assert.Equal("name", def.Name);
            Assert.Equal("//div", def.Path.ToString());
            Assert.Equal(AttributeType.Text, def.Type);
            Assert.Null(def.TokenTargetName);
            Assert.Null(def.TokenTargetValue);
        }

        [Fact]
        public void Parse_NamePathValue_Ok()
        {
            XmlStructureDefinition def = XmlStructureDefinition.Parse("name=//div @n");

            Assert.Equal("name", def.Name);
            Assert.Equal("//div /@n", def.Path.ToString());
            Assert.Equal(AttributeType.Text, def.Type);
            Assert.Null(def.TokenTargetName);
            Assert.Null(def.TokenTargetValue);
        }

        [Fact]
        public void Parse_NamePathValueN_Ok()
        {
            XmlStructureDefinition def = XmlStructureDefinition.Parse("name#=//div @n");

            Assert.Equal("name", def.Name);
            Assert.Equal("//div /@n", def.Path.ToString());
            Assert.Equal(AttributeType.Number, def.Type);
            Assert.Null(def.TokenTargetName);
            Assert.Null(def.TokenTargetValue);
        }

        [Fact]
        public void Parse_NamePathValueTerminal_Ok()
        {
            XmlStructureDefinition def = XmlStructureDefinition.Parse("name=//div @n$");

            Assert.Equal("name", def.Name);
            Assert.Equal("//div /@n$", def.Path.ToString());
            Assert.Equal(AttributeType.Text, def.Type);
            Assert.Null(def.TokenTargetName);
            Assert.Null(def.TokenTargetValue);
        }

        [Fact]
        public void Parse_NamePathValues_Ok()
        {
            XmlStructureDefinition def = XmlStructureDefinition.Parse("name=//div @n head");

            Assert.Equal("name", def.Name);
            Assert.Equal("//div /@n /head", def.Path.ToString());
            Assert.Equal(AttributeType.Text, def.Type);
            Assert.Null(def.TokenTargetName);
            Assert.Null(def.TokenTargetValue);
        }

        [Fact]
        public void Parse_NamePathValuesTerminal_Ok()
        {
            XmlStructureDefinition def = XmlStructureDefinition.Parse("name=//div @n$ head$");

            Assert.Equal("name", def.Name);
            Assert.Equal("//div /@n$ /head$", def.Path.ToString());
            Assert.Equal(AttributeType.Text, def.Type);
            Assert.Null(def.TokenTargetName);
            Assert.Null(def.TokenTargetValue);
        }

        [Fact]
        public void Parse_NamePathNValues_Ok()
        {
            XmlStructureDefinition def = XmlStructureDefinition.Parse("name#=//div @n head");

            Assert.Equal("name", def.Name);
            Assert.Equal("//div /@n /head", def.Path.ToString());
            Assert.Equal(AttributeType.Number, def.Type);
            Assert.Null(def.TokenTargetName);
            Assert.Null(def.TokenTargetValue);
        }

        [Fact]
        public void Parse_NameWithTargetName_Ok()
        {
            XmlStructureDefinition def = XmlStructureDefinition.Parse("name:x=//sound");

            Assert.Equal("name", def.Name);
            Assert.Equal("//sound", def.Path.ToString());
            Assert.Equal(AttributeType.Text, def.Type);

            Assert.Equal("x", def.TokenTargetName);
            Assert.Null(def.TokenTargetValue);
        }

        [Fact]
        public void Parse_NameWithTargetNameAndValue_Ok()
        {
            XmlStructureDefinition def = XmlStructureDefinition.Parse("name:x:snd=//sound");

            Assert.Equal("name", def.Name);
            Assert.Equal("//sound", def.Path.ToString());
            Assert.Equal(AttributeType.Text, def.Type);

            Assert.Equal("x", def.TokenTargetName);
            Assert.Equal("snd", def.TokenTargetValue);
        }
    }
}
