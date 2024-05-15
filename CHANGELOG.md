# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

- 2024-05-15: updated packages and refactored authentication according to the updated auth library. Application-specific models and user mappers now are no longer required.
- 2024-05-08: updated packages.
- 2023-11-27: ⚠️ upgraded to .NET 8.
- 2023-10-02: updated packages.
- 2023-09-26: updated packages.
- 2023-06-29: added `check-meta` command to CLI.
- 2023-06-28: updated packages.
- 2023-04-13:
  - fixes in default query in CLI tool build-sql command.
  - added `id` privileged attribute to document search. You can now do queries including document ID filters, like `[@id="1"];[value="hello"]`. This is especially useful when testing, to restrict results to a single document. You can use any valid numeric operator, or even use the string-equals operator, which internally gets remapped to numeric-equals.

## [0.0.11] - 2023-04-11

- 2023-04-07: rebuilt ANTLR artifacts after adding the missing `AND` to `txtExpr` in the grammar.
- 2023-04-05:
  - added Chiron experimental plugins.
  - fixed sort by reversed value in terms query builder.

## [0.0.10] - 2023-03-29

- 2023-03-29: more commands.
- 2023-03-27:
  - updated packages.
  - more indexes in PgSql scheme.

## [0.0.9] - 2023-03-25

- 2023-03-25: fix to terms distribution "other" count.
- 2023-03-24: updated packages.
- 2023-03-09: fix to `XmlTagFillerTextFilter`: when extracting XML elements to be filled, and getting their outer XML, the default namespace is added even though not present in the source XML code. This caused the outer XML length to be larger than the original one, and thus the filling process would proceed beyond its right boundary, overwriting other text. As specified in the [docs](https://learn.microsoft.com/en-us/dotnet/standard/data/xml/managing-namespaces-in-an-xml-document), "while usually a `xmlns` attribute is followed by `:` and a prefix like `xmlns:tei`, to define a default namespace you use the bare `xmlns` name without colon and prefix" (this is an unprefixed namespace, not a null namespace; a null namespace cannot exist); "the default namespace is declared in the root element and applies to all unqualified elements in the document. Default namespaces apply to elements only, not to attributes. To use the default namespace, omit the prefix and the colon from the declaration on the element". For a more robust solution, the filler compares the outer XML of its target elements with the source XML code without taking into account mismatches due only to an `xmlns` attribute present in one of the two compared strings (if both strings have `xmlns`, they must be equal -- otherwise an exception will be thrown).

## [0.0.8] - 2023-02-27

- 2023-02-25: updated packages.
- 2023-02-12: updated packages.

## [0.0.7] - 2023-02-12

- 2023-02-06: updated packages.
- 2023-01-31:
  - refactored infrastructure to use new `Fusi.Tools.Configuration` rather than `Fusi.Tools.Config`. This also implied upgrading `Corpus` dependencies to 8.x.x. As a consequence, `SimpleInjector` dependency was dropped. Factories at `Pythia.Api.Services` were updated so that now they build their own `IHost` instance configured according to the specified profile. The same happened to all the `Pythia.Cli.Plugin...` providers.
  - updated references to the new `Fusi.Microsoft.Extensions.Configuration.InMemoryJson`, which no more depends on Newtonsoft JSON libraries and added `IHostBuilder`-related extensions.
  - all the library versions have bumped from `2.1.0` to `3.0.0`.

## [0.0.6] - 2023-01-30

- 2023-01-26: added bulk export command and `-i` option to add profiles command.
- 2023-01-17: updated packages.
- 2023-01-13: refactored CLI to use [Spectre Console](https://spectreconsole.net).

## [0.0.5] - 2023-01-12

- 2023-01-12: updated packages.
- 2023-01-09: refactored CLI infrastructure.
- 2022-12-26:
  - refactored terms query builder to use additional table `token_occurrence_count` which is populated by the new repository's method `FinalizeIndex`.
  - added value length min/max filters to term filters.
- 2022-12-22: added terms distribution.
- 2022-12-17: added `Id` to `SearchResult` for better handling of search results by client code.

## [0.0.4] - 2022-12-15

- 2022-12-15: updated packages.
- 2022-12-08:
  - added context to filters.
  - added UDP project.
- 2022-11-10: upgraded to NET 7.
- 2022-11-05:
  - refactored all projects for nullability check.
  - all versions aligned to 2.1.0.
- 2022-10-27: updated packages.
- 2022-09-02: updated packages with fix in Corpus document sort.
- 2022-08-27: updated packages.
- 2022-06-28: updated packages.
- 2022-05-17: updated packages and fixed test DD SQL.
- 2022-04-18: updated packages and regenerated ANTLR artifacts with new version.

## [0.0.1] - 2022-01-17

- Upgraded all libraries to .NET 6.0.
