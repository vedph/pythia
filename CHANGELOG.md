# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.1.0] - 2024-09-19

- 2024-09-19: **rewritten listeners** to allow for fully recursive queries freely mixing logical and location operators. This is not a breaking change, as the grammar stays the same; I just changed its translation to SQL. The old listener is still here, though deprecated, and the SQL builder can call it via `LegacyBuild`.

Essentially the query DSL is based on single name-operator-value expressions, named pairs because they couple a name and a value, and connects these pairs into expressions using two types of operators:

- logical operators (`AND`, `OR`, `AND NOT`);
- special operators, called location operators, which are rendered in SQL in 2 possible ways:
  - as an `INNER JOIN` between left and right pairs;
  - as a `WHERE NOT EXISTS(SELECT 1 FROM ... WHERE ...)` when negated, where the subquery involves the left and right pairs.

The query DSL also allows for brackets for precedence.

So in general I am following this conversion approach:

- each pair, which is a terminal, is converted to a CTE. I build a list of these CTEs, like `WITH s1 AS SELECT..., s2 AS SELECT...` etc.
- for expressions involving no operators or logical operators, there is a 1:1 correspondance between the DSL and SQL: brackets are converted as brackets, and logical operators (`AND`, `OR`, `AND NOT`) are converted into set operators (`INTERSECT`, `UNION`, `EXCEPT`).

So for instance, from a query like "(a OR b) AND c", assuming that a=s1, b=s2, and c=s3, I would get something like:

```sql
(
SELECT * FROM s1
UNION
SELECT * FROM s2
)
INTERSECT
SELECT * FROM s3
```

Location operators instead (LOCOP) require a different syntax. For instance, "a LOCOP b" is converted to:

```sql
SELECT * FROM s1
INNER JOIN s2 ON ...s1 and s2 conditions...
```

and "a NOT LOCOP b" is converted to:

```sql
SELECT * FROM s1
WHERE NOT EXISTS(SELECT 1 FROM s2 WHERE ...s1 and s2 conditions...)
```

The problem arises when dealing with groups (as defined by brackets) having at any of both of their sides a LOCOP. So, "(a OR b) AND c"; "a AND (b OR c)"; "(a OR b) AND (c OR d)" pose no issues for the conversion, because I can just output brackets and operators where they are in the original DSL. Instead, for LOCOP I have a different syntax. Also, a LOCOP is implemented using a custom PostgreSQL function, which must be called with the names of the left and right side terms it should evaluate for their location. For instance, A simple query like "a LOCOP b" would produce a SQL code like this:

```sql
-- CTE list
WITH s1 AS ..., s2 AS ...
-- result: this is the query built by the listener
, r AS
(
-- pyt_is_near_within(a.p1, a.p2, b.p1, b.p2, n=0, m=0)
SELECT s1.* FROM s1
INNER JOIN s2 ON s1.document_id=s2.document_id AND
pyt_is_near_within(s1.p1, s1.p2, s2.p1, s2.p2, 0, 0)
) -- r
-- ... omitted final code which selects from r
```

Here I just have two simple terms, a and b; but if I have "a LOCOP (b OR c)", or "(a OR b) LOCOP c", or "(a OR b) LOCOP (c OR d)", this requires wrapping the CTE subqueries into a subquery with an alias, so that I can use that alias to refer to the group from the function. So, I wrote two new listeners which (a) collect all the pairs, numbering them progressively (s1, s2, ...) and generating their CTE SQL; and (b) generate the query by putting the various CTEs together with the correct operators, brackets, and nesting, using a stack of SQL fragments. So, now the conversion happens in two steps, and the two listeners are used in sequence. The old listener is still here and it is still the default, but it will be deprecated and replaced as soon as I am sure that the new listeners are working correctly. Currently, a new set of tests is used for the SQL query builder, and the build-sql command in the CLI tool can be used to test the new listeners when passing `-n`.

- 2024-09-18:
  - changed XML structure parser so that the structure value is saved in `span` rather than being added as a `value` attribute.
  - added `_` prefix for structure attributes. This allows queries to refer to multiple structure attributes rather than just to the structure's name. For instance, `[$fp-lat] AND [_value="pro tempore"]` means that not only we want to find a structure named `fp-lat`, but also that the value of this structure should be `pro tempore`. Should we use `value` we would find nothing, because any non `_`-prefixed name implies a token's attribute.
  - added indexing option for words index: `-n` allows specifying which tokens should be excluded from the index, by providing the name of any attributes which should NOT be attached to that token. For instance, adding `-n email -n foreign -n pers-name` would exclude from words index (and consequently from lemmata index) all the tokens having at least 1 attribute named `email`, `foreign`, or `pers-name`. This allows pulluting the word and lemmata index with irrelevant data.
- 2024-09-16: updated packages.
- 2024-09-09: updated packages.

## [4.0.3] - 2024-08-31

- 2024-08-29:
  - fixed right align implementation.
  - updated documentation.
  - updated packages.

## [4.0.2] - 2024-08-10

- 2024-08-10: refactored locop translation.
- 2024-08-09:
  - added search export endpoint.
  - refactored search endpoint as GET.
- 2024-08-05: made `ITokenFilter` async.
- 2024-07-31: ⚠️ model refactoring in progress, bumping all the versions to 4.x.x (release 3.0.3 is the last with the old model): a more denormalized storage model replaces the original one to enhance performance and simplify code:
  - the SQL schema has been updated to reflect the new model.
  - the index repository has been updated to reflect the new model.
  - CLI tool and API have been updated to reflect the new model.
  - terms have been replaced by words and lemmata.
  - more tests are being added.
  - some documentation has been updated, but more is needed.
- 2024-07-15: updated packages.

## [3.0.5] - 2024-06-03]

- 2024-06-03: updated packages.

## [0.0.12] - 2024-05-27

- 2024-05-27: updated test packages.
- 2024-05-22: updated packages.
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
