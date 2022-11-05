using Pythia.Core.Plugin.Analysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

namespace Pythia.Core.Plugin.Test.Analysis
{
    public sealed class FsForwardTokenCacheTest : IDisposable
    {
        private readonly string _dir;

        public FsForwardTokenCacheTest()
        {
            _dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "~FsForwardTokenCacheTest");
        }

        private void EnsureDirExists()
        {
            if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);
        }

        private void EnsureDirNotExists()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Fact]
        public void Open_NotExisting_Ok()
        {
            FsForwardTokenCache cache = new();
            EnsureDirNotExists();

            cache.Open(_dir);

            Assert.True(Directory.Exists(_dir));
        }

        [Fact]
        public void Open_Existing_Ok()
        {
            FsForwardTokenCache cache = new();
            EnsureDirExists();

            cache.Open(_dir);

            Assert.True(Directory.Exists(_dir));
        }

        [Fact]
        public void Delete_NotExisting_Ok()
        {
            FsForwardTokenCache cache = new();
            EnsureDirNotExists();

            cache.Delete(_dir);

            Assert.False(Directory.Exists(_dir));
        }

        [Fact]
        public void Delete_Existing_Ok()
        {
            FsForwardTokenCache cache = new();
            EnsureDirExists();

            cache.Delete(_dir);

            Assert.False(Directory.Exists(_dir));
        }

        [Fact]
        public void AddTokens_6_Ok()
        {
            FsForwardTokenCache cache = new();
            cache.Open(_dir);

            StandardTokenizer tokenizer = new();
            const string text = "Hello, world! This is a test.";
            tokenizer.Start(new StringReader(text), 1);
            List<Token> tokens = new();
            while (tokenizer.Next())
            {
                tokenizer.CurrentToken.DocumentId = 1;
                tokens.Add(tokenizer.CurrentToken.Clone());
            }

            cache.AddTokens(1, tokens, text);

            cache.Close();

            string path = Path.Combine(_dir, "00001.001.txt");
            Assert.True(File.Exists(path));
        }

        [Fact]
        public void AddTokens_6Max5_Ok()
        {
            FsForwardTokenCache cache = new()
            {
                TokensPerFile = 5
            };
            cache.Open(_dir);

            StandardTokenizer tokenizer = new();
            const string text = "Hello, world! This is a test.";
            tokenizer.Start(new StringReader(text), 1);
            List<Token> tokens = new();
            while (tokenizer.Next())
            {
                tokenizer.CurrentToken.DocumentId = 1;
                tokens.Add(tokenizer.CurrentToken.Clone());
            }

            cache.AddTokens(1, tokens, text);

            cache.Close();

            string path = Path.Combine(_dir, "00001.001.txt");
            Assert.True(File.Exists(path));
            path = Path.Combine(_dir, "00001.002.txt");
            Assert.True(File.Exists(path));
        }

        [Fact]
        public void GetToken_Ok()
        {
            FsForwardTokenCache cache = new()
            {
                TokensPerFile = 5
            };
            cache.Open(_dir);

            // add 6 tokens in 2 files
            StandardTokenizer tokenizer = new();
            const string text = "Hello, world! This is a test.";
            tokenizer.Start(new StringReader(text), 1);
            List<Token> tokens = new();
            while (tokenizer.Next())
            {
                tokenizer.CurrentToken.DocumentId = 1;
                tokens.Add(tokenizer.CurrentToken.Clone());
            }
            cache.AddTokens(1, tokens, text);
            cache.Close();

            cache.Open(_dir);
            string[] expected = new[]
            {
                "Hello,", "world!", "This", "is", "a", "test."
            };
            for (int i = 0; i < 6; i++)
            {
                Token? token = cache.GetToken(1, 1 + i);
                Assert.NotNull(token);
                Assert.Equal(expected[i], token.Value);
            }
            cache.Close();
        }

        #region IDisposable Support
        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Thread.Sleep(1000);
                    EnsureDirNotExists();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
