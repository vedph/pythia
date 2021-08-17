using Antlr4.Runtime;
using System.IO;

namespace Pythia.Core.Query
{
    // https://stackoverflow.com/questions/18132078/handling-errors-in-antlr4

    /// <summary>
    /// An error listener which just throws any error received.
    /// </summary>
    /// <seealso cref="Antlr4.Runtime.BaseErrorListener" />
    public sealed class ThrowingErrorListener : BaseErrorListener
    {
        /// <summary>
        /// Handles the syntax error by throwing a new <see
        /// cref="PythiaQueryException"/>.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="recognizer">The recognizer.</param>
        /// <param name="offendingSymbol">The offending symbol.</param>
        /// <param name="line">The line.</param>
        /// <param name="charPositionInLine">The character position in line.
        /// </param>
        /// <param name="msg">The MSG.</param>
        /// <param name="e">The e.</param>
        /// <exception cref="PythiaQueryException"></exception>
        public override void SyntaxError(TextWriter output,
            IRecognizer recognizer, IToken offendingSymbol,
            int line, int charPositionInLine, string msg,
            RecognitionException e)
        {
            throw new PythiaQueryException($"{line},{charPositionInLine}: {msg}");
        }
    }
}
