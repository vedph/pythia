# CLI Tool

- [CLI Tool](#cli-tool)
  - [Overview](#overview)
  - [Pythia Factory Provider](#pythia-factory-provider)
  - [Add Profiles Command](#add-profiles-command)
  - [Build SQL Command](#build-sql-command)
  - [Build Word Index Command](#build-word-index-command)
  - [Bulk Read Command](#bulk-read-command)
  - [Bulk Write Command](#bulk-write-command)
  - [Cache Tokens Command](#cache-tokens-command)
  - [Check Metadata Files Command](#check-metadata-files-command)
  - [Create Database Command](#create-database-command)
  - [Dump Document Pairs Command](#dump-document-pairs-command)
  - [Dump Map Command](#dump-map-command)
  - [Export Search Command](#export-search-command)
  - [Index Command](#index-command)
  - [Query Command](#query-command)

## Overview

The CLI tool is used to create and manage indexes. This is a multi-platform client, and you can start it by just typing `./pythia` in its folder. This will show you a list of commands. You can type `./pythia` followed by any of the commands plus `-h` to get more help about each specific command.

The only (all optional) customizations required by the tool are:

- the _connection string_ to your DB service. This is found in `appsettings.json`. You can edit this file, or override it using an environment variable in your host machine.

- the _Pythia components factory provider_. The [analysis process](analysis.md) is based on a number of [pluggable components](components.md) selected by their unique tag ID, and variously configured with their options. All these parameters are found in an external profile ID file (a JSON file). To instantiate these components, Pythia uses a factory, which internally has access to all its dependencies and their tag ID mappings. Thus, when you are going to add your own components, you should also change the factory accordingly, creating a new Pythia factory provider.

## Pythia Factory Provider

A Pythia factory provider is a class implementing interface `IPythiaFactoryProvider`, which gets a profile and a connection string, and returns a Pythia factory with its dependency injection properly configured for the set of components you will need.

To avoid rebuilding the CLI tool whenever you want to use a new provider, the tool instantiates its provider as a plugin. All the providers are stored in the tool's `plugins` folder, each under its own subdirectory. Each of these subdirectories is named after the plugin's DLL file name.

For instance, the plugin(s) library `Pythia.Cli.Plugin.X.dll` should be placed in a subfolder of this folder named `Pythia.Cli.Plugin.X`, together with all its required files. Inside this assembly, there will be a single plugin (=provider implementation), tagged (with `TagAttribute`) as `factory-provider.X`.

If you don't specify any options, the tool will just use the default Pythia factory provider, which uses all the stock plugins.

If you want to use a different provider, just build your own library, place its binaries under the proper subfolder in the `plugins` directory, and add the `-t` (tag) parameter to the commands requiring it to tell the CLI to use the plugin with that tag.

This allows reusing a unique code base (and thus its already compiled binaries) even when the indexing components are external to the CLI tool. The same instead does not happen for the API, because these are typically built to create a specific Docker image with all its dependencies packed inside. In this case, you just inject the required factory, and build the customized API. This is why the API project is essentially a thin skeleton with very few code; all its relevant components are found in libraries, which get imported into several API customizations.

## Add Profiles Command

🎯 Add profile(s) from JSON files to the Pythia database with the specified name.

```ps1
./pythia add-profiles <INPUT_FILES_MASK> [-d <DB_NAME>] [-i <CSV_IDS>] [-p]
```

- `INPUT_FILES_MASK`: the input file(s) mask for the profile files to be added.
- `-d DB_NAME`: the database name (default=`pythia`).
- `-i CSV_IDS`: the optional IDs to assign to the profiles added. If not specified, each profile will get an ID equal to its source file name (without its extension and directory name). You can override this automatic ID assignment by specifying 1 or more IDs to replace the file-name derived IDs, in the same order in which files will be processed (the command will process files in alphabetical order). If you want to apply the default ID, just leave the ID blank, e.g. `alpha,,gamma` means that the first profile will get ID `alpha`; the second profile will get the automatic ID from its file name; and the third profile will get ID `gamma`.
- `-p`: preflight run (diagnostic run, do not write to database).

## Build SQL Command

🎯 Interactively build SQL code from queries. This command has no arguments, as it starts an interactive text-based session with the user, where each typed query produces the corresponding SQL code.

```ps1
./pythia build-sql
```

## Build Word Index Command

🎯 Build words index from tokens.

```ps1
./pythia index-w [-d <DB_NAME>] [-c <COUNTS>] [-x <ATTR>] 
```

- `-c COUNTS`: the class counts for document attribute bins (name=N, multiple).
- `-d DB_NAME`: the database name (default=`pythia`).
- `-x ATTR`: the document attributes to exclude from word index (multiple).

Example:

```ps1
./pythia index-w -d pythia-demo -c date-value=3 -c date_value=3 -x date
```

## Bulk Read Command

🎯 Import bulk tables data from the database as exported with the [bulk write command](#bulk-write-command).

```ps1
./pythia bulk-read <INPUT_DIR>
```

Example:

```ps1
./pythia bulk-read c:/users/dfusi/desktop/dump
```

## Bulk Write Command

🎯 Export bulk tables data from the database, to be later used when restoring it via the API startup services or the [bulk read command](#bulk-read-command).

```ps1
./pythia bulk-write <OUTPUT_DIR> [-d <DB_NAME>]
```

- `OUTPUT_DIR` is the target directory.
- `DB_NAME` is the source database name. Default=`pythia`.

Example:

```ps1
./pythia bulk-write c:/users/dfusi/desktop/dump
```

💡 This function is used to allow the Pythia API restore a database from a set of PostgreSQL binary files generated via bulk table copy (e.g. `COPY table TO STDOUT (FORMAT BINARY);`). You must have your dump files (one for each table in the database) in some folder in your host machine; connect this folder to the container API via a volume; and set the corresponding environment variable (`DATA_SOURCEDIR`) to that volume. If this is true, the API will seed data from the dump files on startup when creating the database. Example:

```yml
pythia-api:
  environment:
      - DATA__SOURCEDIR=/opt/dump/
  volumes:
      - /opt/dump:/opt/dump
```

Note that in Windows hosts you would need to quote a path including colons (e.g. `c:/data:/opt/dump`), which causes syntactic issues. You can use this [alternative syntax](https://www.reddit.com/r/docker/comments/hkx3s0/volume_mount_with_a_colon_in_the_path_with/):

```yml
    volumes:
      - type: bind
        source: 'c:/data'
        target: '/opt/dump'
```

>See also [this SO post](https://stackoverflow.com/questions/46166304/docker-compose-volumes-without-colon).

## Cache Tokens Command

🎯 Cache the tokens got from tokenizing the texts from the specified source. This is a legacy command used to apply processing like POS tagging outside the Pythia environment.

```ps1
./pythia cache-tokens <SOURCE> <OUTPUT_DIR> <PROFILE_PATH> <PROFILE_ID> [-d <DB_NAME>] [-t <PLUGIN_TAG>]
```

- `SOURCE`: the documents source.
- `OUTPUT_DIR`: the output.
- `PROFILE_PATH`: the path to the file for the 1st tokenization profile.
- `PROFILE_ID`: the ID of the profile to use for the 2nd tokenization. This will be set as the profile ID of the documents added to the index.
- `-d DB_NAME`: the database name (default=`pythia`).
- `-t PLUGIN_TAG`: the tag of the Pythia factory provider plugin to use.

## Check Metadata Files Command

🎯 Check that each source file for indexing has its corresponding metadata file. For each file in the input mask, it builds the corresponding companion metadata file name by replacing a pattern with a text, and checks whether the resulting path corresponds to an existing file. It is recommended to perform this check before indexing when you are using metadata attribute parsers which rely on companion files, so to ensure your indexing process won't stop when a metadata file is missing.

```ps1
./pythia check-meta <INPUT_FILE_MASK> [-f REGEX_TO_FIND] [-r TEXT_TO_REPLACE]
```

- `INPUT_FILE_MASK`: the input file(s) mask.
- `-f`: the regular expression pattern to find in each input file path name.
- `-r`: the text to replace for each match found.

## Create Database Command

🎯 Create or clear a Pythia database.

```ps1
./pythia create-db [-d <DB_NAME>] [-c]
```

- `-d DB_NAME`: the database name (default=`pythia`).
- `-c`: clear the database if exists.

## Dump Document Pairs Command

🎯 Dump into a CSV file document name=value pairs for words and lemmata counts. This can be useful when you need to inspect them before creating a word index.

```ps1
./pythia dump-pairs [-d <DB_NAME>] [-o <OUTPUT_PATH>]
```

- `-c COUNTS`: the class counts for document attribute bins (name=N, multiple).
- `-d DB_NAME`: the database name (default=`pythia`).
- `-o OUTPUT_PATH`: the output path for the dump (default is `doc-pairs.csv` in desktop folder).
- `-x ATTR`: the document attributes to exclude from word index (multiple).

## Dump Map Command

🎯 Generate and dump the document's text map for the specified document.

```ps1
./pythia dump-map <SOURCE> <PROFILE_ID> <OUTPUT_PATH> [-d <DB_NAME>] [-t <PLUGIN_TAG>]
```

- `SOURCE`: the documents source.
- `PROFILE_ID`: the ID of the profile to use for the source documents.
- `OUTPUT_PATH`: the output path for the dump.
- `-d DB_NAME`: the database name (default=`pythia`).
- `-t PLUGIN_TAG`: the tag of the Pythia factory provider plugin to use.

Example:

```ps1
./pythia dump-map c:/users/dfusi/desktop/pythia/sample.xml sample c:/users/dfusi/desktop/dump.txt
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

## Export Search Command

🎯 Export the results of a search into CSV.

```ps1
./pythia export-search [-d <DB_NAME>] [-q <QUERY>] [-o <OUTPUT_DIR>] [-p <PAGE_SIZE>] [-f <FIRST_PAGE>] [-l <LAST_PAGE>] [-m <MAX_ROWS>] [-c <CONTEXT_SIZE>]
```

- `-d DB_NAME`: the database name (default=`pythia`).
- `-q QUERY`: the search query. If not specified it will be prompted. If you just type a word, the query syntax will be completed like `[value="word"]`.
- `-o OUTPUT_DIR`: the output directory (default=desktop).
- `-p PAGE_SIZE`: the default virtual page size (default=100).
- `-f FIRST_PAGE`: the number of the first page to export (default=1).
- `-l LAST_PAGE`: the number of the last page to export (default=0 meaning the last page found).
- `-m MAX_ROWS`: the maximum number of rows per output file (default=0 for unlimited). If greater than 0, a new file will be created whenever the rows limit is reached.
- `-c CONTEXT_SIZE`: the size of the KWIC context (default=5).

## Index Command

🎯 Index the specified source into the Pythia database.

When dump mode is enabled, the filtered text is dumped to the specified directory for each document indexed. This can be useful for diagnostic purposes, so that you can inspect the text being input to the indexing process proper.

```ps1
./pythia index <PROFILE_ID> <SOURCE> [-d <DB_NAME>] [-c <TS>] [-o] [-p] [-t <PLUGIN_TAG>] [-u <DUMP_MODE>] [-r <DUMP_DIR>]
```

- `PROFILE_ID`: the ID of the profile to use for the source documents.
- `SOURCE`: the source.
- `-d DB_NAME`: the database name (default=`pythia`).
- `-c TS`: content to index: freely combine `T`=token, `S`=structure. Default=`TS`.
- `-o`: true to store the document's content in the index.
- `-p`: preflight run (diagnostic run, do not write to database).
- `-t PLUGIN_TAG`: the tag of the Pythia factory provider plugin to use.
- `-u DUMP_MODE`: the optional dump mode to use: 0=none (default), 1=dump filtered text, 2=dump filtered text and don't index.
- `-r DUMP_DIR`: the directory to dump files to when dumping is enabled with `-u`.

## Query Command

🎯 Interactively execute queries against the Pythia database. This command has no arguments, as it starts an interactive text-based session with the user, where each typed query produces the corresponding SQL query code which is then executed.

```ps1
./pythia query [-d <DB_NAME>]
```

- `-d DB_NAME`: the database name (default=`pythia`).
