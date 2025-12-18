# Index Check

The words and lemmata index is of course affected by errors in the POS tagging process. POS tags are assigned to token-spans during indexing, and as seen above they represent the main pillar for building word and lemmata indexes on top of them.

For instance, Italian POS taggers are easily confused by enclitics: while a word form like `facendo` is perfectly recognized as present participle of verb `fare`, syntactically built forms like `facendone`, `facendoli`, etc. are not recognized at all. This error not only leaves untagged forms, but also propagates to the word index, where such forms will not be considered at all, and consequently to the lemma index.

Ideally, one should have a better POS tagging model, or mark problematic cases in advance in source texts. Anyway, for several reasons, including practical ones, often none of these options is available. In these cases, one might at least want to try refining the indexes.

A first approach could be comparing the resulting indexes of words and lemmata with some big **list of word forms**, like [Morph-It!](https://docs.sslmit.unibo.it/doku.php?id=resources:morph-it) for Italian. That's a brute force approach, but it would at least detect errors like wrongly typed words, text markup errors, or indexing artifacts. Additionally, if the list provides more data, like lemma and part of speech, it can provide further refinements to the validation process.

So, one might start by looking up each token-span, which is the base words and lemmata are built on:

- when it is found, add a warning if the POS of the span is different from that of the word or lemma being checked.
- when it is not found, proceed with further attempts in order to provide more information to the human operator in charge of examining validation results.

Among these attempts, we can leverage a special family of Pythia components designed for the indexing process in general, known as **variant builders**. A variant builder often uses a set of rules to build zero or more hypothetical variant forms from an input form, possibly with its POS.

For instance, given an adjective like `bellissimo` (superlative of `bello`) it could attempt to generate the corresponding positive form; or, given a verb like `facendone` it could attempt to generate the corresponding form without enclitics (`facendo`); or, given a truncated form like `suor` or `pensar`, it could attempt to generate the corresponding full forms `suora` and `pensare`; or, given a form with prothesis like `istudio` it could attempt to generate the form without prothesis (`studio`); and so forth. Of course, such components are designed to fit specific corpora features, and just provide a set of attempts at detecting the real word behind an otherwise unknown form: a sort of fallback mechanism for taggers. Anyway, they can be useful to systematically provide hints for corrections, or, in ideal cases, also for automatic fixes.

So, when a form is not found in a list, we could leverage one or more variant builders to find in the same list any of its potential variants, like `facendo` from `facendone`. If this happens, we can then patch the index by supplying missing POS data, and later rebuild the word and lemmata indexes.

If also this fails, other strategies can be adopted to deal with specific errors: for instance, look for stitched words, like `facendonemenzione` from `facendone menzione` (by looking up the list); or even try a fuzzy search to find the most probable target. It all depends on the type and number of errors in the corpus, and on the level of refinement one might want to automate.

## Components

Some tool components are used to check the word forms in the index.

The check is performed using the `WordChecker` component (in the `Pythia.Tools` project), which gets a `WordToCheck` object and checks it, returning a `WordCheckResult` object. This is used by the [CLI](16-cli.md) check word index command, and uses the following data and components to check all the forms in a Pythia database:

- the lookup index received for checking. This is a LiteDB database including a list of inflected forms to be used to detect wrong spans (and consequently wrong words and lemmata) in the database.
- an instance of an `IVariantBuilder`, used to generate potential [variants](#variants) for each form which is not found in the lookup index. For instance, there might be variants for elision, archaic orthography, enclisis, etc.
- an instance of `PosTagBuilder` for building [POS tags](#tags) from their value and features.

The general logic for each word to check is:

1. find the word in the lookup index (just the word form, without filtering by POS).
2. if any word is found, ensure that at least one of them has a compatible POS, i.e. a POS with the same POS tag and any subset of features as the POS of the lookup entry found.
3. if no match was found, use the variant builder to generate potential variants of the form being searched, and search for them in the lookup index. If any is found, add a result for it with the variant which was found. So, this indicates that potentially the form, which was not found, might rather be a variant of an existing form.
4. if still no match was found, just return an error result. This may indicate that the form is wrong, or just that it is valid, but it is not present in the reference lookup index. Users will have to evaluate this.

Each result is an instance of `WordCheckResult`, which includes all the data required to evaluate the potential error and the form being checked.

The CLI tool uses an instance of `IWordReportWriter` (currently, the `CsvWordReportWriter`) to write each result to an output, which can then be revised by users who might also edit it and possibly lend it to a patching system which can automatically fix at least some of the reported issues in it. This is why the result also includes an `Action` property, which can be used later to direct the patching system together with the rest of its data.

## Tags

Projects `Pythia.Tagger.*` contain utility components for dealing with POS tags. These are mostly used to check the Pythia words and lemmata index against an external list of inflected word forms.

The `PosTag` class represents a POS tag with its POS tag and features. `PosTagBuilder` derives from it and is used to build POS tag strings from a `PosTag`.

On the UDPipe side, `UDTags` provides the most useful UDPipe tag constants for POS tags and features.

## Variants

A generic interface `IVariantBuilder` represents a component which can build variants of a given word form.

An additional string similarity scorer is provided to support fuzzy matching of additional variants.

The variant builder may use a generic lookup repository, represented by interface `ILookupIndex`. This is a container of `LookupEntry` entries. Besides a RAM-based repository, a LiteDB-based implementation of this index is provided in `Pythia.Tagger.LiteDB`.

As for Italian (in `Pythia.Tagger.Ita.Plugin`), the following implementations are provided:

- Italian variant builder (`ItalianVariantBuilder`): provides many various variants which might be potentially derived from a word according to various derivational processes (superlatives, enclitics, elisions, truncation, ancient forms, and various graphical artifacts). This can be used to catch word forms which are potentially valid even when not found in a list of inflected word forms.
- Italian POS tag builder (`ItalianPosTagBuilder`): builds Italian POS tags according to the configuration defined in `Pythia.Tagger.Ita.Plugin/Assets/Pos.csv`.
- Morph-It! list converter (`MorphitConverter`): used to convert the Morph-It! list of Italian word forms into a lookup index, transforming its tags into UDP tags.
