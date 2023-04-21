# Model

- [Model](#model)
  - [De-materializing Text](#de-materializing-text)
  - [Objects and Attributes](#objects-and-attributes)
  - [The Way to Objects via Metadata](#the-way-to-objects-via-metadata)

## De-materializing Text

In more traditional approaches, a text from the standpoint of a simple full text search engine is just a sequence of characters; from it, sequences corresponding to "words" (tokens) are extracted, and usually filtered in various ways (e.g. by removing case differences, accents, etc.). Then, finding words in the index essentially means matching the sequence of characters you type with any of the indexed sequences. In this model, all what defines a "word" is a sequence of characters.

Yet, in some scenarios you might have much more data about a word, like e.g. details about its morphology (from a POS tagger), phonology (e.g. syllables or accents), semantics, or even more exotic features like typographic styles; and you would want to search for any of these features, variously combining them.

Also, you might want to deal with word's context, like finding a word before or after another; or two words in the same context, where this context can be any text structure (a sentence, a verse, a paragraph...); or a word in a specific position within its context (e.g. at verse start, at sentence end, or even at a specified minimum and/or maximum span from their start or end).

## Objects and Attributes

Pythia was born right for these purposes, and with concordances as its first output. In it, the text is not primarily viewed as a sequence of characters, but rather as a set of **objects**, each with any number and type of metadata (**attributes**). These objects have different scale: the minimal one is the "word" (the token); but any wider text structure can be an object: a sentence, a verse, a strophe, a paragraph, etc. There is no limit to objects detection, nor constraints; unlike in many markup languages like XML, these wider structures can freely overlap and nest.

In this model, the sequence of characters of a word object will just be any of its possible attributes (metadata), together with things like document position, letters count, syllables count, accentuation pattern, part of speech, morphological tags, semantic tags, etc. There is no limit to the metadata you might want to add, and the indexing system is modular to allow you assign any kind of attributes to any object. In the end, the document themselves are object, with their attributes like author, title, date, genre, etc.

At a higher level of abstraction, while objects are all created equal, textual objects like words or sentences differ from objects like documents just because they always have a metadatum representing their **position** (relative to the count of tokens in a document).

So, in a sense the text gets de-materialized into a set of objects with metadata; and metadata are the paths leading to objects. In the Pythia model, searching ultimately means finding all the _document's positions_ whose _metadata_ match the specified criteria, in the scope of the specified subset of the index.

## The Way to Objects via Metadata

While metadata and attributes here are mostly used as synonyms, the convention adopted here is that _metadata are modeled as attributes_, i.e. essentially _name=value_ pairs.

Matching metadata means matching these attributes, i.e. finding those attributes with a specific name and/or value. For instance, searching for the word `sic` means matching all the occurrences of tokens whose attribute `value` (the attribute representing a token's text) is equal to `sic`; but you might also want to find all the adverbs starting with `s`, counting no more of 5 letters or 1 syllable, at the beginning of the sentence, in documents in a specific chronological interval, etc.

Thus, in this model you can imagine a set of objects as a circle including any number and type of shapes; each of these shapes projects to the world outside of that circle any number of connection points, which provide a uniform search surface. The search engine just deals with these points, without caring whether they are linked to a word, a sentence, a document, etc.

More concretely, in a Pythia query matching attributes is done via **pairs**, each with the form _attribute name + operator + "attribute value"_. For instance, when you look for the word `chommoda`, you are really looking for a pair with these properties:

- attribute **name** = `value` (the word's text value is represented by an attribute with this name).
- **operator** = _equals_.
- attribute **value** = `"chommoda"`.

---

⏭️ [query](query.md)
