namespace Pythia.Tagger;

/// <summary>
/// Universal Dependencies (UDPipe) tags constants.
/// </summary>
public static class UDTags
{
    #region Universal POS Tags
    /// <summary>
    /// Abbreviation tag for words that are abbreviations.
    /// </summary>
    public const string ABBR = "ABBR";

    /// <summary>
    /// Adjective tag for words that typically modify nouns and specify their 
    /// properties or attributes.
    /// </summary>
    public const string ADJ = "ADJ";

    /// <summary>
    /// Adposition tag for prepositions and postpositions that express spatial, 
    /// temporal, and other relations.
    /// </summary>
    public const string ADP = "ADP";

    /// <summary>
    /// Adverb tag for words that typically modify verbs, adjectives, or other
    /// adverbs.
    /// </summary>
    public const string ADV = "ADV";

    /// <summary>
    /// Auxiliary tag for function words that accompany the main verb of a
    /// clause to express tense, mood, aspect, etc.
    /// </summary>
    public const string AUX = "AUX";

    /// <summary>
    /// Coordinating conjunction tag for words that connect words, phrases,
    /// or clauses of equal status.
    /// </summary>
    public const string CCONJ = "CCONJ";

    /// <summary>
    /// Date expression tag for temporal expressions.
    /// </summary>
    public const string DATE = "DATE";

    /// <summary>
    /// Determiner tag for words that modify nouns or noun phrases and express
    /// the reference of the noun phrase.
    /// </summary>
    public const string DET = "DET";

    /// <summary>
    /// Email address tag.
    /// </summary>
    public const string EMAIL = "EMAIL";

    /// <summary>
    /// Interjection tag for words that express an emotion or sentiment.
    /// </summary>
    public const string INTJ = "INTJ";

    /// <summary>
    /// Noun tag for words denoting a person, place, thing, or idea.
    /// </summary>
    public const string NOUN = "NOUN";

    /// <summary>
    /// Numeral tag for words that express a number or a quantity.
    /// </summary>
    public const string NUM = "NUM";

    /// <summary>
    /// Particle tag.
    /// </summary>
    public const string PART = "PART";

    /// <summary>
    /// Pronoun tag for words that substitute for nouns or noun phrases.
    /// </summary>
    public const string PRON = "PRON";

    /// <summary>
    /// Proper noun tag for names of individuals, places, organizations, etc.
    /// </summary>
    public const string PROPN = "PROPN";

    /// <summary>
    /// Punctuation tag.
    /// </summary>
    public const string PUNCT = "PUNCT";

    /// <summary>
    /// Subordinating conjunction tag for words that connect clauses where one
    /// is dependent on the other.
    /// </summary>
    public const string SCONJ = "SCONJ";

    /// <summary>
    /// Symbol tag for mathematical, currency, and other symbols.
    /// </summary>
    public const string SYM = "SYM";

    /// <summary>
    /// Verb tag for words that describe actions, states, or occurrences.
    /// </summary>
    public const string VERB = "VERB";

    /// <summary>
    /// Other tag for words that do not belong to any of the defined parts
    /// of speech.
    /// </summary>
    public const string X = "X";
    #endregion

    #region Verbal Features
    /// <summary>
    /// Tense feature prefix for verbs, e.g., Tense=Past, Tense=Pres, Tense=Fut.
    /// </summary>
    public const string FEAT_TENSE = "Tense";

    /// <summary>
    /// Present tense value.
    /// </summary>
    public const string TENSE_PRESENT = "Pres";

    /// <summary>
    /// Past tense value.
    /// </summary>
    public const string TENSE_PAST = "Past";

    /// <summary>
    /// Future tense value.
    /// </summary>
    public const string TENSE_FUTURE = "Fut";

    /// <summary>
    /// Imperfect tense value, used in some languages.
    /// </summary>
    public const string TENSE_IMPERFECT = "Imp";

    /// <summary>
    /// Pluperfect tense value, used in some languages.
    /// </summary>
    public const string TENSE_PLUPERFECT = "Pqp";

    /// <summary>
    /// Mood feature prefix for verbs, e.g., Mood=Ind, Mood=Imp.
    /// </summary>
    public const string FEAT_MOOD = "Mood";

    /// <summary>
    /// Indicative mood value.
    /// </summary>
    public const string MOOD_INDICATIVE = "Ind";

    /// <summary>
    /// Imperative mood value.
    /// </summary>
    public const string MOOD_IMPERATIVE = "Imp";

    /// <summary>
    /// Subjunctive mood value.
    /// </summary>
    public const string MOOD_SUBJUNCTIVE = "Sub";

    /// <summary>
    /// Conditional mood value.
    /// </summary>
    public const string MOOD_CONDITIONAL = "Cnd";

    /// <summary>
    /// Person feature prefix, e.g., Person=1, Person=2, Person=3.
    /// </summary>
    public const string FEAT_PERSON = "Person";

    /// <summary>
    /// First person value.
    /// </summary>
    public const string PERSON_FIRST = "1";

    /// <summary>
    /// Second person value.
    /// </summary>
    public const string PERSON_SECOND = "2";

    /// <summary>
    /// Third person value.
    /// </summary>
    public const string PERSON_THIRD = "3";

    /// <summary>
    /// Aspect feature prefix for verbs, e.g., Aspect=Perf, Aspect=Imp.
    /// </summary>
    public const string FEAT_ASPECT = "Aspect";

    /// <summary>
    /// Perfective aspect value.
    /// </summary>
    public const string ASPECT_PERFECTIVE = "Perf";

    /// <summary>
    /// Imperfective aspect value.
    /// </summary>
    public const string ASPECT_IMPERFECTIVE = "Imp";

    /// <summary>
    /// Progressive aspect value.
    /// </summary>
    public const string ASPECT_PROGRESSIVE = "Prog";

    /// <summary>
    /// Verb form feature prefix, e.g., VerbForm=Fin, VerbForm=Inf.
    /// </summary>
    public const string FEAT_VERBFORM = "VerbForm";

    /// <summary>
    /// Finite verb form value.
    /// </summary>
    public const string VERBFORM_FINITE = "Fin";

    /// <summary>
    /// Infinitive verb form value.
    /// </summary>
    public const string VERBFORM_INFINITIVE = "Inf";

    /// <summary>
    /// Participle verb form value.
    /// </summary>
    public const string VERBFORM_PARTICIPLE = "Part";

    /// <summary>
    /// Gerund verb form value.
    /// </summary>
    public const string VERBFORM_GERUND = "Ger";

    /// <summary>
    /// Voice feature prefix for verbs, e.g., Voice=Act, Voice=Pass.
    /// </summary>
    public const string FEAT_VOICE = "Voice";

    /// <summary>
    /// Active voice value.
    /// </summary>
    public const string VOICE_ACTIVE = "Act";

    /// <summary>
    /// Passive voice value.
    /// </summary>
    public const string VOICE_PASSIVE = "Pass";

    /// <summary>
    /// Middle voice value.
    /// </summary>
    public const string VOICE_MIDDLE = "Mid";
    #endregion

    #region Nominal Features
    /// <summary>
    /// Number feature prefix, e.g., Number=Sing, Number=Plur.
    /// </summary>
    public const string FEAT_NUMBER = "Number";

    /// <summary>
    /// Singular number value.
    /// </summary>
    public const string NUMBER_SINGULAR = "Sing";

    /// <summary>
    /// Plural number value.
    /// </summary>
    public const string NUMBER_PLURAL = "Plur";

    /// <summary>
    /// Dual number value.
    /// </summary>
    public const string NUMBER_DUAL = "Dual";

    /// <summary>
    /// Gender feature prefix, e.g., Gender=Masc, Gender=Fem.
    /// </summary>
    public const string FEAT_GENDER = "Gender";

    /// <summary>
    /// Masculine gender value.
    /// </summary>
    public const string GENDER_MASCULINE = "Masc";

    /// <summary>
    /// Feminine gender value.
    /// </summary>
    public const string GENDER_FEMININE = "Fem";

    /// <summary>
    /// Neuter gender value.
    /// </summary>
    public const string GENDER_NEUTER = "Neut";

    /// <summary>
    /// Common gender value.
    /// </summary>
    public const string GENDER_COMMON = "Com";

    /// <summary>
    /// Case feature prefix, e.g., Case=Nom, Case=Acc.
    /// </summary>
    public const string FEAT_CASE = "Case";

    /// <summary>
    /// Nominative case value.
    /// </summary>
    public const string CASE_NOMINATIVE = "Nom";

    /// <summary>
    /// Accusative case value.
    /// </summary>
    public const string CASE_ACCUSATIVE = "Acc";

    /// <summary>
    /// Genitive case value.
    /// </summary>
    public const string CASE_GENITIVE = "Gen";

    /// <summary>
    /// Dative case value.
    /// </summary>
    public const string CASE_DATIVE = "Dat";

    /// <summary>
    /// Definite feature prefix, e.g., Definite=Def, Definite=Ind.
    /// </summary>
    public const string FEAT_DEFINITE = "Definite";

    /// <summary>
    /// Definite value.
    /// </summary>
    public const string DEFINITE_DEFINITE = "Def";

    /// <summary>
    /// Indefinite value.
    /// </summary>
    public const string DEFINITE_INDEFINITE = "Ind";
    #endregion

    #region Pronominal Features
    /// <summary>
    /// Pronoun type feature prefix, e.g., PronType=Prs, PronType=Dem.
    /// </summary>
    public const string FEAT_PRONTYPE = "PronType";

    /// <summary>
    /// Article pronoun type value (used for definite and indefinite articles).
    /// </summary>
    public const string PRONTYPE_ARTICLE = "Art";

    /// <summary>
    /// Exclamative pronoun type value (used in exclamatory expressions,
    /// e.g., "what a day!").
    /// </summary>
    public const string PRONTYPE_EXCLAMATIVE = "Exc";

    /// <summary>
    /// Emphatic pronoun type value (used for stress or emphasis, depending
    /// on the language).
    /// </summary>
    public const string PRONTYPE_EMPHATIC = "Emp";

    /// <summary>
    /// Personal pronoun type value.
    /// </summary>
    public const string PRONTYPE_PERSONAL = "Prs";

    /// <summary>
    /// Demonstrative pronoun type value.
    /// </summary>
    public const string PRONTYPE_DEMONSTRATIVE = "Dem";

    /// <summary>
    /// Indefinite pronoun type value.
    /// </summary>
    public const string PRONTYPE_INDEFINITE = "Ind";

    /// <summary>
    /// Interrogative pronoun type value.
    /// </summary>
    public const string PRONTYPE_INTERROGATIVE = "Int";

    /// <summary>
    /// Negative pronoun type value (e.g., "nobody", "nothing").
    /// </summary>
    public const string PRONTYPE_NEGATIVE = "Neg";

    /// <summary>
    /// Reciprocal pronoun type value (e.g., "each other").
    /// </summary>
    public const string PRONTYPE_RECIPROCAL = "Rcp";

    /// <summary>
    /// Relative pronoun type value.
    /// </summary>
    public const string PRONTYPE_RELATIVE = "Rel";

    /// <summary>
    /// Total pronoun type value (e.g., "everyone", "everything").
    /// </summary>
    public const string PRONTYPE_TOTAL = "Tot";
    #endregion

    #region Adjective Features
    /// <summary>
    /// The degree feature prefix for adjectives, e.g., Degree=Pos.
    /// </summary>
    public const string FEAT_DEGREE = "Degree";

    /// <summary>
    /// The positive degree.
    /// </summary>
    public const string DEGREE_POS = "Pos";

    /// <summary>
    /// The comparative degree.
    /// </summary>
    public const string DEGREE_CMP = "Cmp";

    /// <summary>
    /// The superlative degree.
    /// </summary>
    public const string DEGREE_SUP = "Sup";
    #endregion
}
