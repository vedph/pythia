# Pythia Documentation

Welcome to the Pythia documentation. As for the tool, this is work in progress. Currently it is organized in three main sections:

- **architecture** shows the conceptual model of the engine, the syntax of its query language, how queries get translated into SQL, and the underlying database schema.
- **analysis** focuses on the process of building a Pythia database, i.e. indexing a corpus for use with this engine.
- **tooling** adds some basic information about the Pythia command line interface (CLI).

## Architecture

- [model](model.md)
- [query](query.md)
- [query samples](query-samples.md)
- [SQL queries](sql.md)
  - [SQL queries without location operators](sql-ex-non-locop.md)
  - [SQL queries with location operators](sql-ex-locop.md)
- [term distributions](term-list.md)
- [storage](storage.md)

## Analysis

- [analysis process](analysis.md)
- [software components](components.md)
- [integrating UDPipe](udp.md)
  - [simple example](example.md)
  - [real world example](example-ac.md)

## Tooling

- [CLI tool](cli.md)
- [ANTLR setup note](antlr.md)
