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
using System.Threading.Tasks;

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

    /// <summary>
    /// Matches the specified token in the given chunks.
    /// </summary>
    /// <param name="chunks">The UDP chunks.</param>
    /// <param name="token">The token to match.</param>
    /// <returns>UDP token or null if not matched.</returns>
    private Token? MatchToken(IList<UdpChunk> chunks, TextSpan token)
    {
        // create a range for the token
        TextRange tokenRange = new(token.Index, token.Length);

        // for each chunk, find the first sentence which contains
        // the token range
        foreach (UdpChunk chunk in chunks.Where(
            c => !c.IsOversized && !c.HasNoAlpha &&
            c.Range.Overlaps(tokenRange)))
        {
            // if the chunk starts after the token, then stop
            if (chunk.Range.Start > tokenRange.End) break;

            // find the first token in the chunk's sentences which overlaps
            // the token range and is not a punctuation
            foreach (Sentence sentence in chunk.Sentences)
            {
                Token? matched = sentence.Tokens.Find(t =>
                    ParseUdpRange(t.Misc, chunk.Range.Start)
                        .Overlaps(tokenRange) && t.Upos != "PUNCT");

                // if matched, return it
                if (matched != null) return matched;
            }
        }

        // if no token matched, return null
        return null;
    }

    private static List<Token> CollectChildTokens(IList<UdpChunk> chunks,
        Token parent, TextRange parentRange)
    {
        foreach (UdpChunk chunk in chunks.Where(
            c => !c.IsOversized && !c.HasNoAlpha &&
            c.Range.Overlaps(parentRange)))
        {
            foreach (Sentence sentence in chunk.Sentences)
            {
                int i = sentence.Tokens.IndexOf(parent);
                if (i < 0) continue;
                List<Token> children = [];
                for (int j = i + 1; j < sentence.Tokens.Count; j++)
                {
                    Token t = sentence.Tokens[j];
                    if (t.Misc?.Contains("TokenRange") == true) break;
                    children.Add(t);
                }
                return children;
            }
        }
        return [];
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

    private bool ShouldPreservePos(string? oldPos, string? newPos)
    {
        return oldPos != newPos &&
               !string.IsNullOrEmpty(oldPos) &&
               (_options.PreservedTags.Contains(oldPos));
    }

    private string GetPrefixedName(string value)
        => string.IsNullOrEmpty(_options.Prefix) ? value : _options.Prefix + value;

    private static bool MatchesTokenPattern(List<Token> children,
        List<UdpChildTokenOptions> pattern)
    {
        int childIndex = 0;
        int patternIndex = 0;

        while (patternIndex < pattern.Count && childIndex < children.Count)
        {
            var tokenOption = pattern[patternIndex];
            var child = children[childIndex];

            bool matches = true;

            // check UPOS
            if (!string.IsNullOrEmpty(tokenOption.Upos) && 
                child.Upos != tokenOption.Upos)
            {
                matches = false;
            }

            // check XPOS
            if (matches && !string.IsNullOrEmpty(tokenOption.Xpos) && 
                child.Xpos != tokenOption.Xpos)
            {
                matches = false;
            }

            // check features
            if (matches && tokenOption.Feats != null && tokenOption.Feats.Count > 0)
            {
                foreach (var feat in tokenOption.Feats)
                {
                    if (!child.Feats.TryGetValue(feat.Key, out string? value) || 
                        value != feat.Value)
                    {
                        matches = false;
                        break;
                    }
                }
            }

            if (matches)
            {
                // token matches, move to next pattern and child
                patternIndex++;
                childIndex++;
            }
            else if (tokenOption.IsOptional)
            {
                // token doesn't match but is optional, skip pattern
                patternIndex++;
            }
            else
            {
                // token doesn't match and is required, pattern fails
                return false;
            }
        }

        // check if we've consumed all required patterns
        while (patternIndex < pattern.Count)
        {
            if (!pattern[patternIndex].IsOptional) return false;
            patternIndex++;
        }

        return true;
    }

    private void ApplyTargetConfiguration(List<Token> children,
        UdpMultiwordTokenTargetOptions targetOptions, TextSpan targetToken)
    {
        // apply lemma
        targetToken.Lemma = string.IsNullOrEmpty(targetOptions.Lemma)
            ? targetToken.Value : targetOptions.Lemma;

        // apply UPOS
        if ((_options.Props & UdpTokenProps.UPosTag) != 0 && 
            !string.IsNullOrEmpty(targetOptions.Upos))
        {
            targetToken.Pos = targetOptions.Upos;
        }

        // apply XPOS
        if ((_options.Props & UdpTokenProps.XPosTag) != 0 && 
            !string.IsNullOrEmpty(targetOptions.Xpos))
        {
            targetToken.AddAttribute(new Corpus.Core.Attribute
            {
                Name = GetPrefixedName("xpos"),
                Value = targetOptions.Xpos
            });
        }

        // apply features
        if ((_options.Props & UdpTokenProps.Feats) != 0 && 
            targetOptions.Feats != null && targetOptions.Feats.Count > 0)
        {
            foreach (var feat in targetOptions.Feats)
            {
                string featureName = feat.Key;
                string featureValue = feat.Value;

                // handle special "*" key meaning all features from
                // a specific child
                if (featureName == "*")
                {
                    if (int.TryParse(featureValue, out int childIndex) && 
                        childIndex > 0 && childIndex <= children.Count)
                    {
                        // 1-based to 0-based
                        Token sourceChild = children[childIndex - 1];

                        foreach (KeyValuePair<string, string> childFeat in
                            sourceChild.Feats)
                        {
                            targetToken.AddAttribute(new Corpus.Core.Attribute
                            {
                                Name = string.IsNullOrEmpty(_options.FeatPrefix)
                                    ? GetPrefixedName(childFeat.Key.ToLowerInvariant())
                                    : _options.FeatPrefix + GetPrefixedName(childFeat.Key),
                                Value = childFeat.Value.ToLowerInvariant()
                            });
                        }
                    }
                }
                else
                {
                    // handle feature value that might reference a child token
                    if (int.TryParse(featureValue, out int childIndex) && 
                        childIndex > 0 && childIndex <= children.Count)
                    {
                        // 1-based to 0-based
                        Token sourceChild = children[childIndex - 1];
                        if (sourceChild.Feats.TryGetValue(featureName,
                            out string? value))
                        {
                            targetToken.AddAttribute(new Corpus.Core.Attribute
                            {
                                Name = string.IsNullOrEmpty(_options.FeatPrefix)
                                    ? GetPrefixedName(featureName.ToLowerInvariant())
                                    : _options.FeatPrefix + GetPrefixedName(featureName),
                                Value = value.ToLowerInvariant()
                            });
                        }
                    }
                    else
                    {
                        // direct feature value
                        targetToken.AddAttribute(new Corpus.Core.Attribute
                        {
                            Name = string.IsNullOrEmpty(_options.FeatPrefix)
                                ? GetPrefixedName(featureName.ToLowerInvariant())
                                : _options.FeatPrefix + GetPrefixedName(featureName),
                            Value = featureValue.ToLowerInvariant()
                        });
                    }
                }
            }
        }
    }

    private void ApplyMultiword(List<Token> children, TextSpan target)
    {
        // if no multiword configurations are defined, exit
        if (_options.Multiwords == null || _options.Multiwords.Count == 0)
            return;

        // find the first matching multiword config
        foreach (var config in _options.Multiwords)
        {
            // check count constraints
            if (config.MinCount > 0 && children.Count < config.MinCount)
                continue;
            if (config.MaxCount > 0 && children.Count > config.MaxCount)
                continue;

            // check if tokens match the pattern
            if (!MatchesTokenPattern(children, config.Tokens))
                continue;

            // match found, apply the target configuration
            ApplyTargetConfiguration(children, config.Target, target);
            break;
        }
    }

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
    public Task ApplyAsync(TextSpan token, int position,
        IHasDataDictionary? context = null)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (_options.Props == UdpTokenProps.None ||
            context?.Data.ContainsKey(UdpTextFilter.UDP_KEY) != true ||
            (_options.Language != null && token.Language != _options.Language) ||
            (_options.Language?.Length == 0 && !string.IsNullOrEmpty(token.Language)))
        {
            return Task.CompletedTask;
        }

        // find the target token
        IList<UdpChunk> chunks = (IList<UdpChunk>)
            context.Data[UdpTextFilter.UDP_KEY];

        Token? matched = MatchToken(chunks, token);
        if (matched == null) return Task.CompletedTask;

        // if token already has a POS among the tags to preserve, and the
        // matched token has a different POS, then do not touch it at all.
        // This avoids overriding proper names which happen to be tagged in
        // a different way by UDP, like a PROPN "Benedetta" tagged as ADJ.
        if (ShouldPreservePos(token.Pos, matched.Upos))
        {
            return Task.CompletedTask;
        }

        // if it's a multiword token (e.g. della = di la), then collect all
        // its children tokens which are all the tokens following it without
        // a TokenRange in Misc
        if (matched.IsMultiwordToken)
        {
            List<Token> children = CollectChildTokens(chunks, matched,
                new(token.Index, token.Length));
            ApplyMultiword(children, token);
            return Task.CompletedTask;
        }

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

        return Task.CompletedTask;
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
/// Options for a child token in a multiword token.
/// </summary>
public class UdpChildTokenOptions
{
    /// <summary>
    /// The UDP UPOS tag to match.
    /// </summary>
    public string? Upos { get; set; }

    /// <summary>
    /// The UDP XPOS tag to match.
    /// </summary>
    public string? Xpos { get; set; }

    /// <summary>
    /// The features to match. Each entry is a key-value pair.
    /// </summary>
    public Dictionary<string, string>? Feats { get; set; }

    /// <summary>
    /// True if this child token is optional.
    /// </summary>
    public bool IsOptional { get; set; }
}

/// <summary>
/// The options for a multiword token, which contains 2 or more
/// children tokens.
/// </summary>
public class UdpMultiwordTokenTargetOptions
{
    /// <summary>
    /// The lemma to assign. If not specified, simply assign the token's value
    /// as the lemma.
    /// </summary>
    public string? Lemma { get; set; }

    /// <summary>
    /// The UPOS tag to assign.
    /// </summary>
    public required string Upos { get; set; }

    /// <summary>
    /// The optional XPOS tag to assign.
    /// </summary>
    public string? Xpos { get; set; }

    /// <summary>
    /// The optional features to assign. Each entry is a key-value pair
    /// (e.g. Number=Sing or Number=2 where 2 is the ordinal number of the child
    /// token to get the feature's value from).
    /// A special key <c>*</c> means all properties. In this case, the value
    /// is the ordinal number of the child token to get features from.
    /// </summary>
    public Dictionary<string,string>? Feats { get; set; }
}

/// <summary>
/// Options for UDP multiword tokens. This specifies the minimum and maximum
/// counts of child tokens to match, the properties (UPOS, XPOS, Feats) of
/// children tokens to match (tokens are tested in their order), and the
/// target POS tag to build.
/// </summary>
public class UdpMultiwordTokenOptions
{
    /// <summary>
    /// The minimum number of child tokens to match, or 0 to allow any number.
    /// </summary>
    public int MinCount { get; set; }

    /// <summary>
    /// The maximum number of child tokens to match, or 0 to allow any number.
    /// </summary>
    public int MaxCount { get; set; }

    /// <summary>
    /// The children tokens to match.
    /// </summary>
    public List<UdpChildTokenOptions> Tokens { get; set; } = [];

    /// <summary>
    /// The POS data for the target token to build.
    /// </summary>
    public required UdpMultiwordTokenTargetOptions Target { get; set; }
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

    /// <summary>
    /// Gets or sets the language to target. When not empty, only tokens
    /// having the specified language will be enriched with POS tagger data.
    /// When empty, all tokens with a null language will be enriched.
    /// When null, all tokens will be enriched.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// The UDP tags to preserve when the token already has them.
    /// Default is <c>PROPN</c> and <c>ABBR</c>.
    /// </summary>
    public HashSet<string> PreservedTags { get; set; } =
    [
        "PROPN", "ABBR"
    ];

    /// <summary>
    /// The multiword token options. This is used to deal with UDP multiword
    /// tokens, like in Italian "della" = "di la". In this case typically the
    /// POS tagger expands the multiword token into its children tokens,
    /// marking the multiword token (with its original text range) as such,
    /// while its children tokens follow it and have no text range. In this case,
    /// the token filter matches the multiword token, which is the only present
    /// in the text being processed, but collects its POS data from its children
    /// tokens; it then builds a single POS tag by variously combining POS tags
    /// from children, as specified by objects in this list.
    /// </summary>
    public List<UdpMultiwordTokenOptions>? Multiwords { get; set; }
}
