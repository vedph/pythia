using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Fusi.Tools.Configuration;
using Pythia.Core.Analysis;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// A standard tokenizer, which splits tokens at whitespaces or when ending
/// with an apostrophe, which is included in the token, or at currency symbols
/// (e.g. <c>100$</c> becoming two tokens, <c>100</c> and <c>$</c>).
/// Tag: <c>tokenizer.standard</c>.
/// </summary>
/// <seealso cref="TokenizerBase" />
[Tag("tokenizer.standard")]
public sealed class StandardTokenizer : TokenizerBase
{
    private readonly StringBuilder _sb = new();
    private int _offset;

    /// <summary>
    /// Called when resetting the tokenizer.
    /// </summary>
    protected override void OnStarted()
    {
        _offset = 0;
    }

    /// <summary>
    /// Called after <see cref="TokenizerBase.NextAsync" /> has been invoked.
    /// </summary>
    /// <returns>false if end of text reached</returns>
    protected override Task<bool> OnNextAsync()
    {
        // skip whitespaces
        int n;
        while ((n = Reader!.Peek()) != -1 && char.IsWhiteSpace((char)n))
        {
            Reader.Read();
            _offset++;
        }
        if (n == -1) return Task.FromResult(false);

        _sb.Clear();
        int startOffset = _offset;

        // handle first character separately if it's a currency symbol
        char c = (char)n;
        if (char.GetUnicodeCategory(c) == UnicodeCategory.CurrencySymbol)
        {
            // consume the currency symbol
            Reader.Read();
            _offset++;
            _sb.Append(c);
        }
        else
        {
            while ((n = Reader.Peek()) != -1)
            {
                c = (char)n;

                // check for token boundaries without consuming the character
                if (char.IsWhiteSpace(c) ||
                    char.GetUnicodeCategory(c) == UnicodeCategory.CurrencySymbol)
                {
                    // for whitespace, consume it; for currency, leave it
                    if (char.IsWhiteSpace(c))
                    {
                        Reader.Read();
                        _offset++;
                    }
                    break;
                }

                // safely consume the character
                Reader.Read();
                _offset++;
                _sb.Append(c);

                // special case for apostrophe - it's part of the token,
                // but also ends it
                if (c == '\'' && _sb.Length > 1) break;
            }

            if (_sb.Length == 0) return Task.FromResult(false);
        }

        CurrentToken.Value = _sb.ToString();
        CurrentToken.Length = (short)_sb.Length;
        CurrentToken.Index = startOffset;
        return Task.FromResult(true);
    }
}
