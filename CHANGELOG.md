# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

# [0.0.6] - 2023-01-30

- 2023-01-26: added bulk export command and `-i` option to add profiles command.
- 2023-01-17: updated packages.
- 2023-01-13: refactored CLI to use [Spectre Console](https://spectreconsole.net).

# [0.0.5] - 2023-01-12

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
