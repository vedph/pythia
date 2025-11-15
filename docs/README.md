# Pythia Documentation

This solution, Pythia, provides a concordance-based, object-oriented text search engine. This solution represents the backend of a system exposed via API and consumed by a web frontend.

## Model

The Pythia search engine focuses on objects rather than just sequences of characters. The index is a set of objects, each having an unlimited number of metadata. Additionally, objects not only correspond to words, but also to any other structure we may want to index in our documents, from words to sentences, verse, strophes, up to whole documents, or even to groups of documents.

Among these objects, some are internal to the document, like words or sentences; while others are external, like document’s metadata; that’s why internal objects among other properties also include a position in the document itself, which allows for highly granular searches and KWIC-based results.

Thus, search no longer focuses on finding a subsequence of characters in a longer one, but rather on finding objects from their properties. Whatever their nature, all objects live in the same set; and they all expose their metadata on a uniform search surface. We find objects through this surface, by matching their metadata: and such objects can be words, but also sentences, verses, documents, or even immaterial things, when for instance we look for a word class (e.g. a past intransitive verb) rather than for a specific instance of it (like it. “andò”).

So, the textual value of the searched object (when it has any) is no longer at the core: rather, it’s just any of the metadata attached to the object to find. You can specify one or more metadata to find in any logical combination. It might well happen that you do not look for a specific word at all, e.g. when searching for words in a specific language and printed in italic, or for a Latin phrase in an Italian act.

Given this model, the search engine allows for a much richer set of queries, and can deal with any objects, from words to documents and beyond, each having an unlimited set of metadata, whatever their nature, meaning, and source. This way, all our metadata, drawn from all our sources, converge into a uniformly modeled structure, providing a consistent search surface, still open to indefinite expansion.

This approach paves the way to a wide range of search options, not only by virtue of the highly different nature of objects and metadata, but also by means of combinations in search criteria. For instance, in this model we can easily find a specific word (or word class, abbreviation, forename, bold, or whatever other metadata we may want to target) at the start or end of a sentence, by just finding these two objects together. In this case, we will look for a word with all the features we specify, and for a sentence; and we will combine these objects in the search with a positional operator, allowing us to find objects appearing as left- or right-aligned in the document. In the case of a sentence-initial word, we will choose left alignment; while for a sentence-final word, we will choose right alignment.

This is why besides the usual set of logical and grouping operators (equal, not equal, including, starting with, ending with, wildcards, regular expressions, fuzzy matching, and numeric comparisons) the search engine also provides one of positional operators, designed for collocations (near, not near, before, not before, after, not after, inside, not inside, overlaps, left-aligned, right-aligned). Each in-document object, either it’s a word or something longer, just occupies a specific position or range of positions, like segments on a line; and the purpose of such operators is right to geometrically evaluate their relative alignment of two such segments.

## Indexing

The text to be indexed flows through the stages of a configurable pipeline, including all the modules you want to add logic to it. Such modules may come from stock software, or from third parties, so that the indexing process is highly customizable, and covers any aspect of processing the text, from retrieving to rendering it as in the above example.

In general, the pipeline allows for these classes of components, covering preprocessing, open indexing, and even a full environment for reading the indexed texts:

1. source collectors, used to enumerate the documents from some source (the files in a folder, as well as the documents from a cloud repository, etc.).
2. literal filters, applied to query text to ensure a uniform preprocessing corresponding to that applied to documents.
3. text filters, applied to documents as a whole for specific preprocessing and adjustments.
4. attribute parsers, used to extract metadata from documents, whatever its format (TEI, Excel, etc.).
5. document sort key builders, used to build sort keys, which represent the default sorting criterion for documents in the UI, and may be built in different ways according to their metadata.
6. date value calculators, used to calculate a computable date value used to chronologically filter or order documents.
7. tokenizers, used to split the document’s text into “words”, just like in any other search engine.
8. token filters, used not only to filter the text value of each token by removing unwanted characters (e.g. punctuation, accents, casing differences, etc.), but also to supply additional metadata to it (e.g. add syllable or characters counts, POS tags, etc.).
9. structure parsers, used to detect textual structures of any extent, like sentences, in a document. These may vary according to the document’s format, so that for instance the algorithm used for TEI documents is different from that applied to plain text.
10. structure value filters, applied to the value of any detected text structure, just like it happens for each single token.
11. text mappers, used to automatically build a navigable map of the document according to its contents.
12. text renderers, used to render the source document format into some presentational format, like HTML.

>Most of the pipeline components are described in the [components section](components.md).

## Index Schema

The Pythia default implementation relies on a RDBMS. So, querying a corpus means querying a relational database, which allows for a high level of customizations and usages by third-party systems.

The current Pythia implementation is based on PostgreSQL. Consequently, context-related functions are implemented in `PL/pgSQL`. Database schema and functions are defined in these files:

- `Pythia.Sql.PgSql`/`Assets`/`Schema.pgsql`
- `Pythia.Sql.PgSql`/`Assets`/`Functions.pgsql`

The SQL code is organized as follows:

- all the SQL components non specific to a particular SQL implementation are found in the `Pythia.Sql` project.
- PostgreSQL-specific components and overrides are found in `Pythia.Sql.PgSql`.
- the core of the SQL translation is found in `Pythia.Sql`, in `SqlPythiaListener`. A higher-level component leveraging this listener, `SqlQueryBuilder`, is used to build SQL queries from Pythia queries. Also, another query builder is `SqlWordQueryBuilder`, which is used to browse the words and lemmata index.

The Pythia index has a simple architecture, focused around a few entities. The tables are:

- `document`: documents. These are the texts being indexed.
- `document_attribute`: custom attributes of documents. Besides a set of fixed metadata (author, title, source, etc.), defined in the `document` table, documents can have any number of custom metadata in `document_attribute`. Each attribute is just a name=value pair, decorated with a type (e.g. textual, numeric, etc.).
- `corpus`: corpora, i.e. user-defined collection of documents. Corpora are just a way to group documents under a label, for whatever purpose, typically to limit searches to a specific subset of documents.
- `document_corpus`: links between document ID and corpus ID.
- `span`: this is the core of the index. Each document is analyzed into text _spans_. These are primarily tokens, but can also be any larger textual structure, like sentences, verses, paragraphs, etc. All these structures can freely overlap and can be added at will. A special field (`type`) is used to specify the span's type. Whatever the span type, its _position_ is always _token-based_, as the token here is the atomic structure in search: 1=first token in the document, 2=second, etc. Every span defines its position with two such token ordinals, named P1 and P2. So a span is just the sequence of tokens starting with the token at P1 and ending with the token at P2 (included) in a given document. Thus, when dealing with tokens P1 is always equal to P2.
- `span_attribute`: just like documents, a span has a set of fixed attributes (like position, value, or language, in the `span` table) and custom attributes (in `span_attribute`).
- `word`, `lemma`: words and lemmata: additionally, the database can include a superset of calculated data essentially related to word forms and their base form (lemma). First, spans are used as the base for building a list of _words_ (table `word`), defined as all the unique combinations of each token's language, value, part of speech, and lemma. Each word also has its pre-calculated total count of the corresponding tokens. In turn, words are the base for building a list of _lemmata_ (table `lemma`, provided that your indexer uses some kind of lemmatizer), representing all the word forms belonging to the same base form (lemma). Each lemma also has its pre-calculated total count of word forms. Both words (in `word_count`) and lemmata (in `lemma_count`) have a pre-calculated detailed distribution across documents, as grouped by each of the document's attribute's unique name=value pair.

### Word Index

Typically, word and lemmata are used to browse the index by focusing on single word forms or words.

For instance, a UI might provide a (paged) list of lemmata. The user might be able to expand any of these lemmata to see all the word forms referred to it. Alternatively, when there are no lemmata, the UI would directly provide a paged list of words.

In both cases, a user might have a quick glance at the distribution of each lemma or word with these steps:

(1) pick one or more document attributes to see the distribution of the selected lemma/word among all the values of each picked attribute. When picking numeric attributes, user can specify the desired number of "bins", so that all the numeric values are distributed in a set of contiguous ranges.

For instance, in a literary corpus one might pick attribute `genre` to see the distribution of a specific word among its values, like "epigrams", "rhetoric", "epic", etc.; or pick attribute `year` with 3 bins to see the distribution of all the works in different years. With 3 bins, the engine would find the minimum and maximum value for `year`, and then define 3 equal ranges to use as bins.

(2) for each picked attribute, display a pie chart with the frequency of the selected lemma/word for each value or (values bin) of the attribute.

In more detail, the word and lemma index is created as follows:

1. first, the index is cleared, because it needs to be globally computed on the whole dataset.

2. words are inserted grouping tokens by language, value, POS, and lemma. This means that we define as the same word form all the tokens having these properties equal. The word's count is the count of all the tokens grouped under it. Once words are inserted, their identifiers are updated in the corresponding spans.

3. lemmata are inserted grouping words by language, POS, and lemma, provided that there is one. It is assumed that the lemma has been assigned to tokens by a POS tagger. Thus, the set of words having the same language, POS and lemma belong to the same lemma. This is different from word forms (here "words"), where also value is added to the combination. So, it. "prova" is a word, and it. "prove" is a different word, even if both are inflected forms (singular and plural) of the same lemma.

For instance, consider these token-spans (spans also include linguistic structures larger than a single token, but of course we exclude them from this index):

| ID  | token value in context | lang. | POS  | lemma   |
| --- | ---------------------- | ----- | ---- | ------- |
| 1   | la _prova_ del fatto   | ita   | NOUN | prova   |
| 2   | molte _prove_          | ita   | NOUN | prova   |
| 3   | le _prove_ addotte     | ita   | NOUN | prova   |
| 4   | non _provando_         | ita   | VERB | provare |
| 5   | il fatto non _prova_   | ita   | VERB | provare |

The corresponding _words_ (combining value, language, POS, lemma) are:

| word              | lang. | POS  | lemma   |
| ----------------- | ----- | ---- | ------- |
| prova (from 1)    | ita   | NOUN | prova   |
| prove (from 2+3)  | ita   | NOUN | prova   |
| provando (from 4) | ita   | VERB | provare |
| prova (from 5)    | ita   | VERB | provare |

The corresponding _lemmata_ (combining language, POS, lemma) are:

| lemma              | lang. | POS  | lemma   |
| ------------------ | ----- | ---- | ------- |
| prova (from 1+2+3) | ita   | NOUN | prova   |
| provare (from 4+5) | ita   | VERB | provare |

⚠️ Of course, this is an approximate process because a POS tagger just provides a string for the identified lemma. So, in case of word forms having the same language, POS and lemma form, but belonging to two different lexical entries, this process would not be able to make a distinction. For instance, it. "pesca" can be:

- NOUN pesca = peach
- NOUN pesca = fishing
- VERB pesca = to fish

In this case, which admittedly is very rare, we would have these groups:

- words by combining value, language, POS, and lemma:
  - NOUN pesca, a single entry, for both peach and fishing;
  - VERB pesca.
- lemmata by combining language, POS, and lemma: as above.

Yet, this is a corner case and in this context we can tolerate the issues or fix them after the automatic process.

Finally, the lemma's count is the sum of the count of all the words belonging to it. Once lemmata are inserted, their identifiers are updated in the corresponding words.

Their **counts** index is created as follows:

1. a list of all the combinations of name=value pairs in document attributes (both privileged and non privileged) is calculated from the database. Those attributes marked as numeric are grouped into bins corresponding to the ranges calculated from their minium and maximum values, split in a preset number of classes.

2. for each word, go through all the collected pairs and calculate the count of its spans in each of the document attribute's name=value pair.

3. the lemmata counts are just the sum of the words counts for each lemma.

Once you have the index in the database, you can directly access data at will, or export them for third-party analysis. For instance, this simple query:

```sql
select w.pos, count(w.id) as c, sum(w.count) as f
from word w
group by w.pos
order by w.pos;
```

provides the distribution of words into POS categories, with their lexical frequency (`c`) and textual frequency (`f`), e.g.:

| pos   | c     | f      |
| ----- | ----- | ------ |
| ADJ   | 9596  | 114159 |
| ADP   | 175   | 157239 |
| ADV   | 1127  | 64056  |
| AUX   | 276   | 45142  |
| CCONJ | 59    | 43776  |
| DET   | 240   | 100195 |
| INTJ  | 18    | 100    |
| NOUN  | 11472 | 324014 |
| PRON  | 217   | 42082  |
| SCONJ | 82    | 20710  |
| VERB  | 12034 | 117798 |

Or, this variation:

```sql
select length(w.value), count(w.id) as c, sum(w.count) as f
from word w
group by length(w.value)
order by length(w.value);
```

provides the list of words and their frequencies correlated to their length (it's an easy prediction that the shortest words, like articles, prepositions, etc. have the highest textual frequencies); etc.

## Query

The Pythia query language is used to build SQL-based queries from much simpler expressions. Some details about this SQL translation are found at <https://vedph.github.io/pythia-doc/sql>. The ANTLR grammar for the Pythia query language is in `Pythia.Core/Query/PythiaQuery.g4`.

As explained about the Pythia model, the index is just a set of objects, which in most cases represent tokens (“words”), but can also represent sentences, verses, paragraphs, or any other text structure. Each of these objects has any number of metadata (named attributes). A search gets to these objects via their metadata.

So, a search essentially matches an attribute with a value using some comparison type. This matching is the core of any search, and represents what is called a pair, i.e. the union of an attribute’s name with a value via a comparison operator.

A query can consist of a single pair, or connect more pairs with a number of logical or positional (location) operators, optionally defining precedence with brackets.

### Pair

Each pair, whatever the entity it refers to, is wrapped in square brackets, and includes a name, an operator, and a value, or just a name when we just test for its existence (for privileged attributes only: see below). The query syntax is thus:

```txt
[NAME OPERATOR "VALUE"]
```

### Pair Name

The name is just the name of any valid attribute for the type of object we want to search.

From the point of view of the user all the attributes are equal, and can be freely queried for each item, either it is a document, a token, or a structure. Yet, internally some of these attributes are privileged, in the sense that they are considered intrinsic properties of the objects.

Privileged attributes are stored in a different way in the index, as they are directly assigned as intrinsic properties of objects; they are specific fields in the corresponding database table. All the other attributes, which are extensible, are rather linked to objects and stored separately from them, in a related table.

The names of privileged attributes are reserved; so, when defining your own attributes, avoid using these names for them. The reserved names are:

- document’s privileged attributes: id, author, title, date_value, sort_key, source, profile_id.
- span’s privileged attributes: id, p1, p2, index, length, language, pos, lemma, value, text.

Attribute names referring to _structures_ are prefixed with `$`, which distinguishes them from token attributes in the query (there is no possibility of confusing them with document attributes, as these are in a separate section).

For instance, a structure representing a single verse in a poetic text might have name `l` (=line), and would be represented as `$l` in the query language.

The pairs including non-privileged attributes may omit the operator and value when just testing for the existence of the attribute. This is only syntactic sugar: `$l` is equivalent to `$name="l"`. Instead, `$l="1"` refers to a non-privileged attribute named `l` with value equal to `1`.

### Pair Value

Attribute **values** are always included in double quotes `""`, even when they are numeric. The syntax of the value may vary according to the operator selected. For instance, if you are using a wildcard matching operator, characters `?` and `*` will represent wildcards rather than literals.

>Optionally, you can include _escapes_ inside the quotes with the form `&HHHH;` where `HHHH` is the Unicode hex (BMP) character code to be represented.

### Pair Operator

The available pair operators are 14, inspired by CSS attribute selectors:

- `=` **equals** (textual comparison, literal).
- `<>` **not equals** (textual comparison, literal).
- `*=` **contains** (uses a `LIKE` expression, literal).
- `^=` **starts with** (uses a `LIKE` expression, literal).
- `$=` **ends with** (uses a `LIKE` expression, literal).
- `?=` **wildcards** (uses a `LIKE` expression). Allowed wildcards are `?`=any single character, and `*`=any number of any characters.
- `~=` **regular expression** (with different SQL implementations, e.g. `dbo.RegexIsMatch('text', 'expr')` in SQL Server, `REGEXP` function in MySql, `~` in PostgreSQL).
- `%=` **fuzzy matching** with a treshold. The default treshold value is 0.9; you can specify a different treshold by adding it to the end of the value, prefixed by `:`. For instance, `[value%="chommoda:0.75"]`, or just `[value%="chommoda"]` to use the 0.9 treshold.
- **numeric** comparison operators: `==`, `!=`, `<`, `>`, `<=`, `>=`. These can be applied to numeric values only.

>🔧 Technically, attributes values are all modeled as strings, so that they can represent anything; but when using numeric operators, these values will be converted into (and thus treated as) numeric values. This implies that in constrast with systems like e.g. Lucene, where numeric values are handled as strings so that for instance you have to store `0910` to let it compare correctly with `1256`, this is not required for Pythia; here, you just have to use the numeric operators, which implicitly cast the string value into a number.

Thus, for instance this pair:

```txt
[value]="example"
```

just finds the word "example". Here, you can replace the `=` operator with any other one (except of course the numeric operators, as in this sample we are looking for text); so, you might type:

```txt
[value]^="exam"
```

to find all the words starting with `exam`, or:

```txt
[value]$="ple"
```

to find all the words ending with `ple`; etc.

You are not limited to a single pair. Multiple pairs can be _connected_ via logical or location operators, and precedence can be expressed by parentheses.

### Logical Operators

A different set of **logical** operators can be used according to their context (section, see [below](#sections)):

- in the _document section_: `AND`/`OR`/`AND NOT`, optionally grouped by `()`.
- in the _text section_: `OR` or location operators, optionally grouped by `()`.

>Location operators implicitly are all in an `AND` relationship with their left node. In fact, `AND` as a standalone operator is not defined, as in the context of a concordance search engine it would make little sense to find 2 words which happen to be at _any_ distance within the same document. Rather, a positional relationship is always implied by an AND to make the search meaningful (we are looking for connected words in some linguistically motivated context, rather than for documents matching several words, whatever their mutual relationships).

### Location Operators

Location operators are specializations of `AND` with added conditions based on the position of the objects in their document. All the positions are based on token positions.

Location operators have one or more **arguments**, expressed between brackets after their name. Arguments can be specified in any order; each is prefixed by its name, followed by an equals sign and its value. For instance, `BEFORE(m=0,s=l)` specifies arguments `m` with value 0, and `s` with value `l`.

For your reference, all the arguments names are listed here, but not all of the operators use all of them:

- `n`: minimum distance (0-N). Defaults to 0 if not specified.
- `m`: maximum distance (0-N). Defaults to `int`'s max value (a 32-bits signed integer) if not specified.
- `s`: structure context name. When specified, the second pair must be found inside the same structure including the first pair.
- `ns`: minimum distance (0-N) from structure start.
- `ms`: maximum distance (0-N) from structure start.
- `ne`: minimum distance (0-N) from structure end.
- `me`: maximum distance (0-N) from structure end.

👉 All the location operators can be **negated** by prefixing a `NOT` (note that in this case the `s` argument is not allowed, as it would be meaningless).

#### Near

▶️ `NEAR(n,m,s)`: filters the left expression so that it must be _near_, i.e. at the specified distance (ranging from a minimum -`n`- to a maximum -`m`-) from the second one, either before or after it. For instance, in Figure 1 A is either before or after B; the distance between the left A and B is 1, while the distance between B and the right A is 0.

![near](img/locop-near.png)

- Figure 1 - NEAR

#### Before

▶️ `BEFORE(n,m,s)`: filters the left expression so that it must be _before_ the second one, at the specified distance from it. For instance, in Figure 2 two instances of A are before B, either at distance 1 or 0.

![before](img/locop-before.png)

- Figure 2 - BEFORE

#### After

▶️ `AFTER(n,m,s)` filters the first expression so that it must be _after_ the second one, at the specified distance from it. This operator mirrors `BEFORE`. For instance, in Figure 3 two instances of A are after B, either at distance 1 or 0.

![after](img/locop-after.png)

- Figure 3 - AFTER

#### Inside

▶️ `INSIDE(ns,ms,ne,me,s)`: filters the first expression so that it must be _inside_ the span defined by the second one, optionally at the specified minimum and/or maximum distance from the container start (`ns`, `ms`) or end (`ne`, `me`). For instance, in the 4 examples of Figure 4 A is always inside B, whatever its relative position and extent.

![inside](img/locop-inside.png)

- Figure 4 - INSIDE

#### Overlaps

▶️ `OVERLAPS(n,m,s)`: filters the first expression so that its span must overlap the one defined by the second expression, optionally by the specified amount of positions. Here `n` represents the minimum required overlap, and `m` the maximum allowed overlap. For instance, in the 4 examples of Figure 5 there is always overlap (of extent 1) between A and B.

![overlaps](img/locop-overlaps.png)

- Figure 5 - OVERLAPS

#### Lalign

▶️ `LALIGN(n,m,s)`: filters the first expression so that its span must _left-align_ with the one defined by the second expression: `A` can start with or after `B`, but not before `B`. Here, `n` and `m` specify the minimum and maximum offsets from start. For instance, in Figure 6 the left A/B pair has a perfect left alignment (distance=0), while the right pair has offset=1 from the left-alignment position.

![lalign](img/locop-lalign.png)

- Figure 6 - LALIGN

#### Ralign

▶️ `RALIGN(n,m,s)`: filters the first expression so that its span must _right-align_ with the one defined by the second expression: `A` can end with or before `B`, but not after `B`. This mirrors `LALIGN`. For instance, in Figure 6 the left A/B pair has a perfect right alignment (distance=0), while the right pair has offset=1 from the right-alignment position.

![ralign](img/locop-ralign.png)

- Figure 7 - RALIGN

>⚙️ In the current implementation, each operator corresponds to a [PL/pgSQL](https://www.postgresql.org/docs/current/plpgsql.html) function, conventionally prefixed with `pyt_`. These functions receive the arguments listed above in addition to the positions being tested, which are handled by the search system.

The potential of these alignment operators may not be immediately evident, but they can provide a lot of power for contextual searches.

To start with, you can search for a word before or after or near another word, specifying the minimum and maximum distance, and also limiting results to those words included in the same larger encompassing structure (e.g. a sentence). This way, we are not limited to a mechanical numeric criterion, like a raw numeric distance, which might be useless when e.g. you are looking for pairs of words, but one of these happens to be at the end of a sentence, and the other one at the beginning of the next one.

In fact, the power of these operator shines when dealing with larger structures; for instance, you can search for a word at the beginning of a verse, i.e. a word left-aligned with a verse with maximum distance=0, or at its end, i.e. right-aligned with maximum distance=0, etc.

Remember that in Pythia everything is an object with properties (including start/end positions, where applicable), whether it's a single word or a larger linguistic structure like phrase, sentence, verse, or even non-strictly linguistic structures like typographic entities as paragraphs. Such objects all have a start and an end position, making them like segments. A token is just a segment where by definition start and end positions coincide, because positions are token-based. So, once any span of text, whatever the analysis level which defined it, has been defined in this geometrical way, you are free to look for any type of alignment between any types of segments, and additionally play with the operation arguments for minimum, maximum, and embracing structure. Once again, this is the effect of a higher abstraction level in the model, the same which "de-materialized" text from a sequence of characters into a set of objects.

### Query Sections

Text objects (tokens and structures) are not the only available objects in Pythia; there are also documents and corpora. These are used to further delimit the results to documents matching a specific query, or to predefined sets of documents (corpora).

To allow for a simpler syntax, conditions about corpora, documents, and text are specified in three different sections of a query:

1. **corpus** filters (optional). The corpus section is just a list of corpora IDs in `@@...;`. For the section to match, it is enough to match any of the listed corpora IDs.

2. **document** filters section (optional). The documents set is represented by an expression of pairs inside `@...;`, connected by `AND`/`OR`/`AND NOT`/`OR NOT`, and optionally grouped by `()`.

3. **tokens** and **structures** section (required). An expression of pairs, each inside `[...]`, connected by `OR` or a location operator (e.g. `NEAR`), and optionally grouped by `()`. Location operators would not be useful in documents and corpora sections, as documents and corpora do not refer to positions.

Thus, a query's skeleton is (whitespaces are not relevant, but I place sections in different lines just to make the query more readable):

```txt
@@...corpus...;
@...document...;
...tokens and structures...
```

where only the last section is required, while the first two refer to the search scope, as defined by documents (`@`) and their groups (corpora: `@@`).

As a sample, consider this query (whitespaces are irrelevant; see below for the details):

```txt
@@neoteroi rhetoric;
@[author="Catullus"] AND ([date_value<="0"] OR [category="poetry"]);
[value="hionios"] OR ([value="sic"] BEFORE(m=0,s=l) [value="mater"] BEFORE(m=0,s=l) [value="sic"])
```

Here we have:

- a _documents_ section, including 3 pairs for author, category, and date value, connected by logical operators and grouped with parentheses. Here we must match all the documents whose author is `Catullus`, having either their category equal to `poetry` or their date value less than `0` (which for these documents means B.C.). This limits the search only to the documents matching these criteria.

- a _text_ (=tokens/structures) section, including 4 pairs; the first pair (`hionios`) is an alternative match for the second expression, including another value (`sic`). This value is further filtered by its location with reference to the next 2 words, `mater` and `sic` again. Location operators being binary, each connects the left token (filter target) with the right token (filter condition). So, in this example `mater` adds a filter to the first `sic`; in turn, the second `sic` adds a filter to `mater`. This means that we must match a token with value `sic`, but only before to a token with value `mater` at a distance of no more than 0 token positions (`m=0` means a maximum distance of 0), and inside the same verse (`s=l` means a common ancestor structure named `l`=line); in turn, this `mater` must appear immediately before another `sic`, and inside the same verse. So, at line 5 of Catullus' poem 84, dated one century before Christ, `credo, >sic mater, sic< liber avunculus eius`, this query matches the first `sic` only as it happens to be immediately followed by `mater`, which in turn must be immediately followed by another `sic`.

### SQL

The SQL script starts with the code for each pair in the query, represented by a CTE. Then, it combines the results of these CTEs together, and joins them with additional metadata to get the result.

The SQL script skeleton is as follows:

```sql
-- CTE LIST
-- (1: pair:exit, 1st time)
WITH sN AS (SELECT...)
-- (2: pair:exit, else)
, sN AS (SELECT...)*

-- (3: query:enter)
, r AS
(
    -- (4: pair:exit)
    SELECT * FROM s1

    -- one of 5a/5b/5c:
    -- (5a: logic operator)
    INTERSECT -- (AND=INTERSECT, OR=UNION, ANDNOT=EXCEPT)

    -- (5b: locop operator)
    INNER JOIN s2 ON s1.document_id=s2.document_id AND
    ...loc-fn(args)

    -- (5c: negated locop operator)
    WHERE NOT EXISTS
    (
      SELECT 1 FROM s2
      WHERE s2.document_id=s1.document_id AND
      ...loc-fn(args)
    )
)

-- (6: query:exit)
SELECT... INNER JOIN r ON... ORDER BY... LIMIT... OFFSET...;
```

(1) the CTE list includes at least 1 CTE, `s1`. Each pair produces one.
(2) all the other CTEs follow, numbered from `s2` onwards.

>The corresponding method in `SqlPythiaListener` is `AppendPairToCteList`.

(3) the final CTE is `r`, the result, which connects all the preceding CTEs. Its bounds are emitted by `query` enter/exit.

(4) is the `SELECT` which brings into `r` data from the CTE corresponding to the left pair.

(5a) is the case of "simple" `txtExpr`; left is connected to right via a logical operator or bracket. The logical operator becomes a SQL operator, and the bracket becomes a SQL bracket. This is handled by a terminal handler for any operator which is not a `locop`.

(5b) is the case of location expression, where left is connected to right via a `locop`. We `INNER JOIN` left with right in the context of the same document, where the location function matches.

(5c) is the case of a negative location expression. This is like 5b, but we cannot just use a JOIN because this would include also spans from other documents. We rather use a subquery.

>Locop cases are handled by `EnterLocop` (which updates some state) and `ExitLocop` (which builds the SQL).

(6) finally, an end query joins `r` with additional data from other tables, orders the result, and extracts a single page.

>The corresponding method in `SqlPythiaListener` is `GetFinalSelect`.

## Solution Projects

The projects in this solutions can be grouped as follows:

- **corpus**: many projects are centered around the generic concept of a textual corpus, which can be shared by other solutions:
  - `Corpus.Core`: core models and interfaces. The `Analysis` namespace contains interfaces for text analysis, i.e. the process which builds the index from text. The `Reading` namespace contains interfaces and generic logic for the text-reading part, which provides components to build a reading environment within the search tool. Reading components are mostly used to retrieve the text, pick a specific portion of it according to a document's map dynamically built, and render the text into some output format like HTML.
  - `Corpus.Core.Plugin`: reusable components for the analysis pipeline, for file-based system and XML management.
  - `Corpus.Api.Models`: models for API controllers exposing the corpus logic.
  - `Corpus.Api.Controllers`: API controllers exposing the corpus logic.
  - `Corpus.Sql`: base SQL components for corpus tables in the database. Most of the logic is implemented here, while derived components add database-provider specific implementations. These are in `Corpus.Sql.MsSql` (for Microsoft SQL Server), `Corpus.Sql.PgSql` (for PostgreSQL, the one effectively used in the solution).
- **core**: core models and interfaces:
  - `Pythia.Core`: core models and logic. The `Analysis` namespace contains text analysis components for indexing; the `Config` namespace contains the factory class (`PythiaFactory`) used to build and configure the indexing pipeline from a JSON configuration document; the `Query` namespace essentially contains logic for the conversion from the DSL representing the Pythia query language and SQL, using ANTLR. Core grammar classes were generated with the ANTLR C# generator tool from the grammar defined in `Assets/pythia.g4`.
  - `Pythia.Core.Plugin`: reusable analysis components mostly implementing interfaces defined in the core library.
  - `Pythia.Udp.Plugin`: reusable analysis components used to [integrate UDPipe](udp.md) into the analysis pipeline.
- **CLI**: command line interface components. These are used to build the [CLI tool](cli.md) under project `pythia` which provides indexing and other management functions:
  - `Pythia.Cli.Core`: core CLI interfaces.
  - `Pythia.Cli.Plugin.Chiron`: obsolete components for integrating another project named Chiron into the analysis process. Chiron provides phonological analysis of text. A new version of Chiron has been released but these components are not yet updated.
  - `Pythia.Cli.Plugin.Standard`: default implementations of some CLI interfaces.
  - `Pythia.Cli.Plugin.Udp`: components implementing CLI interfaces to provide UDPipe functionality. UDPipe is used in Pythia for POS tagging and lemmatization.
  - `Pythia.Cli.Plugin.Xlsx`: components to integrate Excel-based components into the CLI tool.
- **database**: components for managing the RDBMS used to store the Pythia index:
  - `Pythia.Sql`: most of the logic is contained here.
  - `Pythia.Sql.PgSql`: specific implementations for PostgreSQL.
- **tagging**: code for refining POS tagging especially with reference to the words and lemmata index.
  - `Pythia.Tagger`: core components.
  - `Pythia.Tagger.Ita.Plugin`: components specialized for the Italian language.
  - `Pythia.Tagger.LiteDB`: components for dealing with a LiteDB based storage used to provide lists of inflected forms for validating the forms extracted in the index.
  - `Pythia.Tools`: utility components usd for checking the words in the index.

>`Pythia.Liz.Plugin` contains obsolete components.
