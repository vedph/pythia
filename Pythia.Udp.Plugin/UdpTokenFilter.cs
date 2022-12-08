using Conllu;
using Fusi.Tools;
using Fusi.Tools.Config;
using Fusi.Tools.Text;
using Pythia.Core.Analysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Pythia.Udp.Plugin;

/// <summary>
/// UDP-based token filter. This filter adds new attributes to the token
/// by getting them from the filter's context sentences, keyed under
/// <see cref="UdpTextFilter.SENTENCES_KEY"/>, and assumed to be stored
/// there by the <see cref="UdpTextFilter"/>.
/// See https://lindat.mff.cuni.cz/services/udpipe/ for the CONLLU token
/// properties.
/// <para>Tag: <c>token-filter.udp</c>.</para>
/// </summary>
/// <seealso cref="ITokenFilter" />
[Tag("token-filter.udp")]
public sealed partial class UdpTokenFilter : ITokenFilter,
    IConfigurable<UdpTokenFilterOptions>
{
    private readonly Regex _rangeRegex;
    private UdpTokenFilterOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="UdpTokenFilter"/> class.
    /// </summary>
    public UdpTokenFilter()
    {
        _rangeRegex = RangeRegex();
        _options = new()
        {
            Lemma = true,
            UPosTag= true,
            XPosTag= true,
            DepRel = true
        };
    }

    [GeneratedRegex("TokenRange=([0-9]+):([0-9]+)", RegexOptions.Compiled)]
    private static partial Regex RangeRegex();

    private TextRange ParseUdpRange(string text)
    {
        Match m = _rangeRegex.Match(text);
        if (!m.Success) return TextRange.Empty;

        int a = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        int b = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);

        return new TextRange(a, b - a);
    }

    private Token? MatchToken(IList<Sentence> sentences, Core.Token token)
    {
        TextRange tokenRange = new(token.Index, token.Length);

        foreach (Sentence sentence in sentences)
        {
            Token? matched = sentence.Tokens.Find(
                t => ParseUdpRange(t.Misc).Overlaps(tokenRange));
            if (matched != null) return matched;
        }
        return null;
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(UdpTokenFilterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    private string GetPrefixedName(string value)
        => string.IsNullOrEmpty(_options.Prefix) ? value : _options.Prefix + value;

    /// <summary>
    /// Apply the filter to the specified token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="position">The position which will be assigned to
    /// the resulting token, provided that it's not empty. Some filters
    /// may use this value, e.g. to identify tokens like in deferred
    /// POS tagging.</param>
    /// <param name="context">The optional context. If null, or if lacking
    /// sentences under key <see cref="UdpTextFilter.SENTENCES_KEY"/>,
    /// this filter will do nothing.</param>
    /// <exception cref="ArgumentNullException">token</exception>
    public void Apply(Core.Token token, int position,
        IHasDataDictionary? context = null)
    {
        if (token is null) throw new ArgumentNullException(nameof(token));

        if (_options.IsEmpty() ||
            context?.Data.ContainsKey(UdpTextFilter.SENTENCES_KEY) != true)
        {
            return;
        }

        // find the target token
        IList<Sentence> sentences =
            (IList<Sentence>)context.Data[UdpTextFilter.SENTENCES_KEY];
        Conllu.Token? matched = MatchToken(sentences, token);
        if (matched == null) return;

        // extract data as attributes:
        // lemma
        if (_options.Lemma && !string.IsNullOrEmpty(matched.Lemma))
        {
            token.AddAttribute(new Corpus.Core.Attribute
            {
                Name = GetPrefixedName("lemma"),
                Value = matched.Lemma
            });
        }
        // upos
        if (_options.UPosTag && !string.IsNullOrEmpty(matched.Upos))
        {
            token.AddAttribute(new Corpus.Core.Attribute
            {
                Name = GetPrefixedName("upos"),
                Value = matched.Upos
            });
        }
        // xpos
        if (_options.XPosTag && !string.IsNullOrEmpty(matched.Xpos))
        {
            token.AddAttribute(new Corpus.Core.Attribute
            {
                Name = GetPrefixedName("xpos"),
                Value = matched.Xpos
            });
        }
        // feats
        if (_options.Feats && matched.Feats.Count > 0)
        {
            foreach (var p in matched.Feats)
            {
                token.AddAttribute(new Corpus.Core.Attribute
                {
                    Name = string.IsNullOrEmpty(_options.FeatPrefix)
                     ? GetPrefixedName(p.Key)
                     : _options.FeatPrefix + GetPrefixedName(p.Key),
                    Value = p.Value
                });
            }
        }
        // head
        if (_options.Head && matched.Head != null)
        {
            token.AddAttribute(new Corpus.Core.Attribute
            {
                Name = GetPrefixedName("head"),
                Value = matched.Head.Value.ToString(CultureInfo.InvariantCulture),
                Type = Corpus.Core.AttributeType.Number
            });
        }
        // deprel
        if (_options.DepRel && !string.IsNullOrEmpty(matched.DepRel))
        {
            token.AddAttribute(new Corpus.Core.Attribute
            {
                Name = GetPrefixedName("deprel"),
                Value = matched.DepRel
            });
        }
        // misc
        if (_options.Misc && !string.IsNullOrEmpty(matched.Misc))
        {
            token.AddAttribute(new Corpus.Core.Attribute
            {
                Name = GetPrefixedName("misc"),
                Value = matched.Misc
            });
        }
    }
}

/// <summary>
/// Options for <see cref="UdpTokenFilter"/>.
/// </summary>
public class UdpTokenFilterOptions
{
    /// <summary>
    /// Gets or sets the optional prefix to add before each attribute name
    /// as derived from UDP.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// True to add UDP Lemma as attribute <c>lemma</c>.
    /// </summary>
    public bool Lemma { get; set; }

    /// <summary>
    /// True to add UDP UPosTag as attribute <c>upos</c>.
    /// </summary>
    public bool UPosTag { get; set; }

    /// <summary>
    /// True to add UDP XPosTag as attribute <c>xpos</c>.
    /// </summary>
    public bool XPosTag { get; set; }

    /// <summary>
    /// True to add UDP Feats as attributes, where name is feature name,
    /// eventually prefixed by <see cref="FeatPrefix"/>.
    /// </summary>
    public bool Feats { get; set; }

    /// <summary>
    /// The prefix to add to each feature name attribute.
    /// </summary>
    public string? FeatPrefix { get; set; }

    /// <summary>
    /// True to add UDP Head as attribute <c>head</c> (numeric).
    /// </summary>
    public bool Head { get; set; }

    /// <summary>
    /// True to add UDP DepRel as attribute <c>deprel</c>.
    /// </summary>
    public bool DepRel { get; set; }

    /// <summary>
    /// True to add UDP Misc as attribute <c>misc</c>.
    /// </summary>
    public bool Misc { get; set; }

    /// <summary>
    /// Determines whether this filter is empty.
    /// </summary>
    /// <returns>
    /// <c>true</c> if filter is empty; otherwise, <c>false</c>.
    /// </returns>
    public bool IsEmpty() => Lemma || UPosTag || XPosTag
        || Feats || Head || DepRel || Misc;
}
