using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fusi.Tools.Configuration;
using Pythia.Tagger.Lookup;

namespace Pythia.Tagger.Ita.Plugin;

/// <summary>
/// Variants builder for Italian.
/// Tag: <c>variant-builder.ita</c>.
/// </summary>
[Tag("variant-builder.ita")]
public sealed class ItalianVariantBuilder : IVariantBuilder,
    IConfigurable<ItalianVariantBuilderOptions>
{
    #region Constants
    private const string VOWELS = "aeiou";
    private const string VOWELS_ACUTE = "áéíóú";
    private const string VOWELS_GRAVE = "àèìòù";

    private static readonly Regex _rSuperlative = new(@"issim([oaie])\b*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _rRuleB = 
        new("^(?:da|di|fa|sta|va)(mmi|tti|llo|lle|lla|lli|cci|nne)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _rSigImpt = new("^V[^@]*@.*Mt", RegexOptions.Compiled);

    private static readonly Regex _rRuleC = new(".o(([ctv]i|l[oeai]|gli))$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _rSigCg1P = new("^V[^@]*@.*Mj.*NpP1", RegexOptions.Compiled);

    private static readonly Regex _rSigInf = new("^V[^@]*@.*Mf", RegexOptions.Compiled);
    private static readonly Regex _rSigGerOrPastPart = new("^V[^@]*@.*(Mg|MpTr)",
        RegexOptions.Compiled);

    private static readonly Regex _rRuleG = new(".[ei](si)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _rSigPresPart = new("^V[^@]*@.*MpTe", RegexOptions.Compiled);

    private static readonly Regex _rElided = new(@"[a-zA-Z](')\b*$", RegexOptions.Compiled);

    private static readonly Regex _rIsc = new("^is[ptc].", RegexOptions.Compiled);

    private static readonly Regex _rTruncable = new(".*[aeiou].*[lrmn]$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> _hashTruncatedA = new()
    {
        "suor",
        "or",
        "allor",
        "ancor",
        "finor",
        "ognor",
        "sinor",
        "talor"
    };

    // 02CB = modifier letter grave accent
    // 02CA = modifier letter acute accent
    private static readonly Regex _rAccented = new(@"([aeiou])(['`´\u02cb\u02ca])$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> _hashMonoImpt = new()
    {
        "da", "di", "fa", "sta", "va"
    };

    private static readonly HashSet<string> _hashEnclitics = new()
    {
        "gliela", "glieli", "glielo", "gliene", "glele",
        "cela", "cele", "celi", "celo", "cene",
        "mela", "mele", "meli", "melo", "mene",
        "sela", "sele", "seli", "selo", "sene",
        "tela", "tele", "teli", "telo", "tene",
        "vela", "vele", "veli", "velo", "vene",
        "gli",
        "ci",
        "la", "le", "li", "lo",
        "mi",
        "ne",
        "si",
        "ti",
        "vi"
    };
    #endregion

    private ItalianVariantBuilderOptions? _options;
    private readonly List<Variant> _variants;
    private readonly LookupFilter _filter;
    private readonly Dictionary<string, IList<LookupEntry>> _lookupCache;
    private ILookupIndex? _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItalianVariantBuilder"/>
    /// class.
    /// </summary>
    public ItalianVariantBuilder()
    {
        _variants = new List<Variant>();
        _filter = new LookupFilter
        {
            PageSize = 100
        };
        _lookupCache = new Dictionary<string, IList<LookupEntry>>();
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(ItalianVariantBuilderOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    private static bool IsPalatalVocoid(char c)
    {
        // only e/i or acute/grave should be allowed,
        // but here we can be a bit looser.
        return "eiéèêëíìîï".IndexOf(c) > -1;
    }

    private IList<LookupEntry> Lookup(string value)
    {
        if (_lookupCache.ContainsKey(value))
            return _lookupCache[value];

        _filter.Value = value;
        var entries = _index!.Find(_filter);
        _lookupCache[value] = entries;
        return entries;
    }

    #region Superlatives
    private void FindSuperlatives(string word)
    {
        Match m = _rSuperlative.Match(word);
        if (!m.Success) return;

        // remove ending
        string theme = word[..m.Index];

        // remove -h if we are not going to add a palatal vocoid
        if (theme.EndsWith("h", StringComparison.Ordinal) &&
            !IsPalatalVocoid(m.Groups[1].Value[0]))
        {
            theme = theme[..^1];
        }

        // get the positive grade and find it
        string positive = theme + m.Groups[1].Value;

        var entries = Lookup(positive);
        if (entries == null || entries.Count == 0)
        {
            // try again with -e ending (like insigne, abile, lacrimevole, etc)
            positive = theme + "e";
            entries = Lookup(positive);
            if (entries == null || entries.Count == 0) return;
        }

        foreach (LookupEntry entry in entries)
        {
            if (entry.Signature?.StartsWith("A", StringComparison.Ordinal)
                == true)
            {
                Variant variant = new()
                {
                    Value = positive,
                    Type = "super",
                    Source = word,
                    Signature = entry.Signature
                };

                // add Rs to abbreviated signature, which has no R in data store
                int i = entry.Signature.IndexOf('@');
                if (i > -1)
                {
                    variant.Signature = string.Concat(
                        entry.Signature.AsSpan(0, i + 1),
                        "Rs",
                        entry.Signature.AsSpan(i + 1));
                }
                _variants.Add(variant);
            }
        }
    }
    #endregion

    #region Enclitics
    private bool AddMatchingRecords(string word, string type, string source,
        Regex sigFilter)
    {
        int initialCount = _variants.Count;
        var entries = Lookup(word);
        if (entries == null || entries.Count == 0) return false;

        _variants.AddRange(from entry in entries
            where entry.Signature != null && sigFilter.IsMatch(entry.Signature)
            select new Variant(entry, type, source));
        return _variants.Count > initialCount;
    }

    private static string? StripEndingEnclitics(string word, string? prefix)
    {
        foreach (string ending in _hashEnclitics)
        {
            if (word.EndsWith(ending, StringComparison.Ordinal))
            {
                if (prefix == null) return word[..^ending.Length];

                int i = word.Length - (ending.Length + prefix.Length);
                if (i >= prefix.Length &&
                    prefix == word.Substring(i, prefix.Length))
                {
                    return word[..^ending.Length];
                }
            }
        }

        return null;
    }

    private void FindEncliticGroups(string word)
    {
        #region Theory
        // Serianni p.247
        // http://www.treccani.it/enciclopedia/parole-enclitiche_(Enciclopedia_dell'Italiano)/
        // http://www.treccani.it/vocabolario/enclisi/
        // List of enclitics:
        // Q1 mi, ti, lo, le, la, li, ci, ne, gli
        // Q2 vi
        // Q3 si
        // Combinations effectively used:
        // me te glie se ce ve + lo/la/li/le/ne:
        // Q4:
        // 	melo mela meli mele mene
        // 	telo tela teli tele tene
        // 	glielo gliela glieli glele gliene
        // 	selo sela seli sele sene
        // 	celo cela celi cele cene
        // 	velo vela veli vele vene
        // Rules:
        // a) ecco + Q1/Q2/Q4 (in list additions)
        // b) Impt (leggilo)
        // 	-da' di' fa' sta' va' + Q1: C1 doubled if CV (dammi; in list additions)
        // c) Cg 1p: -ci (fermiamoci); also (added by myself): ti/lo/le/la/li/gli/vi
        // d) Inf: -V > 0 (*andareci > andarci) e -rrV > -r (*porregli > porgli)
        // e) Ger (avendomi)
        // f) Part pass (allontanatomi)
        // g) Part pres: (quasi) solo -si (intrecciantesi)
        #endregion

        const string type = "enclitic";

        // rules B1 and B2:
        // ^(da|di|fa|sta|va)(mmi|tti|llo|lle|lla|lli|cci|nne)$
        // -(clitics...)
        // remove $1 and find; among found, get Impt only.
        Match m = _rRuleB.Match(word);
        if (m.Success && AddMatchingRecords(string.Concat(
            word.AsSpan(0, m.Groups[1].Index), "'"), type, word, _rSigImpt))
        {
            return;
        }

        string? theme = StripEndingEnclitics(word, null);
        if (theme != null)
        {
            if (_hashMonoImpt.Contains(theme))
            {
                theme += "'";
            }
            if (AddMatchingRecords(theme, type, word, _rSigImpt))
            {
                return;
            }
        }

        // rule C
        // o(ci)$
        // remove $1 and find; among found, get Cg 1p only.
        m = _rRuleC.Match(word);
        if (m.Success &&
            AddMatchingRecords(word[..m.Groups[1].Index], type,
            word, _rSigCg1P))
        {
            return;
        }

        // rule D
        // r(Q1/Q2/Q3/Q4)$
        // remove $1, append "re" and find; among found, get Inf only.
        // if not found, change -rre in -re and find; among found, get Inf only.
        theme = StripEndingEnclitics(word, "r");
        if (theme != null)
        {
            // first try with type "rre" and then with "re" because the former is longer
            // (otherwise, a search for "porgli" would find "por" (Inf.with apocope) + "e"
            // instead of "porre")
            string inf = theme + "re";
            if (AddMatchingRecords(inf, type, word, _rSigInf))
            {
                return;
            }
            inf = theme + "e";
            if (AddMatchingRecords(inf, type, word, _rSigInf))
            {
                return;
            }
        }

        // rules E,F
        // (Q1/Q2/Q3/Q4)$
        // remove $1 and find; among found, get Ger/Partpass only.
        theme = StripEndingEnclitics(word, null);
        if (theme != null && AddMatchingRecords(theme, type, word,
            _rSigGerOrPastPart))
        {
            return;
        }

        // rule G
        // [ei](si)$
        // remove $1 and find; among found, get Partpres only.
        m = _rRuleG.Match(word);
        if (m.Success)
        {
            AddMatchingRecords(word[..m.Groups[1].Index], type,
                word, _rSigPresPart);
        }
    }
    #endregion

    #region Elisions
    private static string BuildUnelidedForm(string word, int index, char elided)
    {
        return word[..index] +
               elided +
               (index + 1 < word.Length
                   ? word[(index + 1)..]
                   : "");
    }

    private void FindUnelidedVariants(string word)
    {
        Match m = _rElided.Match(word);
        if (!m.Success) return;

        // try replacing apostrophe with -o, -e, -i, -a (unaccented)
        foreach (char c in "oeia")
        {
            string unelided = BuildUnelidedForm(word, m.Groups[1].Index, c);

            var entries = Lookup(unelided);
            if (entries?.Count > 0)
            {
                _variants.AddRange(from entry in entries
                    select new Variant(entry, "elided", word));
            }
        }
    }
    #endregion

    #region Truncation
    private bool TryTruncatedWithEio(string left, string word)
    {
        const string sType = "untruncated";

        // try adding e/i/o
        StringBuilder sb = new(left + "_");
        foreach (char c in "eio")
        {
            sb[^1] = c;
            var entries = Lookup(sb.ToString());
            if (entries?.Count > 0)
            {
                _variants.AddRange(from entry in entries
                    select new Variant
                    {
                        Value = sb.ToString(),
                        Type = sType,
                        Source = word,
                        Signature = entry.Signature
                    });
                return true;
            }
        }

        return false;
    }

    // http://www.treccani.it/vocabolario/troncamento/
    private void FindUntruncatedVariants(string word)
    {
        // (a) word must contain at least 2 vowels
        // (b) word must end in -e/i/o 
        //     (in -a only: suor, or, allor, ancor, finor, ognor, sinor, talor).
        // (c) final -V must be preceded by l/r/m/n (andiam). If double, it becomes simple
        //     (tor di Quinto).

        Match m = _rTruncable.Match(word);
        if (!m.Success) return;

        // try with a
        if (_hashTruncatedA.Contains(word))
        {
            string sPlusA = word + "a";
            var entries = Lookup(sPlusA);
            if (entries?.Count > 0)
            {
                _variants.AddRange(from entry in entries
                    select new Variant
                    {
                        Value = sPlusA,
                        Type = "untruncated",
                        Source = word,
                        Signature = entry.Signature
                    });
                return;
            }
        }

        // try adding e/i/o (cuor > cuore)
        if (TryTruncatedWithEio(word, word)) return;

        // try redoubling C and adding e/i/o (tor > torre)
        string left = word + word[^1];
        TryTruncatedWithEio(left, word);
    }
    #endregion

    #region Ancient
    private void FindIotaVariants(string word)
    {
        if (!word.Contains('j')) return;

        string iota = word.Replace('j', 'i');
        var entries = Lookup(iota);
        if (entries?.Count > 0)
        {
            _variants.AddRange(from entry in entries
                select new Variant
                {
                    Value = iota,
                    Type = "iota",
                    Source = word,
                    Signature = entry.Signature
                });
        }
    }

    private void FindIscVariants(string word)
    {
        if (!_rIsc.IsMatch(word)) return;

        string variant = word[1..];
        var entries = Lookup(variant);

        if (entries?.Count > 0)
        {
            _variants.AddRange(from entry in entries
                select new Variant
                {
                    Value = variant,
                    Type = "isc",
                    Source = word,
                    Signature = entry.Signature
                });
        }
    }

    private static char InvertAccent(char c)
    {
        return c switch
        {
            'à' => 'á',
            'á' => 'à',
            'è' => 'é',
            'é' => 'è',
            'ì' => 'í',
            'í' => 'ì',
            'ò' => 'ó',
            'ó' => 'ò',
            'ù' => 'ú',
            'ú' => 'ù',
            _ => c,
        };
    }

    private void FindAccentedVariants(string word)
    {
        // variants can be generated from accented AEIOU
        // (though the tokenizer should have kept only final accents)
        char[] accented =
        {
            'à', 'á',
            'è', 'é',
            'ì', 'í',
            'ò', 'ó',
            'ù', 'ú'
        };
        if (word.All(c => !accented.Contains(c))) return;

        // collect all their indexes in the original string
        List<int> indexes = new();
        for (int x = 0; x < word.Length; x++)
            if (accented.Contains(word[x])) indexes.Add(x);

        // try all the accents permutations as variants
        int max = (1 << indexes.Count) - 1;
        StringBuilder sb = new(word);

        for (int permutation = 0; permutation <= max; permutation++)
        {
            for (int bit = 1 << (indexes.Count - 1), i = 0;
                 bit > 0; bit >>= 1, i++)
            {
                if ((bit & permutation) != 0)
                    sb[indexes[i]] = InvertAccent(word[indexes[i]]);
            }

            string variant = sb.ToString();
            var entries = Lookup(variant);
            if (entries?.Count > 0)
            {
                _variants.AddRange(from entry in entries
                    select new Variant
                    {
                        Value = variant,
                        Type = "acute-grave",
                        Source = word,
                        Signature = entry.Signature
                    });
            }
        }
    }
    #endregion

    #region Artifacts
    private bool AddIfFound(string value, string type, string word)
    {
        var entries = Lookup(value);
        if (entries?.Count > 0)
        {
            _variants.AddRange(from entry in entries
                select new Variant
                {
                    Value = value,
                    Type = type,
                    Source = word,
                    Signature = entry.Signature
                });
            return true;
        }

        return false;
    }

    private void FindApostropheArtifacts(string word)
    {
        const string type = "apostrophe";

        // beginning with ': try without it
        if (word.Length > 1 && word[0] == '\'')
        {
            string s = word[1..];
            AddIfFound(s, type, word);
        }

        // ending with ': try without it
        if (word.Length > 1 && word[^1] == '\'')
        {
            string s = word[..^1];
            AddIfFound(s, type, word);
        }

        // beginning and ending with ': try without them
        if (word.Length > 2 && word[0] == '\'' && word[^1] == '\'')
        {
            string s = word[1..^1];
            AddIfFound(s, type, word);
        }
    }

    private void FindAccentArtifacts(string word)
    {
        const string type = "accent";

        Match m = _rAccented.Match(word);
        if (!m.Success) return;

        char c1 = m.Groups[1].Value[0];
        char c2 = m.Groups[2].Value[0];

        // try with acute only when accent is not grave and letter is e/o,
        // assuming that the lookup index has normalized forms like cittá as città
        bool bIsAcute = (c1 == 'e' || c1 == 'o') &&
            (c2 == '´' || c2 == '\u02ca');
        int i = VOWELS.IndexOf(c1);
        string accented = word[..m.Index] +
            (bIsAcute ? VOWELS_ACUTE[i] : VOWELS_GRAVE[i]);
        if (AddIfFound(accented, type, word))
        {
            return;
        }

        // not found: if we are allowed to search for mismatched accents, try with the opposite
        if (_options?.AccentedVariants == true)
        {
            accented = word[..m.Index] +
                (bIsAcute ? VOWELS_GRAVE[i] : VOWELS_ACUTE[i]);
            AddIfFound(accented, type, word);
        }
    }
    #endregion

    /// <summary>
    /// Finds the variant(s) of a specified word.
    /// </summary>
    /// <param name="word">The word.</param>
    /// <param name="index">The lookup index to be used.</param>
    /// <returns>variant(s)</returns>
    /// <exception cref="ArgumentNullException">null word</exception>
    public IList<Variant> Build(string word, ILookupIndex index)
    {
        if (word == null) throw new ArgumentNullException(nameof(word));
        _index = index ?? throw new ArgumentNullException(nameof(index));

        _variants.Clear();
        _lookupCache.Clear();

        // try with superlatives
        if (_options?.Superlatives == true) FindSuperlatives(word);

        // try with enclitics
        if (_options?.EncliticGroups == true) FindEncliticGroups(word);

        // try with truncated
        if (_options?.UntruncatedVariants == true) FindUntruncatedVariants(word);

        // try with elisions
        if (_options?.UnelidedVariants == true) FindUnelidedVariants(word);

        // try without apostrophes. Such variants are rather artifacts
        // due to apostrophes misused as quotes (e.g. "'prova' disse")
        if (_options?.ApostropheArtifacts == true) FindApostropheArtifacts(word);

        // try with accent artifacts (e.g. citta')
        if (_options?.AccentArtifacts == true) FindAccentArtifacts(word);

        // try with i instead of j (e.g. effluvj)
        if (_options?.IotaVariants == true) FindIotaVariants(word);

        // try without initial i in type is- + voiceless plosive (e.g. iscoprire)
        if (_options?.IscVariants == true) FindIscVariants(word);

        // try with different accentuations 
        if (_options?.AccentedVariants == true) FindAccentedVariants(word);

        _index = null;
        return _variants;
    }
}
