# Pythia

- [Pythia](#pythia)
  - [Quick Start](#quick-start)
    - [Prerequisites](#prerequisites)
    - [Procedure](#procedure)
  - [API](#api)
  - [CLI Tool](#cli-tool)
    - [Command add-profiles](#command-add-profiles)
    - [Command build-sql](#command-build-sql)
    - [Command cache-tokens](#command-cache-tokens)
    - [Command create-db](#command-create-db)
    - [Command dump-map](#command-dump-map)
    - [Command index](#command-index)
    - [Command query](#command-query)
  - [Documentation](#documentation)

Pythia simple concordance search engine. For a general introduction see D. Fusi, _Text Searching Beyond the Text: a Case Study_, «Rationes Rerum» 15 (2020) 199-230. The implementation of the system here is more advanced, and query syntax was changed, but the approach is the same.

Please note that this system is work in progress. It is being refactored from an the older prototype, by progressively adding code and refining it.

Main features:

- concordances-based: designed from the ground up with concordances in mind: word locations here are not an afterthought or an additional payload attached to an existing location-less engine. The whole [architecture](./doc/model.md) is based on positions in documents, and these positions may also refer to other text structures besides words. In this higher abstraction level, a text is somewhat "dematerialized" into a set of positions linked to an open set of metadata. Searching for a verse or a sentence, or whatever other textual structure is thus equal to searching for a word, and we can freely mix and combine these different entity types in a query.

- minimal dependencies: simple implementation with widely used standard technologies: the engine relies on a RDBMS, and is wrapped in a REST API. The only dependency is the database service. The index is just a standard RDBMS, so that you can easily integrate it into your own project. You might even bypass the search engine, and directly query or otherwise manipulate it via SQL.

- flexible, modular and open: designed to be totally configurable via external parameters: you decide every relevant aspect of the indexing pipeline (filtering, tokenization, etc.), and can use any kind of input format (e.g. plain text, TEI, etc.) and source (e.g. file system, BLOB storage, web resources etc.).

## Quick Start

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

(the `-c`lear option ensures that you start with a blank database should the database already be present, so you can repeat this command later if you want to reset the database and start from scratch).

2. add to this database the sample profile you find in `Assets/sample.json` (in this sample, I placed a copy of it in my desktop under a folder named `pythia`):

```ps1
./pythia add-profiles c:\users\dfusi\desktop\pythia\sample.json pythia
```

You should find a profile with id `sample` in the `profile` table.

3. index the sample.xml TEI document you find in `Assets/sample.xml` (I copied it in my desktop as above):

```ps1
./pythia index sample c:\users\dfusi\desktop\pythia\sample.xml pythia -d
```

This indexes the document without saving data into the target database, just for diagnostic purposes. You can then repeat the command without `-d` to save data.

You can interactively build SQL query or run it from the CLI tool with the commands:

```ps1
./pythia build-sql

./pythia query pythia
```

In the second command, used to run queries, you must specify the database name.

## API

To run the API with the sample, 1-document database, you can generate the binary dump of its tables using my dbtool CLI app like:

```ps1
.\dbtool bulk-write pythia c:\users\dfusi\desktop\pythia-dump\ app_user,app_role,app_role_claim,app_user_role,app_user_claim,app_user_login,app_user_token,corpus,profile,document,document_attribute,document_corpus,document_structure,token,occurrence,occurrence_attribute,structure,structure_attribute -t pgsql
```

You then have to place these files under the folder specified in the API configuration variable `Data:SourceDir`.

## CLI Tool

The CLI tool is used to create and manage indexes.

### Command add-profiles

Add profile(s) from JSON files to the Pythia database with the specified name.

```ps1
./pythia add-profiles <InputFilesMask> <DbName> [-i] [-d]
```

where:

- `InputFilesMask`: the input file(s) mask for the profile files to be added.
- `DbName`: the target database name.
- `-i`: write indented JSON.
- `-d`: dry run (diagnostic run, do not write to database).

### Command build-sql

Interactively build SQL code from queries. This command has no arguments, as it starts an interactive text-based session with the user, where each typed query produces the corresponding SQL code.

```ps1
./pythia build-sql
```

### Command cache-tokens

Cache the tokens got from tokenizing the texts from the specified source.

```ps1
./pythia create-db <Source> <Output> <ProfilePath> <ProfileId> <DbName> [-t]
```

where:

- `Source`: the source.
- `Output`: the output.
- `ProfilePath`: the path to the file for the 1st tokenization profile.
- `ProfileId`: the ID of the profile to use for the 2nd tokenization. This will be set as the profile ID of the documents added to the index.
- `DbName`: the target database name.
- `-t`: the tag of the Pythia factory provider plugin to use. The default tag is `factory-provider.standard`.

### Command create-db

Create or clear the Pythia database with the specified name.

```ps1
./pythia create-db <DbName> [-c]
```

where:

- `DbName`: the target database name.
- `-c`: clear the database if exists.

### Command dump-map

Generate and dump the document's text map for the specified document.

```ps1
./pythia dump-map <Source> <DbName> <ProfileId> <OutputPath> [-t]
```

where:

- `Source`: the source document.
- `DbName`: the target database name.
- `ProfileId`: the ID of the profile to use for the source documents.
- `OutputPath`: the output path for the dump.
- `-t`: the tag of the Pythia factory provider plugin to use. The default tag is `factory-provider.standard`.

Sample:

```ps1
./pythia dump-map c:\users\dfusi\desktop\pythia\sample.xml pythia sample c:\users\dfusi\desktop\dump.txt
```

The generated dump is a plain text file like this:

```txt
#Tree
Length (chars): 1558
- [324-1539] /TEI[1]/text[1]/body[1]
.poem - 84 - ad Arrium [332-1530] /TEI[1]/text[1]/body[1]/div[1]

#-: /TEI[1]/text[1]/body[1]
324-1539
From: <body>\r\n<div type="poem" n="84">\r\n<head>ad Arrium</head>\r\n<lg type="eleg" n="1">\r\n<l n="1" type="h"> ...
To: ... Ionios</geogName> esse\r\nsed <quote><geogName>Hionios</geogName></quote>.</l>\r\n</lg>\r\n</div>\r\n</body>

#poem - 84 - ad Arrium: /TEI[1]/text[1]/body[1]/div[1]
332-1530
From: <div type="poem" n="84">\r\n<head>ad Arrium</head>\r\n<lg type="eleg" n="1">\r\n<l n="1" type="h"><quote>c ...
To: ... geogName>Ionios</geogName> esse\r\nsed <quote><geogName>Hionios</geogName></quote>.</l>\r\n</lg>\r\n</div>
```

### Command index

Index the specified source into the Pythia database with the specified name.

```ps1
./pythia index <ProfileId> <Source> <DbName> [-c] [-o] [-d] [-t]
```

where:

- `ProfileId`: the ID of the profile to use for the source documents.
- `Source`: the source.
- `DbName`: the target database name.
- `-c`: content to index: freely combine `T`=token, `S`=structure.
- `-o`: true to store the document's content in the index.
- `-d`: dry run (diagnostic run, do not write to database).
- `-t`: the tag of the Pythia factory provider plugin to use. The default tag is `factory-provider.standard`.

### Command query

Interactively execute queries. This command has no arguments, as it starts an interactive text-based session with the user, where each typed query produces the corresponding SQL code which is then executed against the specified database.

```ps1
./pythia query <DbName>
```

where:

- `DbName`: the index database name.

## Documentation

- [model](./doc/model.md)
- [storage](./doc/storage.md)
- [analysis](./doc/analysis.md)
- [SQL](./doc/sql.md)
- [query samples](./doc/query-samples.md)
