using System.IO;
using System.Reflection;
using System.Text;
using Corpus.Core.Reading;
using Xunit;

namespace Pythia.Core.Test.Reading
{
    public sealed class TextMapNodeTest
    {
        private const string SAMPLE_MAP = "Pythia.Core.Test.Assets.SampleMap.txt";

        private static TextMapNode CreateTextMap()
        {
            using (StreamReader reader = new(
                typeof(TextMapNodeTest).GetTypeInfo().Assembly
                .GetManifestResourceStream(SAMPLE_MAP), Encoding.UTF8))
            {
                return TextMapNode.ParseTree(reader);
            }
        }

        [Fact]
        public void GetPath_Root_0()
        {
            TextMapNode root = CreateTextMap();
            string sPath = root.GetPath();
            Assert.NotNull(sPath);
            Assert.Equal("0", sPath);
        }

        [Fact]
        public void GetPath_Alpha_0_0()
        {
            TextMapNode root = CreateTextMap();

            TextMapNode node = root.Children[0];
            string path = node.GetPath();
            Assert.NotNull(path);
            Assert.Equal("0.0", path);
        }

        [Fact]
        public void GetPath_Beta_0_1()
        {
            TextMapNode root = CreateTextMap();

            TextMapNode node = root.Children[1];
            string path = node.GetPath();
            Assert.NotNull(path);
            Assert.Equal("0.1", path);
        }

        [Fact]
        public void GetPath_Gamma_0_2()
        {
            TextMapNode root = CreateTextMap();

            TextMapNode node = root.Children[2];
            string path = node.GetPath();
            Assert.NotNull(path);
            Assert.Equal("0.2", path);
        }

        [Fact]
        public void GetPath_Alpha1_0_0_0()
        {
            TextMapNode root = CreateTextMap();

            TextMapNode node = root.Children[0].Children[0];
            string path = node.GetPath();
            Assert.NotNull(path);
            Assert.Equal("0.0.0", path);
        }

        [Fact]
        public void GetPath_Alpha2_0_0_1()
        {
            TextMapNode root = CreateTextMap();

            TextMapNode node = root.Children[0].Children[1];
            string path = node.GetPath();
            Assert.NotNull(path);
            Assert.Equal("0.0.1", path);
        }

        [Fact]
        public void GetPath_Beta1_0_1_0()
        {
            TextMapNode root = CreateTextMap();

            TextMapNode node = root.Children[1].Children[0];
            string path = node.GetPath();
            Assert.NotNull(path);
            Assert.Equal("0.1.0", path);
        }

        // TODO gamma and delta

        [Fact]
        public void GetDescendant_1_0_Beta1()
        {
            TextMapNode root = CreateTextMap();
            TextMapNode node = root.GetDescendant("1.0");
            Assert.NotNull(node);
            Assert.Equal("beta-1", node.Label);
        }

        [Fact]
        public void GetDescendant_1_1_Null()
        {
            TextMapNode root = CreateTextMap();
            TextMapNode node = root.GetDescendant("1.1");
            Assert.Null(node);
        }

        [Fact]
        public void DumpTree_Parsed()
        {
            TextMapNode root = CreateTextMap();
            string dump = root.DumpTree();

            using (StreamReader reader = new(
                typeof(TextMapNodeTest).GetTypeInfo().Assembly.GetManifestResourceStream
                    (SAMPLE_MAP), Encoding.UTF8))
            {
                string source = reader.ReadToEnd();
                Assert.Equal(source, dump);
            }
        }

        [Fact]
        public void Visit_NodesInOrder()
        {
            TextMapNode root = CreateTextMap();
            int expected = 0;
            root.Visit(n =>
            {
                Assert.Equal(expected, n.StartIndex);
                expected += 100;
                return true;
            });
        }
    }
}
