﻿using Fusi.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Pythia.Core.Analysis;

/// <summary>
/// Base class for tokenizers. Most tokenizers derive from this class,
/// which provides some basic plumbing. Note that empty tokens are discarded,
/// and have no effect on the token position.
/// </summary>
public abstract class TokenizerBase : ITokenizer
{
    /// <summary>
    /// Gets the optional context.
    /// </summary>
    protected IHasDataDictionary? Context { get; private set; }

    /// <summary>
    /// Gets the document identifier.
    /// The tokenizer will assign this ID to the tokens being read,
    /// and eventually use it for specialized purposes (e.g. token lookup
    /// in deferred POS-tagging).
    /// </summary>
    protected int DocumentId { get; private set; }

    /// <summary>
    /// Gets the token filters used by this tokenizer.
    /// </summary>
    public IList<ITokenFilter> Filters { get; }

    /// <summary>
    /// Gets the text reader representing the source for this tokenizer.
    /// </summary>
    protected TextReader? Reader { get; set; }

    /// <summary>
    /// Gets the current position.
    /// </summary>
    protected int Position { get; private set; }

    /// <summary>
    /// Gets the current token.
    /// </summary>
    public TextSpan CurrentToken { get; }

    /// <summary>
    /// Gets the current token's unfiltered value.
    /// </summary>
    protected string? CurrentUnfilteredValue { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenizerBase"/> class.
    /// </summary>
    protected TokenizerBase()
    {
        CurrentToken = new TextSpan();
        Filters = [];
    }

    /// <summary>
    /// Called after <see cref="Start"/> has been called.
    /// </summary>
    protected virtual void OnStarted()
    {
    }

    /// <summary>
    /// Start the tokenizer for the specified input text.
    /// </summary>
    /// <param name="reader">The reader to read the next token from.</param>
    /// <param name="documentId">The ID of the document to be tokenized.</param>
    /// <param name="context">The optional context.</param>
    /// <exception cref="ArgumentNullException">reader</exception>
    public void Start(TextReader reader, int documentId,
        IHasDataDictionary? context = null)
    {
        Reader = reader ?? throw new ArgumentNullException(nameof(reader));
        DocumentId = documentId;
        Position = 0;
        Context = context;
        OnStarted();
    }

    /// <summary>
    /// Called after <see cref="NextAsync"/> has been invoked; implement in your
    /// tokenizer to do the actual work.
    /// </summary>
    /// <returns>false if end of text reached</returns>
    protected abstract Task<bool> OnNextAsync();

    /// <summary>
    /// Advance to the next available token if any.
    /// </summary>
    /// <returns>false if end of input reached</returns>
    public async Task<bool> NextAsync()
    {
        do
        {
            // get the next token
            CurrentToken.Reset();
            if (!await OnNextAsync()) return false;

            // set document ID
            CurrentToken.DocumentId = DocumentId;
            CurrentToken.Text = CurrentToken.Value;

            // apply filters to it
            CurrentUnfilteredValue = CurrentToken.Value;
            if (Filters.Count > 0)
            {
                foreach (ITokenFilter filter in Filters)
                    await filter.ApplyAsync(CurrentToken, Position + 1, Context);
            }

            // repeat until we get a non-empty token
        } while (string.IsNullOrEmpty(CurrentToken.Value));

        // increase position. Note that the position is incremented
        // only when a non-empty token is found.
        CurrentToken.SetPositions(++Position);

        return true;
    }
}
