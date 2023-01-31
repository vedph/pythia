using System.Text;
using Fusi.Tools;
using Fusi.Tools.Configuration;
using Pythia.Core.Analysis;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// Simple whitespace tokenizer.
    /// Tag: <c>tokenizer.whitespace</c>.
    /// </summary>
    /// <seealso cref="TokenizerBase" />
    [Tag("tokenizer.whitespace")]
    public sealed class WhitespaceTokenizer : TokenizerBase
    {
        private readonly StringBuilder _sb;
        private int _offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhitespaceTokenizer" />
        /// class.
        /// </summary>
        public WhitespaceTokenizer()
        {
            _sb = new StringBuilder();
        }

        /// <summary>
        /// Called when resetting the tokenizer.
        /// </summary>
        protected override void OnStarted()
        {
            _offset = 0;
        }

        /// <summary>
        /// Called after <see cref="TokenizerBase.Next" /> has been invoked.
        /// </summary>
        /// <returns>
        /// false if end of text reached
        /// </returns>
        protected override bool OnNext()
        {
            int n;
            while ((n = Reader!.Peek()) != -1 && char.IsWhiteSpace((char) n))
            {
                Reader.Read();
                _offset++;
            }
            if (n == -1) return false;

            _sb.Clear();
            int startOffset = _offset;
            while ((n = Reader.Read()) != -1)
            {
                _offset++;
                char c = (char) n;
                if (char.IsWhiteSpace(c)) break;
                _sb.Append(c);
            }
            if (_sb.Length == 0) return false;

            CurrentToken.Value = _sb.ToString();
            CurrentToken.Length = (short) _sb.Length;
            CurrentToken.Index = startOffset;
            return true;
        }
    }
}
