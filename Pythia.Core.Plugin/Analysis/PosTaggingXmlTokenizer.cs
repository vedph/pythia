﻿using Fusi.Tools.Configuration;
using Pythia.Core.Analysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// POS-tagging XML tokenizer, used for both real-time and deferred
/// tokenization. This tokenizer derives from <see cref="XmlTokenizerBase"/>,
/// and as such it uses an inner tokenizer for each text node in an XML
/// document.
/// This tokenizer accumulates tokens until a sentence end is found; then,
/// if it has a POS tagger (set via <see cref="SetTagger(ITokenPosTagger)"/>),
/// it applies POS tags to all the sentence's tokens; otherwise, it adds a
/// <c>s0</c> attribute to each sentence-end TextSpan. In any case, it then
/// emits tokens as they are requested.
/// This behavior is required because POS tagging requires a full sentence
/// context.
/// </summary>
/// <remarks>Note that the sentence ends are detected by looking at the
/// full original text, as looking at the single tokens, even when
/// unfiltered, might not be enough; tokens which become empty when filtered
/// would be skipped, and tokens and sentence-end markers might be split
/// between two text nodes, e.g. <c>&lt;hi&gt;test&lt;/hi&gt;.</c> where
/// the stop is located in a different text node.
/// <para>Tag: <c>tokenizer.pos-tagging</c>.</para>
/// </remarks>
[Tag("tokenizer.pos-tagging")]
public sealed class PosTaggingXmlTokenizer : XmlTokenizerBase,
    IConfigurable<PosTaggingXmlTokenizerOptions>
{
    private const int MAX_SENT_TOKENS = 1000;

    private readonly List<TextSpan> _queuedTokens;
    private readonly Regex _endPunctRegex;
    private readonly HashSet<XName> _sentenceStopTags;
    private TextSpan? _aheadToken;
    private int _maxSentenceTokens;
    private ITokenPosTagger? _tagger;

    private bool _sentenceTermedByNode;

    /// <summary>
    /// Initializes a new instance of the <see cref="PosTaggingXmlTokenizer"/>
    /// class.
    /// </summary>
    public PosTaggingXmlTokenizer()
    {
        _queuedTokens = [];
        _sentenceStopTags = [];
        // https://stackoverflow.com/questions/8199774/how-to-match-regex-at-start-index
        _endPunctRegex = new Regex(@"\G\S*[.!?](?:\P{L}|[^0-9])?");
        _maxSentenceTokens = MAX_SENT_TOKENS;
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(PosTaggingXmlTokenizerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _maxSentenceTokens = options.MaxSentenceTokens > 0?
            options.MaxSentenceTokens : MAX_SENT_TOKENS;

        _sentenceStopTags.Clear();
        if (options.StopTags != null)
        {
            foreach (string tag in options.StopTags)
                _sentenceStopTags.Add(tag);
        }
    }

    /// <summary>
    /// Sets the POS tagger to be used by this tokenizer, or null when
    /// just preparing tokens for deferred tokenization.
    /// </summary>
    /// <param name="tagger">The POS tagger</param>
    public void SetTagger(ITokenPosTagger? tagger)
    {
        _tagger = tagger;
    }

    /// <summary>
    /// Called after <see cref="TokenizerBase.Start" /> has been called.
    /// </summary>
    protected override void OnStarted()
    {
        base.OnStarted();

        _queuedTokens.Clear();
        _aheadToken = null;
    }

    /// <summary>
    /// Called when the next relevant node has been read and processed
    /// by this class. Relevant XML node types are only element, end
    /// element, text, and CDATA. This implementation checks for end-element
    /// nodes to detect sentence end.
    /// <param name="reader">The reader.</param>
    /// </summary>
    protected override void OnNextNode(XmlReader reader)
    {
        if (_sentenceStopTags == null
            || (!reader.IsEmptyElement
                && reader.NodeType != XmlNodeType.EndElement))
        {
            return;
        }

        XName name = XName.Get(reader.LocalName, reader.NamespaceURI);
        if (_sentenceStopTags.Contains(name))
            _sentenceTermedByNode = true;
    }

    private bool IsEndOfSentence()
    {
        if (_sentenceTermedByNode) return true;
        return _endPunctRegex.IsMatch(FullText!, CurrentToken.Index);
    }

    private bool DequeueToken()
    {
        if (_queuedTokens.Count == 0) return false;

        TextSpan TextSpan = _queuedTokens[0];
        _queuedTokens.RemoveAt(0);
        CurrentToken.CopyFrom(TextSpan);
        return true;
    }

    private void TagQueuedTokens()
    {
        if (_queuedTokens.Count == 0) return;

        // if tagging in real time, do it
        if (_tagger != null)
        {
            _tagger.Tag(_queuedTokens, "pos");
        }
        // else add s0 (=sentence end) attributes for deferred tokenization
        else
        {
            TextSpan TextSpan = _queuedTokens[^1];
            TextSpan.AddAttribute(
                new Corpus.Core.Attribute
                {
                    Name = "s0",
                    Value = _queuedTokens.Count.ToString(CultureInfo.InvariantCulture),
                    Type = Corpus.Core.AttributeType.Number,
                    TargetId = TextSpan.P1
                });
        }
    }

    /// <summary>
    /// Called after <see cref="TokenizerBase.NextAsync" /> has been invoked.
    /// </summary>
    /// <returns>
    /// false if end of text reached
    /// </returns>
    protected override async Task<bool> OnNextAsync()
    {
        // if there are enqueued tokens, just return the first
        if (DequeueToken()) return true;

        // read and enqueue the next tokens if any
        _sentenceTermedByNode = false;
        while (_queuedTokens.Count < _maxSentenceTokens)
        {
            if (_aheadToken != null)
            {
                _queuedTokens.Add(_aheadToken);
                _aheadToken = null;
            }

            // read the next TextSpan if any
            if (!await base.OnNextAsync()) break;

            // if the XML structure encountered in reading the next TextSpan
            // terminated this sentence, save the TextSpan read for later
            // consumption, tag the enqueued tokens, and return the first
            // of them.
            if (_sentenceTermedByNode)
            {
                TagQueuedTokens();
                if (_queuedTokens.Count > 0)
                {
                    // save read-ahead TextSpan for later consumption
                    _aheadToken = CurrentToken.Clone();
                    DequeueToken();
                    return true;
                }
            }

            _queuedTokens.Add(CurrentToken.Clone());
            if (IsEndOfSentence()) break;
        }

        if (_queuedTokens.Count > 0)
        {
            TagQueuedTokens();
            DequeueToken();
            return true;
        }
        else
        {
            return false;
        }
    }
}

/// <summary>
/// Options for <see cref="PosTaggingXmlTokenizer"/>.
/// </summary>
public sealed class PosTaggingXmlTokenizerOptions
{
    /// <summary>
    /// Gets or sets the maximum tokens count per sentence. This is a
    /// limit set to ensure that if no sentence can be detected, the tokens
    /// are not accumulated in memory indefinitely. The default value
    /// is 1000.
    /// </summary>
    public int MaxSentenceTokens { get; set; }

    /// <summary>
    /// Gets or sets the stop tags names. A "stop tag" is a tag implying a
    /// sentence stop when closed (e.g. <c>head</c> in a TEI document,
    /// as a title is anyway a "sentence", distinct from the following text,
    /// either it ends with a stop or not).
    /// Namespace URIs can be prefixed to the tag name inside braces.
    /// Each tag gets filled with spaces, while a stop tag gets filled with
    /// a full stop followed by spaces.
    /// </summary>
    public IList<string>? StopTags { get; set; }
}
