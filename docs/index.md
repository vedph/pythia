# Pythia Documentation

Welcome to the Pythia documentation. As for the tool, this is work in progress. Currently it is organized in three main sections:

- **architecture** shows the conceptual model of the engine, the syntax of its query language, how queries get translated into SQL, and the underlying database schema.
- **analysis** focuses on the process of building a Pythia database, i.e. indexing a corpus for use with this engine.
- **tooling** adds some basic information about the Pythia command line interface (CLI).

## Architecture

- [model](model.md)
- [query](query.md)
- [query samples](query-samples.md)
- [storage](storage.md)
- [SQL translation](sql.md)
- [word index](words.md)

## Analysis

- [analysis process](analysis.md)
- [software components](components.md)
- [integrating UDPipe](udp.md)
  - [simple example](example.md)
  - [simple example dump: Catullus](example-dump-1.md)
  - [simple example dump: Horatius](example-dump-1.md)
  - [real world example](example-ac.md)

## Tooling

- [CLI tool](cli.md)
- [ANTLR setup note](antlr.md)
