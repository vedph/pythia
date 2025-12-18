# Index Check

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
