# POS Tagging Tools

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
