# CLAUDE.md

## Purpose of the Solution

The Pythia solution provides object-based full-text indexing into a PostgreSQL database.

The index core is based on text **spans**, which are objects representing a range of characters in a document. Each span is associated with a set of metadata, which can be anything from the text value to its position in the document, part of speech, or any other property. Spans are not limited to words; they cover any other structure in texts, like phrases, sentences, verses, paragraphs, etc.

Consequently, **search** focuses on finding objects from their metadata using a rich set of operators, including *positional operators*, which focus on spans alignment (near, not near, before, not before, after, not after, inside, not inside, overlaps, left-aligned, right-aligned): for instance we find a sentence-initial word by finding it aligned with the start of a sentence.

The text to be indexed flows through a configurable **pipeline**, including:

1. **source collectors**, to enumerate the documents from some source.
2. **text filters**, applied to the whole document's text.
3. **attribute parsers**, to extract metadata from documents, whatever its format.
4. **document sort key builders**, providing strings representing the default sorting criterion for documents.
5. **date value calculators**, to calculate date values for documents.
6. **tokenizers**, to split the document's text into tokens.
7. **token filters**, not only to filter the text value of each token by removing unwanted characters but also to supply additional metadata to it (e.g. syllable or character counts, POS tags, etc.).
8. **structure parsers**, to detect textual structures of any extent, like sentences, built of tokens.
9. **structure value filters**, applied to the value of any detected text structure, like for each token.
10. **text mappers**, to build a "map" of the document.
11. **text renderers**, to render the source document format into a presentational format, like HTML.

## Projects List

XUnit test projects are named after the project they test, with a `.Test` suffix.

- `Corpus.Api.Controllers`: API controllers for Corpus API.
- `Corpus.Api.Models`: API models for Corpus API.
- `Corpus.Core`: core models and services for documents.
- `Corpus.Core.Plugin`: plugins for Corpus core functionality (retrieve and display text).
- `Corpus.Sql`: SQL-related functionality for Corpus.
- `Corpus.Sql.PgSql`: PostgreSQL-specific functionality for Corpus.
- `pythia`: Pythia CLI tool.
- `Pythia.Api`: Pythia API.
- `Pythia.Api.Controllers`: API controllers.
- `Pythia.Api.Models`: API models.
- `Pythia.Api.Services`: API services.
- `Pythia.Cli.Core`: core CLI functionality.
- `Pythia.Cli.Plugin.Standard`: standard CLI plugins.
- `Pythia.Cli.Plugin.Udp`: UDPipe CLI plugins.
- `Pythia.Cli.Plugin.Xlsx`: Excel CLI plugins.
- `Pythia.Core`: core functionality.
- `Pythia.Core.Plugin`: plugins for Pythia core functionality.
- `Pythia.Sql`: SQL-related functionality for index.
- `Pythia.Sql.PgSql`: PostgreSQL-specific functionality for index.
- `Pythia.Tagger`: POS tagging utilities.
- `Pythia.Tagger.Ita.Plugin`: Italian-specific POS tagging utilities.
- `Pythia.Tagger.LiteDB`: LiteDB-based lookup index (used to check index against lists of words).
- `Pythia.Tools`: tools (mostly for word index checking).
- `Pythia.Udp.Plugin`: UDPipe plugins.
- `Pythia.Xlsx.Plugin`: Excel plugins.
