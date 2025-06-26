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

    private static readonly Regex _superlativeRegex = new(@"issim([oaie])\b*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _ruleBRegex = 
        new("^(?:da|di|fa|sta|va)(mmi|tti|llo|lle|lla|lli|cci|nne)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _ruleCRegex = new(".o(([ctv]i|l[oeai]|gli))$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _ruleGRegex = new(".[ei](si)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _elidedRegex = new(@"[a-zA-Z](')\b*$",
        RegexOptions.Compiled);

    private static readonly Regex _iscRegex = new("^is[ptc].", RegexOptions.Compiled);

    private static readonly Regex _truncableRegex = new(".*[aeiou].*[lrmn]$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> _truncatedA =
    [
        "suor",
        "or",
        "allor",
        "ancor",
        "finor",
        "ognor",
        "sinor",
        "talor"
    ];

    // 02CB = modifier letter grave accent
    // 02CA = modifier letter acute accent
    private static readonly Regex _rAccented = new(@"([aeiou])(['`´\u02cb\u02ca])$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> _monoImpt =
    [
        "da", "di", "fa", "sta", "va"
    ];

    private static readonly HashSet<string> _enclitics =
    [
        "gliela", "glieli", "glielo", "gliene", "gliele",
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
    ];
    #endregion

    private ItalianVariantBuilderOptions? _options;
    private readonly List<VariantForm> _variants;
    private readonly LookupFilter _filter;
    private readonly ItalianPosTagBuilder _posBuilder = new();
    private ILookupIndex? _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItalianVariantBuilder"/>
    /// class.
    /// </summary>
    public ItalianVariantBuilder()
    {
        _variants = [];
        _filter = new LookupFilter
        {
            PageSize = 0
        };
        _options = new ItalianVariantBuilderOptions
        {
            AccentArtifacts = true,
            AccentedVariants = true,
            ApostropheArtifacts = true,
            EncliticGroups = true,
            IotaVariants = true,
            IscVariants = true,
            Superlatives = true,
            UnelidedVariants = true,
            UntruncatedVariants = true
        };
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
        _filter.Value = value;
        return _index!.Find(_filter).Items;
    }

    private static ItalianPosTagBuilder GetTagWithFeature(string pos,
        string name, string value)
    {
        ItalianPosTagBuilder builder = new();
        ItalianPosTagBuilder tag = new(builder.Parse(pos)!);
        tag.Features[name] = value;
        return tag;
    }

    #region Superlatives
    private void FindSuperlatives(string word)
    {
        // check if word ends with -issimo/a/i/e
        Match m = _superlativeRegex.Match(word);
        if (!m.Success) return;

        // remove ending
        string theme = word[..m.Index];

        // remove -h if we are not going to add a palatal vocoid
        if (theme.EndsWith('h') &&
            !IsPalatalVocoid(m.Groups[1].Value[0]))
        {
            theme = theme[..^1];
        }

        // build the hypothetical positive grade and find it
        string positive = theme + m.Groups[1].Value;
        IList<LookupEntry> entries = Lookup(positive);

        // if not found, try a positive with -e ending (like insigne, abile,
        // lacrimevole, etc)
        if (entries.Count == 0)
        {
            positive = theme + "e";
            entries = Lookup(positive);
            // if not found, give up
            if (entries.Count == 0) return;
        }

        // add a variant for each ADJ entry found
        foreach (LookupEntry entry in entries)
        {
            if (entry.Pos!.StartsWith(UDTags.ADJ))
            {
                VariantForm variant = new()
                {
                    Value = positive,
                    Type = "super",
                    Source = word,
                    Pos = entry.Pos
                };

                // add superlative to variant POS
                variant.Pos = GetTagWithFeature(
                    entry.Pos, UDTags.FEAT_DEGREE, UDTags.DEGREE_SUPERLATIVE)
                    .Build();

                _variants.Add(variant);
            }
        }
    }
    #endregion

    #region Enclitics
    private bool AddMatchingRecords(string word, string type,
        string source, string? pos, params string[] features)
    {
        // find matching entries, nope if no results
        int initialCount = _variants.Count;
        IList<LookupEntry> entries = Lookup(word);
        if (entries.Count == 0) return false;

        // add variants for each entry found, filtering by POS if specified
        if (pos != null)
        {
            _variants.AddRange(from entry in entries
                               where entry.Pos != null &&
                               _posBuilder.Parse(entry.Pos)?
                                    .IsMatch(pos, features) == true
                               select new VariantForm(entry, type, source));
        }
        else
        {
            _variants.AddRange(from entry in entries
                               where entry.Pos != null
                               select new VariantForm(entry, type, source));
        }
        return _variants.Count > initialCount;
    }

    private bool AddMatchingRecords(string word, string type,
        string source, string? pos, string featuresQuery)
    {
        // find matching entries, nope if no results
        int initialCount = _variants.Count;
        IList<LookupEntry> entries = Lookup(word);
        if (entries.Count == 0) return false;

        // add variants for each entry found, filtering by POS if specified
        if (pos != null)
        {
            _variants.AddRange(from entry in entries
                               where entry.Pos != null &&
                               _posBuilder.Parse(entry.Pos)?
                                    .IsMatch(pos, featuresQuery) == true
                               select new VariantForm(entry, type, source));
        }
        else
        {
            _variants.AddRange(from entry in entries
                               where entry.Pos != null
                               select new VariantForm(entry, type, source));
        }
        return _variants.Count > initialCount;
    }

    private static string? StripEndingEnclitics(string word, string? prefix)
    {
        foreach (string ending in _enclitics)
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
        // 	glielo gliela glieli gliele gliene
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
        Match m = _ruleBRegex.Match(word);
        if (m.Success && AddMatchingRecords(string.Concat(
            word.AsSpan(0, m.Groups[1].Index), "'"), type, word,
            UDTags.VERB, UDTags.FEAT_MOOD, UDTags.MOOD_IMPERATIVE))
        {
            return;
        }

        string? theme = StripEndingEnclitics(word, null);
        if (theme != null)
        {
            if (_monoImpt.Contains(theme))
            {
                theme += "'";
            }
            if (AddMatchingRecords(theme, type, word,
                UDTags.VERB, UDTags.FEAT_MOOD, UDTags.MOOD_IMPERATIVE))
            {
                return;
            }
        }

        // rule C
        // o(ci)$
        // remove $1 and find; among found, get Cg 1p only.
        m = _ruleCRegex.Match(word);
        if (m.Success &&
            AddMatchingRecords(word[..m.Groups[1].Index], type,
            word, UDTags.VERB,
            UDTags.FEAT_MOOD, UDTags.MOOD_SUBJUNCTIVE,
            UDTags.FEAT_PERSON, UDTags.PERSON_FIRST,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL))
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
            // first try with type "rre" and then with "re" because the former
            // is longer (otherwise, a search for "porgli" would find "por"
            // (Inf.with apocope) + "e" instead of "porre")
            string inf = theme + "re";
            if (AddMatchingRecords(inf, type, word,
                UDTags.VERB, UDTags.FEAT_VERBFORM, UDTags.VERBFORM_INFINITIVE))
            {
                return;
            }
            inf = theme + "e";
            if (AddMatchingRecords(inf, type, word,
                UDTags.VERB, UDTags.FEAT_VERBFORM, UDTags.VERBFORM_INFINITIVE))
            {
                return;
            }
        }

        // rules E,F
        // (Q1/Q2/Q3/Q4)$
        // remove $1 and find; among found, get Ger/Partpass only.
        theme = StripEndingEnclitics(word, null);
        if (theme != null && AddMatchingRecords(theme, type, word,
            UDTags.VERB, $"{UDTags.FEAT_VERBFORM}={UDTags.VERBFORM_GERUND} OR " +
                $"({UDTags.FEAT_VERBFORM}={UDTags.VERBFORM_PARTICIPLE} AND" +
                $"{UDTags.FEAT_TENSE}={UDTags.TENSE_PAST})"))
        {
            return;
        }

        // rule G
        // [ei](si)$
        // remove $1 and find; among found, get Partpres only.
        m = _ruleGRegex.Match(word);
        if (m.Success)
        {
            AddMatchingRecords(word[..m.Groups[1].Index], type,
                word, UDTags.VERB,
                UDTags.FEAT_VERBFORM, UDTags.VERBFORM_PARTICIPLE,
                UDTags.FEAT_TENSE, UDTags.TENSE_PRESENT);
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
        Match m = _elidedRegex.Match(word);
        if (!m.Success) return;

        // try replacing apostrophe with -o, -e, -i, -a (unaccented)
        foreach (char c in "oeia")
        {
            string unelided = BuildUnelidedForm(word, m.Groups[1].Index, c);

            IList<LookupEntry> entries = Lookup(unelided);
            if (entries?.Count > 0)
            {
                _variants.AddRange(from entry in entries
                    select new VariantForm(entry, "elided", word));
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
            IList<LookupEntry> entries = Lookup(sb.ToString());
            if (entries?.Count > 0)
            {
                _variants.AddRange(from entry in entries
                    select new VariantForm
                    {
                        Value = sb.ToString(),
                        Type = sType,
                        Source = word,
                        Pos = entry.Pos
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
        // (c) final -V must be preceded by l/r/m/n (andiam). If double,
        //     it becomes simple (tor di Quinto).

        Match m = _truncableRegex.Match(word);
        if (!m.Success) return;

        // try with a
        if (_truncatedA.Contains(word))
        {
            string plusA = word + "a";
            IList<LookupEntry> entries = Lookup(plusA);
            if (entries?.Count > 0)
            {
                _variants.AddRange(from entry in entries
                    select new VariantForm
                    {
                        Value = plusA,
                        Type = "untruncated",
                        Source = word,
                        Pos = entry.Pos
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
        IList<LookupEntry> entries = Lookup(iota);
        if (entries?.Count > 0)
        {
            _variants.AddRange(from entry in entries
                select new VariantForm
                {
                    Value = iota,
                    Type = "iota",
                    Source = word,
                    Pos = entry.Pos
                });
        }
    }

    private void FindIscVariants(string word)
    {
        if (!_iscRegex.IsMatch(word)) return;

        string variant = word[1..];
        IList<LookupEntry> entries = Lookup(variant);

        if (entries?.Count > 0)
        {
            _variants.AddRange(from entry in entries
                select new VariantForm
                {
                    Value = variant,
                    Type = "isc",
                    Source = word,
                    Pos = entry.Pos
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
        List<int> indexes = [];
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
            IList<LookupEntry> entries = Lookup(variant);
            if (entries?.Count > 0)
            {
                _variants.AddRange(from entry in entries
                    select new VariantForm
                    {
                        Value = variant,
                        Type = "acute-grave",
                        Source = word,
                        Pos = entry.Pos
                    });
            }
        }
    }
    #endregion

    #region Artifacts
    private bool AddIfFound(string value, string type, string word)
    {
        IList<LookupEntry> entries = Lookup(value);
        if (entries?.Count > 0)
        {
            _variants.AddRange(from entry in entries
                select new VariantForm
                {
                    Value = value,
                    Type = type,
                    Source = word,
                    Pos = entry.Pos
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
        // assuming that the lookup index has normalized forms like
        // cittá as città
        bool isAcute = (c1 == 'e' || c1 == 'o') &&
            (c2 == '´' || c2 == '\u02ca');
        int i = VOWELS.IndexOf(c1);
        string accented = word[..m.Index] +
            (isAcute ? VOWELS_ACUTE[i] : VOWELS_GRAVE[i]);
        if (AddIfFound(accented, type, word))
        {
            return;
        }

        // not found: if we are allowed to search for mismatched accents,
        // try with the opposite
        if (_options?.AccentedVariants == true)
        {
            accented = word[..m.Index] +
                (isAcute ? VOWELS_GRAVE[i] : VOWELS_ACUTE[i]);
            AddIfFound(accented, type, word);
        }
    }
    #endregion

    /// <summary>
    /// Finds the variant(s) of a specified word.
    /// </summary>
    /// <param name="word">The word.</param>
    /// <param name="pos">The optional part of speech for the word.</param>
    /// <param name="index">The lookup index to be used.</param>
    /// <returns>variant(s)</returns>
    /// <exception cref="ArgumentNullException">null word</exception>
    public IList<VariantForm> Build(string word, string? pos, ILookupIndex index)
    {
        ArgumentNullException.ThrowIfNull(word);
        _index = index ?? throw new ArgumentNullException(nameof(index));

        _variants.Clear();

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
