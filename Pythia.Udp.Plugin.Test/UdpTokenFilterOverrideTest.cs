using Conllu;
using Fusi.Tools;
using Fusi.Tools.Text;
using Pythia.Core;
using Pythia.Core.Plugin.Analysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Pythia.Udp.Plugin.Test;

// These tests do not require network access: they build UdpChunk/Sentence/
// Token objects manually, bypassing UdpTextFilter's UDPipe service call, so
// they can exercise UdpTokenFilter's override logic in isolation.
public sealed class UdpTokenFilterOverrideTest
{
    private const string OVERRIDE_KEY = "abbr-ranges";

    private static Token CreateMatchedToken(string misc)
    {
        return new Token
        {
            Lemma = "eg",
            Upos = "NOUN",
            Xpos = "S",
            Feats = new Dictionary<string, string> { ["Number"] = "Sing" },
            Head = 3,
            DepRel = "obj",
            Misc = misc
        };
    }

    private static (DataDictionary context, TextSpan token) CreateContext(
        Token matched, TextRange? overrideRange)
    {
        Sentence sentence = new();
        sentence.Tokens.Add(matched);

        UdpChunk chunk = new(new TextRange(0, 20));
        chunk.Sentences.Add(sentence);

        DataDictionary context = new();
        context.Data[UdpTextFilter.UDP_KEY] = new List<UdpChunk> { chunk };

        if (overrideRange != null)
        {
            context.Data[OVERRIDE_KEY] = new List<XmlTagListEntry>
            {
                new("abbr", overrideRange.Value)
            };
        }

        TextSpan token = new()
        {
            Index = 4,
            Length = 2,
            Value = "eg",
            Pos = "PROPN"
        };

        return (context, token);
    }

    private static UdpTokenFilter CreateFilter()
    {
        UdpTokenFilter filter = new();
        filter.Configure(new UdpTokenFilterOptions
        {
            Props = UdpTokenProps.All,
            Overrides = new Dictionary<string, UdpTokenOverride>
            {
                [OVERRIDE_KEY] = new UdpTokenOverride
                {
                    Upos = "X",
                    Feats = new Dictionary<string, string> { ["Abbr"] = "Yes" }
                }
            }
        });
        return filter;
    }

    [Fact]
    public async Task ApplyAsync_OverrideOverlapping_OverridesUposAndFeats()
    {
        Token matched = CreateMatchedToken("TokenRange=4:6");
        // overlaps the matched token's document range (4,2)
        var (context, token) = CreateContext(matched, new TextRange(4, 6));

        UdpTokenFilter filter = CreateFilter();
        await filter.ApplyAsync(token, 1, context);

        // upos overridden, even though old pos "PROPN" is a preserved tag
        // and differs from matched.Upos "NOUN" (override bypasses the
        // preserved-tags policy)
        Assert.Equal("X", token.Pos);

        // xpos not overridden (override.Xpos is null): falls back to
        // matched's own xpos
        Corpus.Core.Attribute? xpos = token.Attributes!
            .FirstOrDefault(a => a.Name == "xpos");
        Assert.NotNull(xpos);
        Assert.Equal("S", xpos.Value);

        // feats fully replaced by the override's feats
        Assert.DoesNotContain(token.Attributes!, a => a.Name == "number");
        Corpus.Core.Attribute? abbr = token.Attributes!
            .FirstOrDefault(a => a.Name == "abbr");
        Assert.NotNull(abbr);
        Assert.Equal("yes", abbr.Value);

        // properties not covered by the override still come from matched
        Assert.Equal("eg", token.Lemma);
        Assert.Equal("3", token.Attributes!
            .First(a => a.Name == "head").Value);
        Assert.Equal("obj", token.Attributes!
            .First(a => a.Name == "deprel").Value);
    }

    [Fact]
    public async Task ApplyAsync_OverrideNotOverlapping_UsesTaggerDataAndPreservesPos()
    {
        Token matched = CreateMatchedToken("TokenRange=4:6");
        // does NOT overlap the matched token's document range (4,2)
        var (context, token) = CreateContext(matched, new TextRange(100, 5));

        UdpTokenFilter filter = CreateFilter();
        await filter.ApplyAsync(token, 1, context);

        // no override applies here, and old pos "PROPN" is a preserved tag
        // differing from matched.Upos "NOUN", so the token must be left
        // untouched entirely
        Assert.Equal("PROPN", token.Pos);
        Assert.Null(token.Attributes);
    }

    [Fact]
    public async Task ApplyAsync_OverrideOverlapping_BypassesMultiwordHandling()
    {
        Token matched = CreateMatchedToken("TokenRange=4:6");
        matched.Identifier = new TokenIdentifier("3-4");
        Assert.True(matched.IsMultiwordToken);

        var (context, token) = CreateContext(matched, new TextRange(4, 6));

        UdpTokenFilter filter = CreateFilter();
        await filter.ApplyAsync(token, 1, context);

        // even though matched is a multiword token (with no children
        // collected here), the override applies directly instead of
        // going through multiword reconciliation
        Assert.Equal("X", token.Pos);
        Corpus.Core.Attribute? abbr = token.Attributes!
            .FirstOrDefault(a => a.Name == "abbr");
        Assert.NotNull(abbr);
        Assert.Equal("yes", abbr.Value);
    }
}
