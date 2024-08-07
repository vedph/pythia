﻿using System.Text;
using System.Threading.Tasks;
using Fusi.Tools.Configuration;
using Pythia.Core.Analysis;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// A standard tokenizer, which splits tokens at whitespaces or when ending
/// with an apostrophe, which is included in the token.
/// Tag: <c>tokenizer.standard</c>.
/// </summary>
/// <seealso cref="TokenizerBase" />
[Tag("tokenizer.standard")]
public sealed class StandardTokenizer : TokenizerBase
{
    private readonly StringBuilder _sb;
    private int _offset;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhitespaceTokenizer" />
    /// class.
    /// </summary>
    public StandardTokenizer()
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
    /// Called after <see cref="TokenizerBase.NextAsync" /> has been invoked.
    /// </summary>
    /// <returns>
    /// false if end of text reached
    /// </returns>
    protected override Task<bool> OnNextAsync()
    {
        int n;
        while ((n = Reader!.Peek()) != -1 && char.IsWhiteSpace((char)n))
        {
            Reader.Read();
            _offset++;
        }
        if (n == -1) return Task.FromResult(false);

        _sb.Clear();
        int startOffset = _offset;
        while ((n = Reader.Read()) != -1)
        {
            _offset++;
            char c = (char)n;
            if (char.IsWhiteSpace(c)) break;
            _sb.Append(c);
            if (c == '\'' && _sb.Length > 1) break;
        }
        if (_sb.Length == 0) return Task.FromResult(false);

        CurrentToken.Value = _sb.ToString();
        CurrentToken.Length = (short)_sb.Length;
        CurrentToken.Index = startOffset;
        return Task.FromResult(true);
    }
}
