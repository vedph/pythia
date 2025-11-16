# POS Tagging Tools

## Tags

Projects `Pythia.Tagger.*` contain utility components for dealing with POS tags. These are mostly used to check the Pythia words and lemmata index against an external list of inflected word forms.

The `PosTag` class represents a POS tag with its POS tag and features. `PosTagBuilder` derives from it and is used to build POS tag strings from a `PosTag`.

On the UDPipe side, `UDTags` provides the most useful UDPipe tag constants for POS tags and features.

## Variants

A generic interface `IVariantBuilder` represents a component which can build variants of a given word form. Currently its implementation for Italian provides many various variants which might be potentially derived from a word according to various derivational processes (superlatives, enclitics, elisions, truncation, ancient forms, and various graphical artifacts). This can be used to catch word forms which are potentially valid even when not found in a list of inflected word forms.

An additional string similarity scorer is provided to support fuzzy matching of additional variants.

The variant builder may use a generic lookup repository, represented by interface `ILookupIndex`. This is a container of `LookupEntry` entries.
