using Conllu;
using Fusi.Tools;
using Fusi.Tools.Configuration;
using Fusi.Tools.Text;
using Pythia.Core;
using Pythia.Core.Analysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pythia.Udp.Plugin;

/// <summary>
/// UDP-based token filter. This filter adds new attributes to the token
/// by getting them from the filter's context sentences, keyed under
/// <see cref="UdpTextFilter.UDP_KEY"/>, and assumed to be stored
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
            Props = UdpTokenProps.Lemma | UdpTokenProps.UPosTag |
                UdpTokenProps.XPosTag | UdpTokenProps.DepRel
        };
    }

    [GeneratedRegex("TokenRange=([0-9]+):([0-9]+)", RegexOptions.Compiled)]
    private static partial Regex RangeRegex();

    private TextRange ParseUdpRange(string text, int offset)
    {
        if (string.IsNullOrEmpty(text)) return TextRange.Empty;
        Match m = _rangeRegex.Match(text);
        if (!m.Success) return TextRange.Empty;

        int a = offset + int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        int b = offset + int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);

        return new TextRange(a, b - a);
    }

    private Token? MatchToken(IList<UdpChunk> chunks, TextSpan token)
    {
        TextRange tokenRange = new(token.Index, token.Length);

        foreach (UdpChunk chunk in chunks
            .Where(c => !c.IsOversized && !c.HasNoAlpha &&
                        c.Range.Overlaps(tokenRange)))
        {
            if (chunk.Range.Start > tokenRange.End) break;

            foreach (Sentence sentence in chunk.Sentences)
            {
                Token? matched = sentence.Tokens.Find(
                    t => ParseUdpRange(t.Misc, chunk.Range.Start)
                         .Overlaps(tokenRange));
                if (matched != null) return matched;
            }
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
    /// sentences under key <see cref="UdpTextFilter.UDP_KEY"/>,
    /// this filter will do nothing.</param>
    /// <exception cref="ArgumentNullException">token</exception>
    public void Apply(TextSpan token, int position,
        IHasDataDictionary? context = null)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (_options.Props == UdpTokenProps.None ||
            context?.Data.ContainsKey(UdpTextFilter.UDP_KEY) != true)
        {
            return;
        }

        // find the target token
        IList<UdpChunk> chunks = (IList<UdpChunk>)
            context.Data[UdpTextFilter.UDP_KEY];

        Token? matched = MatchToken(chunks, token);
        if (matched == null) return;

        // extract data as attributes (except for upos):
        // lemma
        if ((_options.Props & UdpTokenProps.Lemma) != 0 &&
            !string.IsNullOrEmpty(matched.Lemma))
        {
            token.Lemma = matched.Lemma;
        }

        // upos
        if ((_options.Props & UdpTokenProps.UPosTag) != 0 &&
            !string.IsNullOrEmpty(matched.Upos))
        {
            token.Pos = matched.Upos;
        }

        // xpos
        if ((_options.Props & UdpTokenProps.XPosTag) != 0 &&
            !string.IsNullOrEmpty(matched.Xpos))
        {
            token.AddAttribute(new Corpus.Core.Attribute
            {
                Name = GetPrefixedName("xpos"),
                Value = matched.Xpos
            });
        }

        // feats
        if ((_options.Props & UdpTokenProps.Feats) != 0 &&
            matched.Feats.Count > 0)
        {
            foreach (var p in matched.Feats)
            {
                token.AddAttribute(new Corpus.Core.Attribute
                {
                    Name = string.IsNullOrEmpty(_options.FeatPrefix)
                     ? GetPrefixedName(p.Key.ToLowerInvariant())
                     : _options.FeatPrefix + GetPrefixedName(p.Key),
                    Value = p.Value.ToLowerInvariant()
                });
            }
        }

        // head
        if ((_options.Props & UdpTokenProps.Head) != 0 && matched.Head != null)
        {
            token.AddAttribute(new Corpus.Core.Attribute
            {
                Name = GetPrefixedName("head"),
                Value = matched.Head.Value.ToString(CultureInfo.InvariantCulture),
                Type = Corpus.Core.AttributeType.Number
            });
        }

        // deprel
        if ((_options.Props & UdpTokenProps.DepRel) != 0 &&
            !string.IsNullOrEmpty(matched.DepRel))
        {
            token.AddAttribute(new Corpus.Core.Attribute
            {
                Name = GetPrefixedName("deprel"),
                Value = matched.DepRel
            });
        }

        // misc
        if ((_options.Props & UdpTokenProps.Misc) != 0 &&
            !string.IsNullOrEmpty(matched.Misc))
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
/// The properties of an UDP token. This is used by
/// <see cref="UdpTokenFilterOptions"/> to specify which properties should
/// be mapped to token attributes.
/// </summary>
[Flags]
public enum UdpTokenProps
{
    /// <summary>No properties.</summary>
    None = 0,
    /// <summary>UDP Lemma (=attribute <c>lemma</c>): 1.</summary>
    Lemma = 0x01,
    /// <summary>UDP UPosTag (=attribute <c>upos</c>): 2.</summary>
    UPosTag = 0x02,
    /// <summary>UDP XPosTag (=attribute <c>xpos</c>): 4.</summary>
    XPosTag = 0x04,
    /// <summary>UDP Feats (=one attribute per feature, named after it,
    /// and eventually prefixed).</summary>
    Feats = 0x08,
    /// <summary>UDP Head (=numeric attribute <c>head</c>): 8.</summary>
    Head = 0x10,
    /// <summary>UDP DepRel (=attribute <c>deprel</c>): 16.</summary>
    DepRel = 0x20,
    /// <summary>UDP Misc (=attribute <c>misc</c>): 32.</summary>
    Misc = 0x40,
    /// <summary>All the UDP properties.</summary>
    All = Lemma | UPosTag | XPosTag | Feats | Head | DepRel | Misc,
}

/// <summary>
/// Options for <see cref="UdpTokenFilter"/>.
/// </summary>
public class UdpTokenFilterOptions
{
    /// <summary>
    /// Gets or sets the UDP properties to map to attributes.
    /// </summary>
    public UdpTokenProps Props { get; set; }

    /// <summary>
    /// Gets or sets the optional prefix to add before each attribute name
    /// as derived from UDP.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// The prefix to add to each feature name attribute.
    /// </summary>
    public string? FeatPrefix { get; set; }
}
