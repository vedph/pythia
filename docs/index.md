# Pythia Documentation

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

## Architecture

- [model](01-model.md)
- [query](02-query.md)
- [query samples](03-query-samples.md)
- [storage](04-storage.md)
- [SQL translation](05-sql.md)
- [word index](06-words.md)

## Analysis

- [analysis process](07-analysis.md)
- [software components](08-components.md)
- [integrating UDPipe](09-udp.md)
  - [simple example](10-example.md)
  - [simple example dump: Catullus](11-example-dump-1.md)
  - [simple example dump: Horatius](12-example-dump-1.md)
  - [real world example](13-example-ac.md)

## Tooling

- [CLI tool](14-cli.md)
- [ANTLR setup note](15-antlr.md)
