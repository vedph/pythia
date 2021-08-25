# CLI Tool

- [CLI Tool](#cli-tool)
  - [Overview](#overview)
  - [Pythia Factory Provider](#pythia-factory-provider)
  - [Command add-profiles](#command-add-profiles)
  - [Command build-sql](#command-build-sql)
  - [Command cache-tokens](#command-cache-tokens)
  - [Command create-db](#command-create-db)
  - [Command dump-map](#command-dump-map)
  - [Command index](#command-index)
  - [Command query](#command-query)

## Overview

The CLI tool is used to create and manage indexes. It is a multi-platform client, and you can start it by just typing `./pythia` in its folder. This will show you a list of commands. You can type `./pythia` followed by any of the commands plus `--help` to get more help about each specific command.

The only customizations required by the tool are:

- the connection string to your DB service. This is found in `appsettings.json`. You can edit this file, or override it using an environment variable in your host machine.
- the Pythia components factory provider. The [analysis process](analysis.md) is based on a number of pluggable components selected by their unique tag ID, and variously configured with their options. All these parameters are found in an external profile ID file (a JSON file). To instantiate these components, Pythia uses a factory, which internally has access to all its dependencies and their tag ID mappings. Thus, when you are going to add your own components, you should also change the factory accordingly, creating a new Pythia factory provider.

## Pythia Factory Provider

A Pythia factory provider is a simple class implementing interface `IPythiaFactoryProvider`, which gets a profile and a connection string, and returns a Pythia factory with its dependency injection properly configured for the set of components you will need.

To avoid rebuilding the CLI tool whenever you want to use a new provider, the tool instantiates its provider as a plugin. All the providers are stored in the tool's `plugins` folder, each under its own subdirectory. Each of these subdirectories is named after the plugin's DLL file name.

For instance, the plugin(s) library `Pythia.Cli.Plugin.Standard.dll` should be placed in a subfolder of this folder named `Pythia.Cli.Plugin.Standard`, together with all its required files. Inside this assembly, there is a single plugin (=provider implementation), tagged (with `TagAttribute`) as `factory-provider.standard`. When no plugin is specified, the CLI tool looks for the plugin with this tag and uses it as its factory provider.

If you want to use a different provider, just build your own library, place its binaries under the proper subfolder in the `plugins` directory, and add the `-t` (tag) parameter to the commands requiring it to tell the CLI to use the plugin with that tag.

This allows reusing a unique code base (and thus its already compiled binaries) even when the indexing components are external to the CLI tool. The same instead does not happen for the API, because these are typically built to create a specific Docker image with all its dependencies packed inside. In this case, you just inject the required factory, and build the customized API. This is why the API project is essentially a thin skeleton with very few code; all its relevant components are found in libraries, which get imported into several API customizations.

## Command add-profiles

Add profile(s) from JSON files to the Pythia database with the specified name.

```ps1
./pythia add-profiles <InputFilesMask> <DbName> [-i] [-d]
```

where:

- `InputFilesMask`: the input file(s) mask for the profile files to be added.
- `DbName`: the target database name.
- `-i`: write indented JSON.
- `-d`: dry run (diagnostic run, do not write to database).

## Command build-sql

Interactively build SQL code from queries. This command has no arguments, as it starts an interactive text-based session with the user, where each typed query produces the corresponding SQL code.

```ps1
./pythia build-sql
```

## Command cache-tokens

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

## Command create-db

Create or clear the Pythia database with the specified name.

```ps1
./pythia create-db <DbName> [-c]
```

where:

- `DbName`: the target database name.
- `-c`: clear the database if exists.

## Command dump-map

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

## Command index

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

## Command query

Interactively execute queries. This command has no arguments, as it starts an interactive text-based session with the user, where each typed query produces the corresponding SQL code which is then executed against the specified database.

```ps1
./pythia query <DbName>
```

where:

- `DbName`: the index database name.
