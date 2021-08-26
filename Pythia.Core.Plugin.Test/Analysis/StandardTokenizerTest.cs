using System.IO;
using Pythia.Core.Analysis;
using Pythia.Core.Plugin.Analysis;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis
{
    public sealed class StandardTokenizerTest
    {
        [Fact]
        public void Next_Empty_False()
        {
            ITokenizer tokenizer = new StandardTokenizer();
            tokenizer.Start(new StringReader(""), 1);

            Assert.False(tokenizer.Next());
        }

        [Fact]
        public void Next_WhitespaceOnly_False()
        {
            ITokenizer tokenizer = new StandardTokenizer();
            tokenizer.Start(new StringReader("  "), 1);

            Assert.False(tokenizer.Next());
        }

        [Fact]
        public void Next_Simple_Ok()
        {
            ITokenizer tokenizer = new StandardTokenizer();
            tokenizer.Start(new StringReader("alpha"), 1);

            Assert.True(tokenizer.Next());

            Assert.Equal(0, tokenizer.CurrentToken.Index);
            Assert.Equal(5, tokenizer.CurrentToken.Length);
            Assert.Equal(1, tokenizer.CurrentToken.Position);
            Assert.Equal("alpha", tokenizer.CurrentToken.Value);
        }

        [Fact]
        public void Next_SimpleWithNonLetters_Ok()
        {
            ITokenizer tokenizer = new StandardTokenizer();
            tokenizer.Start(new StringReader("al[p]ha"), 1);

            Assert.True(tokenizer.Next());

            Assert.Equal(0, tokenizer.CurrentToken.Index);
            Assert.Equal(7, tokenizer.CurrentToken.Length);
            Assert.Equal(1, tokenizer.CurrentToken.Position);
            Assert.Equal("al[p]ha", tokenizer.CurrentToken.Value);
        }

        [Fact]
        public void Next_Two_Ok()
        {
            ITokenizer tokenizer = new StandardTokenizer();
            tokenizer.Start(new StringReader("alpha beta"), 1);

            // alpha
            Assert.True(tokenizer.Next());
            Assert.Equal(0, tokenizer.CurrentToken.Index);
            Assert.Equal(5, tokenizer.CurrentToken.Length);
            Assert.Equal(1, tokenizer.CurrentToken.Position);
            Assert.Equal("alpha", tokenizer.CurrentToken.Value);
            // beta
            Assert.True(tokenizer.Next());
            Assert.Equal(6, tokenizer.CurrentToken.Index);
            Assert.Equal(4, tokenizer.CurrentToken.Length);
            Assert.Equal(2, tokenizer.CurrentToken.Position);
            Assert.Equal("beta", tokenizer.CurrentToken.Value);
            // end
            Assert.False(tokenizer.Next());
        }

        [Fact]
        public void Next_TwoWithRightApostrophe_Ok()
        {
            ITokenizer tokenizer = new StandardTokenizer();
            tokenizer.Start(new StringReader("l'incenso"), 1);

            // l'
            Assert.True(tokenizer.Next());
            Assert.Equal(0, tokenizer.CurrentToken.Index);
            Assert.Equal(2, tokenizer.CurrentToken.Length);
            Assert.Equal(1, tokenizer.CurrentToken.Position);
            Assert.Equal("l'", tokenizer.CurrentToken.Value);
            // incenso
            Assert.True(tokenizer.Next());
            Assert.Equal(2, tokenizer.CurrentToken.Index);
            Assert.Equal(7, tokenizer.CurrentToken.Length);
            Assert.Equal(2, tokenizer.CurrentToken.Position);
            Assert.Equal("incenso", tokenizer.CurrentToken.Value);
            // end
            Assert.False(tokenizer.Next());
        }

        [Fact]
        public void Next_TwoWithLeftApostrophe_Ok()
        {
            ITokenizer tokenizer = new StandardTokenizer();
            tokenizer.Start(new StringReader("'l ponerò"), 1);

            // 'l
            Assert.True(tokenizer.Next());
            Assert.Equal(0, tokenizer.CurrentToken.Index);
            Assert.Equal(2, tokenizer.CurrentToken.Length);
            Assert.Equal(1, tokenizer.CurrentToken.Position);
            Assert.Equal("'l", tokenizer.CurrentToken.Value);
            // ponerò
            Assert.True(tokenizer.Next());
            Assert.Equal(3, tokenizer.CurrentToken.Index);
            Assert.Equal(6, tokenizer.CurrentToken.Length);
            Assert.Equal(2, tokenizer.CurrentToken.Position);
            Assert.Equal("ponerò", tokenizer.CurrentToken.Value);
            // end
            Assert.False(tokenizer.Next());
        }

        [Fact]
        public void Next_FilteredOutToken_Ok()
        {
            ITokenizer tokenizer = new StandardTokenizer();
            tokenizer.Filters.Add(new LoAlnumAposTokenFilter());
            //                                012345678901
            tokenizer.Start(new StringReader("alpha - beta"), 1);

            // alpha
            Assert.True(tokenizer.Next());
            Assert.Equal(0, tokenizer.CurrentToken.Index);
            Assert.Equal(5, tokenizer.CurrentToken.Length);
            Assert.Equal(1, tokenizer.CurrentToken.Position);
            Assert.Equal("alpha", tokenizer.CurrentToken.Value);
            // beta
            Assert.True(tokenizer.Next());
            Assert.Equal(8, tokenizer.CurrentToken.Index);
            Assert.Equal(4, tokenizer.CurrentToken.Length);
            Assert.Equal(2, tokenizer.CurrentToken.Position);
            Assert.Equal("beta", tokenizer.CurrentToken.Value);
            // end
            Assert.False(tokenizer.Next());
        }
    }
}
