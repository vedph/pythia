# Profile Components

- [Profile Components](#profile-components)
  - [Source Collectors](#source-collectors)
    - [File Source Collector](#file-source-collector)
  - [Literal Filters](#literal-filters)
    - [Italian Literal Filter](#italian-literal-filter)
  - [Text Filters](#text-filters)
    - [Quotation Mark Text Filter](#quotation-mark-text-filter)
    - [TEI Text Filter](#tei-text-filter)
  - [Attribute Parsers](#attribute-parsers)
    - [XML Attribute Parser](#xml-attribute-parser)
  - [Document Sort Key Builders](#document-sort-key-builders)
    - [Standard Document Sort Key Builder](#standard-document-sort-key-builder)
  - [Date Value Calculators](#date-value-calculators)
    - [Standard Date Value Calculator](#standard-date-value-calculator)
  - [Tokenizers](#tokenizers)
    - [POS Tagging XML Tokenizer](#pos-tagging-xml-tokenizer)
  - [Token Filters](#token-filters)
    - [File System Cache Supplier Token Filter](#file-system-cache-supplier-token-filter)
    - [Alnum Apos Token Filter](#alnum-apos-token-filter)
    - [Lower Alnum Apos Token Filter](#lower-alnum-apos-token-filter)
    - [Italian Token Filter](#italian-token-filter)
    - [Length Supplier Token Filter](#length-supplier-token-filter)
    - [Ancient Greek Syllable Count Supplier Token Filter](#ancient-greek-syllable-count-supplier-token-filter)
    - [Modern Greek Syllable Count Supplier Token Filter](#modern-greek-syllable-count-supplier-token-filter)
    - [Italian Syllable Count Supplier Token Filter](#italian-syllable-count-supplier-token-filter)
    - [Latin Syllable Count Supplier Token Filter](#latin-syllable-count-supplier-token-filter)
  - [Structure Parsers](#structure-parsers)
  - [Structure Value Filters](#structure-value-filters)
    - [Standard Structure Value Filter](#standard-structure-value-filter)
  - [Text Retrievers](#text-retrievers)
    - [File Text Retriever](#file-text-retriever)
    - [Azure BLOB Text Retriever](#azure-blob-text-retriever)
  - [Text Mappers](#text-mappers)
    - [XML Text Mapper](#xml-text-mapper)
  - [Text Pickers](#text-pickers)
    - [XML Text Picker](#xml-text-picker)
  - [Text Renderers](#text-renderers)

This is an overview of some stock components coming with Pythia. Everyone can add new components at will, and use them in the Pythia [profile](analysis.md).

## Source Collectors

### File Source Collector

- tag: `source-collector.file` (in `Corpus.Core.Plugin`)

File system based source collector. This collector just enumerates the files matching a specified mask in a specified directory. Its source is the directory name.

Options:

- `IsRecursive`: true to recurse the specified directory.

## Literal Filters

### Italian Literal Filter

- tag: `literal-filter.ita` (in `Pythia.Core.Plugin`)

Italian literal filter. This removes all the characters which are not letters or apostrophe, strips from them all diacritics, and lowercases all the letters.

## Text Filters

### Quotation Mark Text Filter

- tag: `text-filter.quotation-mark` (in `Corpus.Core.Plugin`)

This filter just replaces U+2019 (right single quotation mark) with an apostrophe (U+0027) when it is included between two letters.

### TEI Text Filter

- tag: `text-filter.tei` (in `Corpus.Core.Plugin`)

Filter for preprocessing TEI documents. This filter blank-fills the whole TEI header (assuming it's coded as `<teiHeader>`), and each tag in the document (unless instructed to keep the tags).

Options:

- `KeepTags`: true to keep tags in the TEI's text. The default value is false. Even when true, the TEI header is cleared anyway.

## Attribute Parsers

### XML Attribute Parser

- tag: `attribute-parser.xml` (in `Corpus.Core.Plugin`)

XML document's attributes parser. This extracts document's metadata from its XML content (e.g. a `teiHeader` in a TEI document).

Options:

- `Mappings`: an array of strings, one for each mapping. A mapping contains a name representing the metadatum key, followed by `=` plus an
XPath-like expression. This is optionally followed by a space plus either `[T]`=text or `[N]`=number, representing the metadatum value type, and eventually another space plus a regular expression which captures the value from the text of the located node. Usually, XPath expressions just contain element names (with their eventual namespace prefix, when namespaces are used) separated by slashes, and may end with a `@`-prefixed name representing an attribute, but they can be any valid XPath 1.0 expression targeting one or more nodes. For instance, the date expressed by a year value could be mapped as
`date-value=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:date/@when [N] [12]\d{3}`.
- `DefaultNsPrefix`: gets or sets the default namespace prefix. When this is set, and the document has a default empty-prefix namespace (`xmlns="URI"`), all the XPath queries get their empty-prefix names prefixed with  this prefix, which in turn is mapped to the default namespace. This is because XPath treats the empty prefix as the null namespace. In other words, only prefixes mapped to namespaces can be used in XPath queries. This means that if you want to query against a namespace in an XML document, even if it is the default namespace, you need to define a prefix for it. So, if for instance you have a TEI document with a default `xmlns="http://www.tei-c.org/ns/1.0"`, and you define mappings with XPath queries like `//body`, nothing will be found. If instead you set `DefaultNsPrefix` to `tei` and then use this prefix in the mappings, like `//tei:body`, this will find the element. See [this SO post](https://stackoverflow.com/questions/585812/using-xpath-with-default-namespace-in-c-sharp) for more.

## Document Sort Key Builders

### Standard Document Sort Key Builder

- tag: `doc-sortkey-builder.standard` (in `Corpus.Core.Plugin`)

Standard sort key builder. This builder uses author, title and year attributes to build a sort key.

## Date Value Calculators

### Standard Date Value Calculator

- `doc-datevalue-calculator.standard` (in `Corpus.Core.Plugin`)

Standard document's date value calculator, which just copies it from a specified document's attribute.

Options:

- `Attribute`: the name of the document's attribute to copy the date value from.

## Tokenizers

### POS Tagging XML Tokenizer

- tag: `tokenizer.pos-tagging` (in `Pythia.Core.Plugin`)

POS-tagging XML tokenizer, used for both real-time and deferred tokenization. This tokenizer uses an inner tokenizer for each text node in an XML document. This tokenizer accumulates tokens until a sentence end is found; then, if it has a POS tagger, it applies POS tags to all the sentence's tokens; otherwise, it adds an `s0` attribute to each sentence-end token. In any case, it then emits tokens as they are requested. This behavior is required because POS tagging requires a full sentence context.

Note that the sentence ends are detected by looking at the full original text, as looking at the single tokens, even when unfiltered, might not be enough; tokens which become empty when filtered would be skipped, and tokens and sentence-end markers might be split between two text nodes, e.g. `<hi>test</hi>.` where the stop is located in a different text node.

## Token Filters

### File System Cache Supplier Token Filter

- tag: `token-filter.cache-supplier.fs` (in `Pythia.Core.Plugin`)

Attributes supplier token filter, drawing selected attributes from the tokens stored in a file-system based cache. This filter is used in deferred POS tagging, to supply POS tags from a tokens cache, which is assumed to have been processed by a 3rd-party POS tagger. Typically, this adds a `pos` attribute to each tagged token, which is later consumed by this filter during indexing.

Options:

- `CacheDirectory`: the tokens cache directory.
- `SuppliedAttributes`: the names of the attributes to be supplied from the cached tokens. All the other attributes of cached tokens are ignored.

### Alnum Apos Token Filter

- tag: `token-filter.alnum-apos` (in `Pythia.Core.Plugin`)

A token filter which removes from the token's value any non- letter / digit / `'` character.

### Lower Alnum Apos Token Filter

- tag: `token-filter.lo-alnum-apos` (in `Pythia.Core.Plugin`)

A token filter which removes from the token's value any non-letter / digit / `'` character and lowercases the letters.

### Italian Token Filter

- tag: `token-filter.ita` (in `Pythia.Core.Plugin`)

Italian token filter. This filter removes all the characters which are not letters or apostrophe, strips from them all diacritics, and lowercases all the letters.

### Length Supplier Token Filter

- tag: `token-filter.len-supplier` (in `Pythia.Core.Plugin`)

Token value's length supplier. This filter just adds an attribute to the token, with name `len` (or the one specified in its options) and value equal to the length of the token's value, counting only its letters.

Options:

- `AttributeName`: the name of the attribute supplied by this filter. The default is `len`.
- `LetterOnly`: a value indicating whether only letters should be counted when calculating the token value's length.

### Ancient Greek Syllable Count Supplier Token Filter

- tag: `token-filter.sylc-supplier-grc` (in `Pythia.Chiron.Plugin`)

Syllables count supplier token filter for the ancient Greek language. This uses the Chiron engine to provide the count of syllables of each filtered token, in a token attribute named `sylc`.

### Modern Greek Syllable Count Supplier Token Filter

- tag: `token-filter.sylc-supplier-gre` (in `Pythia.Chiron.Plugin`)

Syllables count supplier token filter for the modern Greek language. This uses the Chiron engine to provide the count of syllables of each filtered token, in a token attribute named `sylc`.

### Italian Syllable Count Supplier Token Filter

- tag: `token-filter.sylc-supplier-ita` (in `Pythia.Chiron.Plugin`)

Syllables count supplier token filter for the Italian language. This uses the Chiron engine to provide the count of syllables of each filtered token, in a token attribute named `sylc`.

### Latin Syllable Count Supplier Token Filter

- tag: `token-filter.sylc-supplier-lat` (in `Pythia.Chiron.Plugin`)

Syllables count supplier token filter for the Latin language. This uses the Chiron engine to provide the count of syllables of each filtered token, in a token attribute named `sylc`.

## Structure Parsers

## Structure Value Filters

### Standard Structure Value Filter

- tag: `struct-filter.standard` (in `Pythia.Chiron.Plugin`)

Standard structure value filter: this removes any character different from letter or digit or apostrophe or whitespace, removes any diacritics from each letter, and lowercases each letter. Whitespaces are all normalized to a single space, and the result is trimmed.

## Text Retrievers

### File Text Retriever

- tag: `text-retriever.file` (in `Corpus.Core.Plugin`)

File-system based UTF-8 text retriever. This is the simplest text retriever, which just opens a text file from the file system and reads it. 

Options:

- `FindPattern`: an expression used to find a part of the source file path, and replace it with the value in `ReplacePattern`. This can be used to relocate source files once they have been indexed from a different directory.
- `ReplacePattern`: this replaces the expression specified by `FindPattern`. For instance, `^E:\\Temp\\Archive\\` could be a find pattern, and `D:\Jobs\Crusca\Prin2012\Archive\` a replace pattern.

### Azure BLOB Text Retriever

- tag: `text-retriever.az-blob` (in `Corpus.Core.Plugin`)

Microsoft Azure BLOB text retriever. Use this retriever to store document's texts as Azure BLOBs. The document's source property refers to the BLOB URI.

Options:

- `Connection`: the Azure connection string.
- `AccountId`: the account ID.
- `ContainerName`: the name for the Azure container.

## Text Mappers

### XML Text Mapper

- tag: `text-mapper.xml` (in `Corpus.Core.Plugin`)

A generic XML text mapper. This mapper assumes that a specified element is the root node of the map, and then walks down its tree, inserting nodes only for those elements which match any of the specified paths.

Options:

- `RootPath`: gets or sets the root path encoded as an XPath-like expression. This is the path to the element to be used as the root node in a text map. For instance, in a TEI text it might be `/tei:TEI/tei:text/tei:body` (assuming that you declared a `tei` prefix resolved to the TEI namespace URI, `http://www.tei-c.org/ns/1.0`, in `Namespaces`).
- `MappedPaths`: the mapped paths. Each path refers to an element to be mapped as a node in the text map, and usually defines also how to get its label by specifying value path(s). For instance, in a TEI text it might be `tei:body/tei:div /@type /@n$`, which means that there will be a node for each `body/div` element, whose content will be got from its attributes `type` and/or `n`.
- `Namespaces`: a set of optional key=namespace URI pairs. Each string has format `prefix=namespace`. When dealing with documents with namespaces, add all the prefixes you will use in `RootPath` or `MappedPaths` here, so that they will be expanded before processing.

## Text Pickers

### XML Text Picker

- tag: `text-picker.xml` (in `Corpus.Core.Plugin`)

XML text picker.

## Text Renderers
