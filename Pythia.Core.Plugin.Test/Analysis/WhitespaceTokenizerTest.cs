using System.IO;
using Pythia.Core.Plugin.Analysis;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis
{
    public sealed class WhitespaceTokenizerTest
    {
        [Fact]
        public void Next_Empty_False()
        {
            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer();
            tokenizer.Start(new StringReader(""), 1);

            Assert.False(tokenizer.Next());
        }

        [Fact]
        public void Next_Whitespace_False()
        {
            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer();
            tokenizer.Start(new StringReader("  \r\n "), 1);

            Assert.False(tokenizer.Next());
        }

        [Fact]
        public void Next_TextWithSpace_Ok()
        {
            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer();
            tokenizer.Start(new StringReader("alpha beta"), 1);

            Assert.True(tokenizer.Next());

            Token token = tokenizer.CurrentToken;
            Assert.Equal(1, token.Position);
            Assert.Equal(0, token.Index);
            Assert.Equal(5, token.Length);
            Assert.Equal("alpha", token.Value);

            Assert.True(tokenizer.Next());

            token = tokenizer.CurrentToken;
            Assert.Equal(2, token.Position);
            Assert.Equal(6, token.Index);
            Assert.Equal(4, token.Length);
            Assert.Equal("beta", token.Value);

            Assert.False(tokenizer.Next());
        }
    }
}
