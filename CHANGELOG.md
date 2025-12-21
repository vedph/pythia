# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

- 2025-12-21: added more options to index checker.
- 2025-12-20: improved `XmlStructureParser` to replace `RemovedAttributes` setting with `OverriddenAttributes` to allow not only removing but also overriding and adding attributes according to the ghost structure detected.
- 2025-12-19:
  - updated packages.
  - added language parameter to build word index command.
  - added ignore POS mismatches to word index check command.
- 2025-12-18:
  - updated packages.
  - added whitelist to word checker.
  - added result code to the result of word checker.

### [6.0.0] - 2025-11-24

- 2025-11-24: ⚠️ upgraded to NET 10.
- 2025-11-21: ⚠️ refactor word index building for word counts (previous version is in release 5.1.4). The calculation of word counts needed to be refactored to improve performance and remove potential racing issues. Rather than millions of queries, use a bulk, longer query.
- 2025-11-16:
  - refactored documentation.
  - fixes to custom sort options in terms query builder.
  - changed default sort order to `value, sort_key, p1` (was `sort_key, p1`).
- 2025-09-29: updated packages.
- 2025-09-17: updated packages.
- 2025-09-08: updated packages.
- 2025-07-29: updated packages.
- 2025-07-23: added `ExtendedPos` option to `UdpTokenFilter` to append XPOS to UPOS (separating them with a dot) if requested.
- 2025-07-22: fix to matched token location in `UdpTokenFilter`.
- 2025-07-20:
  - updated default UDP model version.
  - improvement: `UdpTokenFilter` can now deal with multiword tokens like `della` = `di` + `la`.
  - improvement: avoid UDP plugin overriding a token already tagged as `PROPN` or `ABBR` (or other tags, as defined by the `PreservedTags` option) when UDP classified it in another way. This is not affecting the usual pipeline, where ghost structures are detected after token filtering, but it might be useful when other filters precede the UDP one.
  - updated packages.

The UDP token filter is used to get POS data from an UDPipe service and inject it into the token being processed. A corner case here is represented by multiword tokens like Italian "della" = "di" + "la".
For instance, consider this Italian sentence: "Questo è della casa.". The POS tagger analyzes this as follows:

| id  | form   | lemma  | UPos  | XPos | Feats                                                     | Head | DepRel | Deps | Misc                            |
| --- | ------ | ------ | ----- | ---- | --------------------------------------------------------- | ---- | ------ | ---- | ------------------------------- |
| 1   | Questo | questo | PRON  | PD   | Gender=Masc\|Number=Sing\|PronType=Dem                    | 5    | nsubj  | -    | TokenRange=0:6                  |
| 2   | è      | essere | AUX   | VA   | Mood=Ind\|Number=Sing\|Person=3\|Tense=Pres\|VerbForm=Fin | 5    | cop    | -    | TokenRange=7:8                  |
| 3-4 | della  | -      | -     | -    | -                                                         | -    | -      |      | TokenRange=9:14                 |
| 3   | di     | di     | ADP   | E    | -                                                         | 5    | case   | -    | -                               |
| 4   | la     | il     | DET   | RD   | Definite=Def\|Gender=Fem\|Number=Sing\|PronType=Art       | 5    | det    | -    | -                               |
| 5   | casa   | casa   | NOUN  | S    | Gender=Fem\|Number=Sing                                   | 0    | root   | -    | SpaceAfter=No\|TokenRange=15:19 |
| 6   | .      | .      | PUNCT | FS   | -                                                         | 5    | punct  | -    | SpaceAfter=No\|TokenRange=19:20 |

As you can see, here `della` is the multiword token (tokens 3-4) and its children (3 and 4) follow it.

Now, in Pythia we need to get POS data for the single `della` token. Filters applied to this token know only about it; `di` and `la` are 'artifacts' from the POS tagger, while here we need to represent all the data on top of the original text, where only `della` exists.

The filter has now been improved to:

1. detect the multiword token (`della`);
2. collect its "children" tokens (`di` and `la`);
3. lookup the configuration provided in filter options to inject specific POS data into the token being processed depending on its children.

>Note that the old implementation relied on token objects references as got from the UDPipe library. Yet, it seems that in such corner cases token instances are reused, so that identifying them by object reference led to isses. This undocumented detail of the UDPipe library had a cascading effect on causing issues in POS tagging.

In most cases, we need to POS-tag the form as it appears on the text (like `della`) rather than its analysis (`di` + `la`), because we are here focusing on a text-based search which must reflect the document's text. Compare for instance the Morph-It list of Italian inflected forms, where POS tags are designed to fit this specific language, and forms like `della` are at the same level as words like `di` or `la`. In this case, the POS tag is `ART-F:s`, meaning article, feminine, singular.

In the same way, here for each typology of such composite forms we must decide which subset of POS data from its components should make their way into the POS tag of the surface form `della`:

- `di`: `ADP.E` (preposition)
- `la`: `DET.RD` (determinative, definite article), with additional features: `Def`, `Fem`, `Sing`, `Art`.

Here we will typically let the morphologically and syntactically richer component prevail, so that we treat the whole form `della` just like a `DET.RD`, ignoring its composite origin. Anyway, the decision will vary accoring to the typology of composition in the various forms presenting multi-word tokens. So, we need a generalized and configurable strategy to deal with them.

To provide a generic, reusable configuration to build a new lemma and tag by variously collecting data from the children tokens, you can use the `Multiwords` option in the filter configuration object. See unit tests for an example. In the case of `della`, the configuration tells the filter to match tokens `ADP.E` followed by `DET.RO`, and provide the token's value as the lemma (which is the default behavior), plus UPOS, XPOS and features from the second token (`la`). Here is how the corresponding JSON configuration object would appear:

```json
{
  "Id": "token-filter.udp",
  "Options": {
    "Props": 43,
    "Language": "",
    "Multiwords": [
      {
        "MinCount": 2,
        "MaxCount": 2,
        "Tokens": [
          {
            "Upos": "ADP",
            "Xpos": "E"
          },
          {
            "Upos": "DET",
            "Xpos": "RD"
          }
        ],
        "Target": {
          "Upos": "DET",
          "Xpos": "RD",
          "Feats": {
            "*": "2"
          }
        }
      }
    ]
  }
},
```

This matches any 2-tokens multiword token having `ADP.E` for its first token and `DET.RD` as its second one. We might also add `Feats` to the filters, but that's not required here. The resulting tag for `della` is `DET.RD` and its `Feats` are copied from all the features of the second token. The lemma is just equal to the token's value (`della`), as this is the default when `Lemma` is not specified.

- 2025-07-09:
  - updated packages.
  - fixed test.
- 2025-06-24:
  - refactored tagger. This is not yet in use, but it will be leveraged for checking the index of lemmata.
  - fix to `SqlIndexRepository.InsertWordsAsync`: the method was assigning a `word_id` FK also to those token spans excluded by POS or attributes. The fixed logic is now in `AssignWordIdsAsync`. The same fix was also applied to `InsertLemmataAsync`.
- 2025-06-15: added to `SqlPythiaPairListener` support for `lemma_id` and `word_id` in search, also building a specific SQL for privileged attributes which are numeric. Now you can make queries like `[lemma_id="5"]`, which will be useful for querying the spans of an index entry.
- 2025-06-14:
  - updated packages.
  - fixed test data. Note that corpus data for test `ValueEqChommodaInCorpus_1` are still missing, so the test will fail. This is because the corpus data are not set in the asset sql used to seed the test database.

### [5.1.3] - 2025-06-03

- 2025-06-03: updated packages.

### [5.1.2] - 2025-02-10

- 2025-02-10: changed backup order in write bulk command. This reflects the new dependencies after adding pos to lemma. In fact, it does not change anything for writing, but it can be useful to have the correct sequence for restoring. This was required because altering the table on an existing database produced a different binary footprint, which is not compatible with the newly created database (where the additional field is there since the table creation, rather than added later) on restore on another machine. In this case, the procedure to avoid recreating the database was backing up the old database and restoring it into a newly created one (via `create-db`). This requires a custom dump format and a different restore type as we need to restore data only, rather than also the schema, e.g.:

```sh
pg_dump -U postgres -d pythia -Fc -f pythia.dump

./pythia create-db -d test

pg_restore -U postgres -d test -Fc --data-only --table app_role pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table app_role_claim pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table app_user pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table app_user_claim pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table app_user_login pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table app_user_role pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table app_user_token pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table profile pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table document pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table document_attribute pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table corpus pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table document_corpus pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table lemma pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table word pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table span pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table span_attribute pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table word_count pythia.dump
pg_restore -U postgres -d test -Fc --data-only --table lemma_count pythia.dump

./pythia bulk-write c:/users/dfusi/desktop/ac/bulk -d test
```

### [5.1.4] - 2025-02-11

- 2025-02-11: fix to lemma SQL query builder for missing AND before pos in some circumstances.
- 2025-02-09: updated packages.

### [5.1.2] - 2025-02-08

- 2025-02-08: fix to `XmlHighlighter` for namespace handling.

### [5.1.1] - 2025-02-08

- 2025-02-08: fix to `InsertLemmaCountsAsync` (avoid null lemmata).

### [5.1.0]- 2025-02-06

- 2025-02-06:
  - added pos to lemma.
  - partially refactored lemma index building procedure.

### [5.0.7] - 2025-01-29

- 2025-01-29:
  - updated packages (affecting only API and MsSql).
  - fixes to `XmlHighlighter`.

### [5.0.6] - 2025-01-20

- 2025-01-20:
  - fix to elapsed time in CLI.
  - added optional notification to build word index command.
  - updated packages.
- 2025-01-14: updated packages and fix to messaging in index command.
- 2025-01-11:
  - updated test packages.
  - added notification to indexing command in CLI tool. Please notice that currently this uses [MailJet](https://www.mailjet.com) and it requires you to save your API keys into environment variables (named `MAILJET_API_KEY_PUBLIC` and `MAILJET_API_KEY_PRIVATE`).
- 2024-01-09: updated packages (not affecting packaged libraries).
- 2024-12-29: updated packages.
- 2024-12-19: updated packages.
- 2024-12-16: updated packages.

### [5.0.5] - 2024-12-05

- 2024-12-05: updated packages and generated Docker image 5.0.5.
- 2024-12-01: updated packages.
- 2024-11-22: moved Corpus projects into Pythia solution, as Pythia is now the primary (and currently only) client of Corpus.
- 2024-11-21: fix to `XmlStructureParser`: length of detected structure must exactly overlap the length of the source XML element to allow proper highlight.

### [5.0.3] - 2024-11-21

- 2024-11-21: updated Corpus packages (refactored XML text picker). ⚠️ This change implies that in your profile configuration for the XML text picker you should leave the default `HitOpen` and `HitClose` escapes as double braces, rather than setting them to opening and closing tags of an `hi` element, and set the new text picker's `HitElement` to the hit element used to wrap highlighted text, e.g. `<hi rend="hit"></hi>`. This is because the text picker will now use the `HitElement` to wrap the highlighted text, and will insert it in the document structure as required to preserve it. Also, you should set `WrapperPrefix` to some wrapper element's opening tag, like `<div>`, and `WrapperSuffix` to the corresponding closing tag, like `</div>`, to ensure that the highlighted text is always wrapped in a container element, even when the text spans across multiple nodes.
- 2024-11-19:
  - updated packages.
  - refactored API code to remove `Startup` class.
- 2024-11-17: replaced Swagger with Scalar (open `scalar/v1`) leveraging .NET 9 OpenAPI components:
  - remove package `Swashbuckle.AspNetCore`.
  - add packages `Microsoft.AspNetCore.OpenApi` and `Scalar.AspNetCore`.
  - remove `ConfigureSwaggerServices` method from `Startup`.
  - add `Scalar.AspNetCore` package, configuring its endpoints in `Startup.Configure` by calling `endpoints.MapOpenApi();` and `endpoints.MapScalarApiReference();`.

## [5.0.1] - 2024-11-16

- 2024-11-16:
  - removed obsolete code.
  - fixed missing check for `NOT` + left/right align in listener.
- 2024-11-15: fix to some span distance functions: `pyt_is_overlap_within`, `pyt_is_left_aligned`, `pyt_is_right_aligned` for corner cases.
- 2024-11-14: fix to dump map command.

## [5.0.0] - 2024-11-13

- 2024-11-13:
  - added POS filter to word index builder.
  - ⚠️ upgraded to .NET 9.

## [4.2.1] - 2024-11-10

- 2024-11-10: added new features to XML structure parser: options `OverriddenPos` and `RemovedPosTags` for ghost structure definitions allow to override the POS assigned to a token contained in a ghost structure, and to remove other POS-dependent tags assigned to it on the basis of a wrong POS assumption. This is useful for instance for TEI elements like `abbr`, `foreign`, or `date`, where the POS tagger might be fooled into assuming the wrong POS. For example, the `abbr` definition might be like this:

```json
{
  "Name": "abbr",
  "XPath": "//tei:abbr",
  "ValueTemplate": "1",
  "TokenTargetName": "abbr",
  "OverriddenPos": "ABBR",
  "RemovedPosTags": [ "clitic", "definite", "degree", "deprel", "gender",
      "lemma", "mood", "number", "numtype", "person", "poss", "prontype",
      "tense", "verbform"
  ]
},
```

> Of course `ABBR` is a custom POS tag. You might as well use a standard tag like `SYM`, but this would fail to give a specific status to abbreviations.

## [4.2.0] - 2024-11-10

- 2024-11-10:
  - refactored standard tokenizer to split on currency characters.
  - refactored Italian token and literal filters to preserve digits and currency characters. This means that using this tokenizer now a currency symbol or a number is a token and they are not discarded from the index.
  - rewritten XML sentence parser using `XmlTagRangeSet` for a more robust implementation and better text values.
  - refactored `XmlTagFillerTextFilter` using `XmlTagRangeSet` for a more robust implementation.
- 2024-11-08:
  - added the feature to create integer-only bins in words index.
  - updated packages.
- 2024-10-31:
  - added dump text spans command to the CLI, while implementing the corresponding methods in the repository. All versions bumped to 4.1.6.
  - fix to `XmlSentenceParser`: a new implementation for `FillEndMarkers` resolves an issue arising when dealing with explicit namespaces in XML documents. The new implementation uses a more robust approach not relying on `OuterXml()`, which produces an XML code which may not align with the input code because of an inserted `xmlns` attribute. So, in code `<TEI xmlns="http://www.tei-c.org/ns/1.0"><text><body><p>It is 5 <choice><abbr>P.M.</abbr><expan>post meridiem</expan></choice>.</p></body></text></TEI>` it should now correctly replace dots with spaces inside the `abbr` element thus getting `P M ` from `P.M.`.
  - added `YearDateValueCalculator`.
- 2024-10-30: updated packages.
- 2024-10-18:
  - added more stats.
  - updated packages.
- 2024-10-16: added missing span stats to SQL repository implementation.

## [4.1.1] - 2024-10-16

- 2024-10-16:
  - better metadata for controllers.
  - updated packages.
- 2024-10-10: updated packages.
- 2024-10-05:
  - updated packages to include improvements in the `Corpora` library. These improvements now allow specifying XPath expressions returning a single value rather than a nodeset as XML structure parser value template arguments.
  - set XML sentence parser result value to be the count of the (whitespace-normalized) characters in the sentence.
- 2024-10-02: updated packages (including Corpora upgraded to .NET 8).

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
