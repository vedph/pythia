# Pythia

- [Pythia](#pythia)
  - [Quick Start](#quick-start)
    - [Prerequisites](#prerequisites)
    - [Procedure](#procedure)
    - [API](#api)
  - [Documentation](#documentation)

Pythia simple concordance search engine. For a general introduction see D. Fusi, _Text Searching Beyond the Text: a Case Study_, «Rationes Rerum» 15 (2020) 199-230. The implementation of the system here is more advanced, and query syntax was changed, but the approach is the same.

Please note that this system is work in progress. It is being refactored from an the older prototype, by progressively adding code and refining it.

Main features:

- concordances-based: designed from the ground up with concordances in mind: word locations here are not an afterthought or an additional payload attached to an existing location-less engine. The whole [architecture](./doc/model.md) is based on positions in documents, and these positions may also refer to other text structures besides words. In this higher abstraction level, a text is somewhat "dematerialized" into a set of token-based _positions_ linked to an open set of _metadata_. Rather than a long sequence of characters, a text is viewed as an abstract set of entities, each having any metadata, in most cases also including their position in the original text. These entities may represent documents, groups of documents (corpora), and words and any other textual structure (e.g. a verse, a strophe, a sentence, a phrase, etc.), with no limits, even when multiple structures overlap. Searching for a verse or a sentence, or whatever other textual structure is equal to searching for a word; and we can freely mix and combine these different entity types in a query.

- minimal dependencies: simple implementation with widely used standard technologies: the engine relies on a RDBMS, and is wrapped in a REST API. The only dependency is the database service. The index is just a standard RDBMS, so that you can easily integrate it into your own project. You might even bypass the search engine, and directly query or otherwise manipulate it via SQL.

- flexible, modular and open: designed to be totally configurable via external parameters: you decide every relevant aspect of the indexing pipeline (filtering, tokenization, etc.), and can use any kind of input format (e.g. plain text, TEI, etc.) and source (e.g. file system, BLOB storage, web resources etc.).

## Quick Start

For a more realistic example you can see [this page](./doc/example.md).

### Prerequisites

The only prerequisite is having a PostgreSQL service.

To launch a PostgreSQL service without installing it, I prefer to use a ready-made Docker also including [PostGIS](https://postgis.net/install/), but any up-to-date PostgreSQL image is fine. You can easily run a container like this (in this sample, I created a folder in my drive at `c:\data\pgsql` to host data outside the container):

```ps1
docker run --volume postgresData://c/data/pgsql -p 5432:5432 --name postgres -e POSTGRES_PASSWORD=postgres -d postgis/postgis:13-master
```

### Procedure

1. use the pythia CLI to create a Pythia database, named `pythia` (or whatever name you prefer):

```ps1
./pythia create-db pythia -c
```

(the `-c`lear option ensures that you start with a blank database, should the database already be present; so you can repeat this command later if you want to reset the database and start from scratch).

2. add to this database the sample profile you find in `Assets/sample.json` (in this sample, I placed a copy of it in my desktop under a folder named `pythia`):

```ps1
./pythia add-profiles c:\users\dfusi\desktop\pythia\sample.json pythia
```

You should find a profile with id `sample` in the `profile` table.

3. index the sample.xml TEI document you find in `Assets/sample.xml` (I copied it in my desktop as above):

```ps1
./pythia index sample c:\users\dfusi\desktop\pythia\sample.xml pythia
```

To index the document without saving data into the target database, just for diagnostic purposes, you can add the `-d` (=dry run) option.

You can interactively build SQL query or run it from the CLI tool with the commands:

```ps1
./pythia build-sql

./pythia query pythia
```

In the second command, used to run queries, you must specify the database name.

### API

To run the API with the sample, 1-document database, you can generate the binary dump of its tables using my dbtool CLI app like:

```ps1
.\dbtool bulk-write pythia c:\users\dfusi\desktop\pythia-dump\ app_user,app_role,app_role_claim,app_user_role,app_user_claim,app_user_login,app_user_token,corpus,profile,document,document_attribute,document_corpus,document_structure,token,occurrence,occurrence_attribute,structure,structure_attribute -t pgsql
```

You can find a ZIP with these files in this solution (`pythia-dump.zip`).

You then have to place these files under the folder specified in the API configuration variable `Data:SourceDir`.

## Documentation

- [model](./doc/model.md)
- [storage](./doc/storage.md)
- [analysis](./doc/analysis.md)
- [components](./doc/components.md)
- [SQL](./doc/sql.md)
- [query samples](./doc/query-samples.md)
- [CLI tool](./doc/cli.md)
- [example](./doc/example.md)
- [real-world example](./doc/example-ac.md)
