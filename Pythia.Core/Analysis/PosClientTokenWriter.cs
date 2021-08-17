using System;
using System.IO;

namespace Pythia.Core.Analysis
{
    /// <summary>
    /// A token writer whose TSV output should be consumed by POS taggers.
    /// </summary>
    /// <seealso cref="Pythia.Core.Analysis.ITokenWriter" />
    public sealed class PosClientTokenWriter : ITokenWriter,
        IDisposable
    {
        private readonly TextWriter _output;
        private readonly string _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="PosClientTokenWriter" /> class.
        /// </summary>
        /// <param name="content">The source document text content,
        /// used to extract the token's original (unfiltered) text.</param>
        /// <param name="output">The output.</param>
        /// <exception cref="ArgumentNullException">output</exception>
        public PosClientTokenWriter(string content, TextWriter output)
        {
            _content = content ??
                throw new ArgumentNullException(nameof(content));
            _output = output ??
                throw new ArgumentNullException(nameof(output));
        }

        /// <summary>
        /// Writes the specified token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <exception cref="ArgumentNullException">token</exception>
        public void Write(Token token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            string original = _content.Substring(token.Index, token.Length);

            _output.WriteLine($"{token.DocumentId}\t{token.Position}\t" +
                $"{token.Value}\t{token.Index}\t{token.Length}\t{original}");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _output?.Flush();
                    _output?.Close();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
