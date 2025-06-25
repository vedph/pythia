using Fusi.Tools;
using Pythia.Tagger.Lookup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Pythia.Tagger.Ita.Plugin;

/// <summary>
/// Converter from Morph-It! list of Italian word forms to an
/// <see cref="ILookupIndex"/>. This list is a tab-delimited file with 3
/// columns for each entry representing a word form, its lemma, and its POS
/// tag. The POS tag has the following syntax:
/// <list type="bullet">
/// <item>POS tag like <c>VERB</c>;</item>
/// <item><c>:</c> followed by ordered feature values, separated by <c>+</c>.
/// These features may include at their end an extra feature representing the
/// enclitic attached to the form, like e.g. <c>abbaiarmi</c>:
/// <c>VER:inf+pres+mi</c>.</item>
/// </list>
/// <para>For instance, entry <c>assolderebbero assoldare VER:cond+pres+3+p</c>
/// refers to the form <c>assolderebbero</c>, belonging to the lemma
/// <c>assoldare</c>, with POS equal to <c>VER</c> (for VERB) and an ordered
/// set of features for verbs: mood (<c>cond</c>), tense (<c>pres</c>),
/// person (<c>3</c>), and number (<c>p</c>). The tag is transformed into
/// standard Universal Dependencies tags and features during conversion.</para>
/// <para>For Italian UD see https://universaldependencies.org/it/.</para>
/// </summary>
public sealed class MorphitConverter
{
    #region Constants
    /// <summary>
    /// The name of the feature representing enclitics in a POS tag.
    /// This is not a standard UD feature, but a Morph-It! feature.
    /// </summary>
    public const string FEAT_ENCLITIC = "Enclitic";

    private readonly HashSet<string> _enclitics =
    [
        "mi", "ti", "lo", "le", "la", "li", "ci", "ne", "gli",
        "vi", "si",
        "melo", "mela", "meli", "mele", "mene",
        "telo", "tela", "teli", "tele", "tene",
        "glielo", "gliela", "glieli", "glele", "gliene",
        "selo", "sela", "seli", "sele", "sene",
        "celo", "cela", "celi", "cele", "cene",
        "velo", "vela", "veli", "vele", "vene"
    ];

    private readonly Dictionary<string, string[]> _posTags = new()
    {
        // abbreviation
        { "ABL", [UDTags.X] },
        { "ADJ", [UDTags.ADJ] },
        { "ADV", [UDTags.ADV] },
        // articulated preposition like "nel"
        { "ARTPRE-M", [UDTags.ADP, UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE] },
        // articulated preposition like "nella"
        { "ARTPRE-F", [UDTags.ADP, UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE] },
        { "AUX", [UDTags.AUX] },
        // meaning not clear
        { "CE", [UDTags.X] },
        // conjunctions, CCONJ is a best guess
        { "CON", [UDTags.CCONJ] },
        { "DET", [UDTags.DET] },
        { "DET-DEMO",
            [UDTags.DET, UDTags.FEAT_PRONTYPE, UDTags.PRONTYPE_DEMONSTRATIVE] },
        { "DET-INDEF",
            [UDTags.DET, UDTags.FEAT_PRONTYPE, UDTags.PRONTYPE_INDEFINITE] },
        // cardinal numbers are NUM in UD (ordinals are ADJ)
        { "DET-NUM-CARD", [UDTags.NUM] },
        { "DET-POSS", [UDTags.DET, UDTags.FEAT_POSSESSION, UDTags.BINARY_YES] },
        { "INT", [UDTags.INTJ] },
        // modal verb ike "osare"
        { "MOD", [UDTags.VERB] },
        { "NOUN", [UDTags.NOUN] },
        { "NOUN-M", [UDTags.NOUN, UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE] },
        { "NOUN-F", [UDTags.NOUN, UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE] },
        { "NPR", [UDTags.PROPN] },
        // other punctuation (e.g. brackets)
        { "PON", [UDTags.PUNCT] },
        // preposition like "nonostante"
        { "PRE", [UDTags.ADP] },
        // indefinite pronouns
        { "PRO-INDEF-M-S", [
            UDTags.PRON,
            UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_SINGULAR,
        ] },
        { "PRO-INDEF-M-P", [
            UDTags.PRON,
            UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL,
        ] },
        { "PRO-INDEF-F-S", [
            UDTags.PRON,
            UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_SINGULAR,
        ] },
        { "PRO-INDEF-F-P", [
            UDTags.PRON,
            UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL,
        ] },
        // numbers like "10mila", which are also duplicated as DET-NUM-CARD
        { "PRO-NUM", [UDTags.NUM] },
        // clitic personal pronouns
        { "PRO-PERS-CLI-1-M-S", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_FIRST,
            UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_SINGULAR,
        ]},
        { "PRO-PERS-CLI-1-M-P", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_FIRST,
            UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL,
        ]},
        { "PRO-PERS-CLI-2-M-S", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_SECOND,
            UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_SINGULAR,
        ]},
        { "PRO-PERS-CLI-2-M-P", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_SECOND,
            UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL,
        ]},
        { "PRO-PERS-CLI-3-M-S", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_THIRD,
            UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_SINGULAR,
        ]},
        { "PRO-PERS-CLI-3-M-P", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_THIRD,
            UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL,
        ]},
        { "PRO-PERS-CLI-1-F-S", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_FIRST,
            UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_SINGULAR,
        ]},
        { "PRO-PERS-CLI-1-F-P", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_FIRST,
            UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL,
        ]},
        { "PRO-PERS-CLI-2-F-S", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_SECOND,
            UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_SINGULAR,
        ]},
        { "PRO-PERS-CLI-2-F-P", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_SECOND,
            UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL,
        ]},
        { "PRO-PERS-CLI-3-F-S", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_THIRD,
            UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_SINGULAR,
        ]},
        { "PRO-PERS-CLI-3-F-P", [
            UDTags.PRON,
            UDTags.FEAT_PERSON, UDTags.PERSON_THIRD,
            UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL,
        ]},
        // possessive pronouns
        { "PRO-POSS-M-S", [
            UDTags.PRON,
            UDTags.FEAT_POSSESSION, UDTags.BINARY_YES,
            UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_SINGULAR,
        ] },
        { "PRO-POSS-M-P", [
            UDTags.PRON,
            UDTags.FEAT_POSSESSION, UDTags.BINARY_YES,
            UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL,
        ] },
        { "PRO-POSS-F-S", [
            UDTags.PRON,
            UDTags.FEAT_POSSESSION, UDTags.BINARY_YES,
            UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_SINGULAR,
        ] },
        { "PRO-POSS-F-P", [
            UDTags.PRON,
            UDTags.FEAT_POSSESSION, UDTags.BINARY_YES,
            UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE,
            UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL,
        ] },
        // sentence end markers (e.g. "...")
        { "SENT", [UDTags.PUNCT] },
        // smileys
        { "SMI", [UDTags.SYM] },
        { "SYM", [UDTags.SYM] },
        { "VER", [UDTags.VERB] }
    };

    private readonly Dictionary<string, string[]> _featTags = new()
    {
        { "s", [UDTags.FEAT_NUMBER, UDTags.NUMBER_SINGULAR] },
        { "p", [UDTags.FEAT_NUMBER, UDTags.NUMBER_PLURAL] },
        { "m", [UDTags.FEAT_GENDER, UDTags.GENDER_MASCULINE] },
        { "f", [UDTags.FEAT_GENDER, UDTags.GENDER_FEMININE] },
        { "pos", [UDTags.FEAT_DEGREE, UDTags.FEAT_DEGREE] },
        { "sup", [UDTags.FEAT_DEGREE, UDTags.DEGREE_SUP] },
        { "ind", [UDTags.FEAT_MOOD, UDTags.MOOD_INDICATIVE] },
        { "sub", [UDTags.FEAT_MOOD, UDTags.MOOD_SUBJUNCTIVE] },
        { "cond", [UDTags.FEAT_MOOD, UDTags.MOOD_CONDITIONAL] },
        { "impr", [UDTags.FEAT_MOOD, UDTags.MOOD_IMPERATIVE] },
        { "inf", [UDTags.FEAT_VERBFORM, UDTags.VERBFORM_INFINITIVE] },
        { "ger", [UDTags.FEAT_VERBFORM, UDTags.VERBFORM_GERUND] },
        { "part", [UDTags.FEAT_VERBFORM, UDTags.VERBFORM_PARTICIPLE] },
        { "pres", [UDTags.FEAT_TENSE, UDTags.TENSE_PRESENT] },
        { "past", [UDTags.FEAT_TENSE, UDTags.TENSE_PAST] },
        { "impf", [UDTags.FEAT_TENSE, UDTags.TENSE_IMPERFECT] },
        { "fut", [UDTags.FEAT_TENSE, UDTags.TENSE_FUTURE] },
        { "1", [UDTags.FEAT_PERSON, UDTags.PERSON_FIRST] },
        { "2", [UDTags.FEAT_PERSON, UDTags.PERSON_SECOND] },
        { "3", [UDTags.FEAT_PERSON, UDTags.PERSON_THIRD] },
    };
    #endregion

    private readonly ILookupEntrySerializer _serializer;

    /// <summary>
    /// Creates a new instance of the <see cref="MorphitConverter"/> class.
    /// </summary>
    /// <param name="serializer">The serializer to write conversion output.</param>
    /// <exception cref="ArgumentNullException">serializer</exception>
    public MorphitConverter(ILookupEntrySerializer serializer)
    {
        _serializer = serializer
            ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <summary>
    /// Parse a MorphIt POS tag with its features. The POS tag is separated
    /// by its features by a single colon, when present (for instance, <c>ADV</c>
    /// has no features). Features are separated by plus signs.
    /// </summary>
    /// <param name="text">The text of the tag to parse.</param>
    /// <returns>Parsed tag builder.</returns>
    /// <exception cref="InvalidOperationException">invalid token in tag</exception>
    public PosTagBuilder ParseTag(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // extract tag (from start to : excluded) and features (after :)
        int i = text.IndexOf(':');
        string pos;
        List<string> featureTokens = new();

        if (i == -1)
        {
            pos = text;
        }
        else
        {
            pos = text[..i];

            // extract features part (skip the colon)
            string featuresText = text[(i + 1)..];

            // split features by '+' character
            if (!string.IsNullOrEmpty(featuresText))
            {
                featureTokens.AddRange(featuresText.Split('+'));
            }
        }

        // map tag
        if (!_posTags.TryGetValue(pos, out string[]? posAndFeats))
        {
            throw new InvalidOperationException(
                $"Unknown POS in tag: \"{text}\"");
        }

        // create builder and set the UD pos
        ItalianPosTagBuilder builder = new()
        {
            Pos = posAndFeats[0]
        };

        // add features implied by POS tag (if any)
        for (int j = 1; j < posAndFeats.Length; j += 2)
        {
            if (j + 1 < posAndFeats.Length)
                builder.Features[posAndFeats[j]] = posAndFeats[j + 1];
        }

        // process features from tokens
        for (int j = 0; j < featureTokens.Count; j++)
        {
            string token = featureTokens[j];

            // check for Italian enclitics (mi, ti, ci, vi, si, ne):
            // these would typically be the last feature in the list
            if (j == featureTokens.Count - 1 && _enclitics.Contains(token))
            {
                builder.Features[FEAT_ENCLITIC] = token;
                continue;
            }

            // map feature token using _featTags
            if (!_featTags.TryGetValue(token, out string[]? featMapping))
            {
                throw new InvalidOperationException(
                    $"Unknown feature in tag: \"{token}\" from \"{text}\"");
            }

            // add the feature to builder
            if (featMapping.Length >= 2)
            {
                builder.Features[featMapping[0]] = featMapping[1];
            }
        }

        return builder;
    }

    public void Convert(TextReader reader, Stream output,
        CancellationToken cancel,
        IProgress<ProgressReport>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(output);
        ProgressReport? report = progress != null ? new ProgressReport() : null;

        // read the Morph-It! file line by line
        string? line;
        int lineNumber = 0;
        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;

            // skip empty lines
            if (string.IsNullOrWhiteSpace(line)) continue;

            // split the line into parts: word form, lemma, and tag
            string[] parts = line.Split('\t');
            if (parts.Length < 3) continue; // malformed line

            string word = parts[0];
            string lemma = parts[1];
            string tagText = parts[2];

            // parse the tag
            PosTagBuilder tagBuilder = ParseTag(tagText);
            LookupEntry entry = new()
            {
                Id = lineNumber,
                Value = word,
                Lemma = lemma,
                Pos = tagBuilder.ToString()
            };

            // write the entry to the output
            _serializer.Serialize(output, entry);

            // report progress
            if (lineNumber % 100 == 0 && progress != null)
            {
                report!.Message = $"Processed {lineNumber} lines.";
                progress.Report(report);
            }

            // check for cancellation
            if (cancel.IsCancellationRequested) return;
        }

        if (progress != null)
        {
            report!.Message = $"Conversion completed. Processed {lineNumber} lines.";
            progress.Report(report);
        }

        output.Flush();
    }
}
