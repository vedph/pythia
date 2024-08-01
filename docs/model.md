# Model

- [Model](#model)
  - [De-materializing Text](#de-materializing-text)
  - [Objects and Attributes](#objects-and-attributes)
  - [The Way to Objects via Metadata](#the-way-to-objects-via-metadata)

## De-materializing Text

In more traditional approaches, a text from the standpoint of a simple full text search engine is just a _sequence of characters_; from it, sequences corresponding to "words" (tokens) are extracted, and usually filtered in various ways (e.g. by removing case differences, accents, etc.).

In this context, finding words in the index essentially means _matching the sequence_ of characters you type with any of the indexed sequences. In this model, all what defines a "word" is a sequence of characters.

Yet, in some scenarios you might have much much _more data_ about a word, like e.g. details about its morphology (from a POS tagger), phonology (e.g. syllables or accents), semantics, or even more exotic features like typographic styles; and you would want to search for _any_ of these features, _combining_ them in different ways.

Also, you might want to deal with word's _context_, like finding a word before or after another; or two words in the same context, where this context can be any text structure (a sentence, a verse, a paragraph...); or a word in a specific position within its context (e.g. at verse start, at sentence end, or even at a specified minimum and/or maximum span from their start or end).

This implies that we also need a way of dealing with _textual structures_ which, while being based on tokens, are larger than them, like sentences (as defined by syntax), verse (as defined by metrics), paragraphs (as defined by graphical layout), etc. To deal with both tokens and these larger structures, and still provide an open set of metadata for any of them, we need to raise the abstraction level.

## Objects and Attributes

In the Pythia model, text is not primarily viewed as a _sequence of characters_, but rather as a set of **objects**, each with any number and type of metadata (named **attributes**).

These objects have different scale: the minimal one is the "word" (the token); but any wider text structure can be an object: a sentence, a verse, a strophe, a paragraph, etc. There is no limit to objects detection, nor constraints; unlike in many markup languages like XML, these wider structures can freely overlap or nest.

So, in a sense the text gets _de-materialized_ into a set of objects with metadata; and metadata are the paths leading to objects. In the Pythia model, searching ultimately means finding all the positioned objects whose _metadata_ match the specified criteria, in the scope of the specified subset of the index.

While most of these objects are tokens, they can also be larger spans of text, like sentences or verses. A **text span** (or simply "span") is a generic model representing any span of text from a specific document, whether it corresponds to a single token, or to any larger textual structure.

A span, whatever its type, always has two token-based positions (named P1 and P2) which define its extent. Such position is simply the ordinal number of the token in its document, as for many other search engines. In the case of tokens, by definition P1 is always equal to P2; while in the case of larger structures, in most cases P2 is greater than P1. This allows dealing with tokens or any larger text structures using the same model, and thus the same search logic; and freely combine them at will, as their only difference is in their type (token, sentence, verse, etc.) and extent (as defined by P1 and P2). Also, given that both tokens and larger structures are spans, they also share the same open model for metadata: like any object, spans have attributes.

So we have objects, and their attributes. Even the sequence of characters of a word object is simply one of its possible attributes (metadata), together with many others, like document position, letters count, syllables count, accentuation pattern, part of speech, morphological tags, semantic tags, etc.

These attributes are an **open set**: there is no limit to the metadata you might want to add, and the indexing system is modular to allow you assign any kind of attributes to any object. In the end, the document themselves are objects with an open set of attributes. An attribute essentially is a name=value pair; so, this defines an open model, where you are free to add as many attributes as you want.

For practical purposes anyway, some of the attributes are treated as _intrinsic_ to their specific object, so that they are always present (even if they still can be empty), and are named **privileged attributes**. For instance, documents have privileged attributes like author, title, or source; and spans of text have privileged attributes like type (e.g. token or sentence), textual value (e.g. the sequence of characters corresponding to it), or position in the document.

At a higher level of abstraction, while objects are all created equal, textual objects like words or sentences differ from objects like documents just because they always have an attribute representing their **position** (relative to the count of tokens in a document).

## The Way to Objects via Metadata

While metadata and attributes here are mostly used as synonyms, the convention adopted here is that _metadata are modeled as attributes_, i.e. essentially _name=value_ pairs. Matching metadata means matching these attributes, i.e. finding those attributes with a specific name and/or value.

For instance, searching for the word `sic` means matching all the occurrences of tokens whose attribute `value` (the attribute representing a token's text) is equal to `sic`; but you might also want to find all the adverbs starting with `s`, counting no more of 5 letters or 1 syllable, at the beginning of the sentence, in documents in a specific chronological interval, etc.

So, in this model you can imagine a set of objects as a circle including any number and type of shapes; each of these shapes projects to the world outside of that circle any number of connection points, which provide a uniform search surface. The search engine just deals with these points, without caring whether they are linked to a word, a sentence, a document, etc.

More concretely, in a Pythia query matching attributes is done via **pairs**, each with a 3 members form:

1. attribute name;
2. operator;
3. attribute value.

For instance, when you look for the Latin word `commoda`, you are really looking for a pair with these properties:

- attribute **name** = `value` (the word's text value is represented by an attribute with this name).
- **operator** = _equals_.
- attribute **value** = `"commoda"`.

---

⏭️ [query](query.md)
