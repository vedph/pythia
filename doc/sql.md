# SQL

- [SQL](#sql)
  - [Overview](#overview)
  - [Anatomy](#anatomy)
    - [1. Pairs](#1-pairs)
    - [2. Pair Sets](#2-pair-sets)
    - [3. Result](#3-result)
    - [Skeleton](#skeleton)

Pythia default implementation relies on a RDBMS. The current implementation is based on PostgreSQL. Consequently, context-related functions are implemented in `PL/pgSQL` (you can look at this [tutorial](https://www.postgresqltutorial.com/) for more).

Thus, a Pythia query, as defined by its own domain specific language ([via ANTLR](./antlr.md)), gets translated into SQL code, which is then executed to get the desired results. You can find the full grammar under `Pythia.Core/Assets`, and its corresponding C# generated code under `Pythia.Core/Query`.

The core of the SQL translation is found in `Pythia.Sql`, in `SqlPythiaListener`. A higher-level component leveraging this listener, `SqlQueryBuilder`, is used to build SQL queries from Pythia queries.

Also, another query builder is `SqlTermsQueryBuilder`, which is not based on a text query, but rather on a simple POCO object representing a filter for terms in the Pythia index. This is used to browse the index of terms.

All the SQL components non specific to a particular SQL implementation are found in the `Pythia.Sql` project. PostgreSQL-specific components and overrides are found in `Pythia.Sql.PgSql`.

## Overview

From the perspective of an **SQL query**, in a search we are essentially finding _positions_ inside _documents_. Positions are counted by tokens, and may indifferently refer to tokens or structures. In turn, documents, tokens and structures may all have metadata attributes.

We thus get to a set of document positions by matching the metadata (_attributes_ in Pythia lingo) attached to tokens or structures: all the matching tokens/structures provide their documents positions.

Attributes are ultimately name=value pairs. Some of these are intrinsic to a token/structure, and are known as _intrinsic_ (or _privileged_) attributes; others can be freely added during analysis, without limits.

So, we thus want to find rows of document IDs + token positions. Where do we find them in our database?

- for _tokens_, they come from `document_id`, `position` (table `occurrence`);
- for _structures_, they come from `document_id`, ranging from `start_position` to `end_position` (table `structure`). All the positions included in each structure are expanded in table `document_structure`.

Both tokens and structures have attributes we can use to filter them. Additionally, besides document ID and position each pair also adds a couple of calculated fields: the ID of the target entity (i.e. the ID of the source occurrence or structure) and its type (conventionally a letter: `t`oken or `s`tructure). Clients can use this additional information to browse entities, consolidate results including structures, etc.

Once we have found the document positions, in the end-user result we need the tokens with `document_id`, `position`, `index`, `length`, `value`, and `document`'s `author`, `title`, and `sort_key`.

So, the query proceeds by different stages; first, it collects document positions; then, it joins these with data coming from other tables in the database. These stages are represented with SQL common table expressions (CTE).

## Anatomy

The anatomy of a Pythia query includes pairs, pairs sets, and final result.

### 1. Pairs

The atoms of each query are represented by matching attributes, in the form `name operator value`. For instance, searching the word `chommoda` is equal to matching an expression like "token-value equals chommoda".

In the current Pythia syntax, each pair is wrapped in square brackets, and values are delimited by double quotes. So, the above sample would be `[value="chommoda"]`. Here, `value` is the attribute name reserved to represent an intrinsic attribute of every token, i.e. its textual value.

The reserved attribute names for each entity type are:

1. _tokens_: `value`, `language`, `position` (token-based, as always), `length` (in characters).
2. _structures_: `name`, `start_position`, `end_position`.
3. _documents_: `author`, `title`, `date_value`, `sort_key`, `source`, `profile_id`.

Any other name refers to custom attributes. Apart from the reserved names, there is no distinction between intrinsic and custom attributes: you just place the attribute name in the pair and filter it with some operator and value.

In the generated SQL, each pair is a `SELECT` command annotated with a reference name like `p1` for the first pair in the query, `p2` for the second, etc.

### 2. Pair Sets

Every single token/structure pair is wrapped in a CTE query, which gets named with an ordinal number prefixed by `s`. Besides the pair, the set can also contain additional filters defining the document/corpus scope.

For instance, this is a set with its pair, corresponding to the query `[value="chommoda"]` (=find all the words equal to `chommoda`):

```sql
WITH s1 AS
(
  -- s1: value EQ "chommoda"
  SELECT DISTINCT
  occurrence.document_id,
  occurrence.position,
  't' AS entity_type,
  occurrence.id AS entity_id
  FROM occurrence
  INNER JOIN token ON occurrence.token_id=token.id
  WHERE
  LOWER(token.value)=LOWER('chommoda')
) -- s1
```

The set `s1` is a CTE including a pair `SELECT` (`p1`). This just selects all the document's positions for tokens having their `value` attribute equal to `chommoda`. Also, some run-time metadata are added, like the type of entity originating the matches and its ID. This allows clients to do further manipulations once results are got.

To illustrate the additional content of a set, consider a query including also _document_ filters, like `@[author="Catullus"];[value="chommoda"]` (=find all the words equal to `chommoda` in all the documents whose author is `Catullus`). The query produces this set:

```sql
WITH s1 AS
(
  -- s1: value EQ "chommoda"
  SELECT DISTINCT
  occurrence.document_id,
  occurrence.position,
  't' AS entity_type,
  occurrence.id AS entity_id
  FROM occurrence
  INNER JOIN token ON occurrence.token_id=token.id
  INNER JOIN document ON occurrence.document_id=document.id
  INNER JOIN document_attribute ON occurrence.document_id=document_attribute.document_id
  WHERE
  -- doc begin
  (
  -- s1: author EQ "Catullus"
  LOWER(document.author)=LOWER('Catullus')
  )
  -- doc end
  AND
  LOWER(token.value)=LOWER('chommoda')
) -- s1
```

As you can see, additional SQL code is injected to filter the documents as requested. This is the code inside comments `doc begin` and `doc end`, plus the `JOIN`s required to include the document table.

Finally, here is a sample of a set including also _corpus_ filters, like `@@alpha beta;@[author="Catullus"];[value="chommoda"]` (=find all the words equal to `chommoda` in all the documents whose author is `Catullus` and which are found in any of the corpora with ID `alpha` or `beta`):

```sql
WITH s1 AS
(
  -- s1: value EQ "chommoda"
  SELECT DISTINCT
  occurrence.document_id,
  occurrence.position,
  't' AS entity_type,
  occurrence.id AS entity_id
  FROM occurrence
  INNER JOIN token ON occurrence.token_id=token.id
  INNER JOIN document ON occurrence.document_id=document.id
  INNER JOIN document_attribute ON occurrence.document_id=document_attribute.document_id
  -- crp begin
  INNER JOIN document_corpus
  ON occurrence.document_id=document_corpus.document_id
  AND document_corpus.corpus_id IN('alpha', 'beta')
  -- crp end
  WHERE
  -- doc begin
  (
  -- s1: author EQ "Catullus"
  LOWER(document.author)=LOWER('Catullus')
  )
  -- doc end
  AND
  LOWER(token.value)=LOWER('chommoda')
) -- s1
```

As for structures, the sample query `[$lg]` (=find all the stanzas; this is a shortcut for `[$name="lg"]`) produces this set:

```sql
WITH s1 AS
(
  -- s1: $lg
  SELECT DISTINCT
  document_structure.document_id,
  document_structure.position,
  's' AS entity_type,
  document_structure.structure_id AS entity_id
  FROM document_structure
  INNER JOIN structure ON document_structure.structure_id=structure.id
  WHERE
  EXISTS
  (
    SELECT * FROM structure_attribute sa
    WHERE sa.structure_id=structure.id
    AND LOWER(sa.name)=LOWER('lg')
  )
) -- s1
```

Notice that here the query draws from `document_structure`, which contains the expansion of each structure into all its included tokens. So, the results will include all the tokens inside the structure being matched. In the case of the sample document, these are all the tokens except for the title, which is outside stanzas.

### 3. Result

Multiple sets are connected with operators which get translated into SQL set operations, like `INTERSECT`, `UNION`, `EXCEPT`. In the case of positional operators, the CTEs are merged via specialized functions in subqueries.

The query builder walks the query syntax tree, and emits a CTE for each pair found. These CTEs are merged one after another with [set operations](https://stackoverflow.com/questions/11542288/how-do-you-union-with-multiple-ctes).

During all these steps, the only collected data are document positions (plus entity IDs and types); in the end, the final result gets sorted, paged, and joined with additional information from other tables.

### Skeleton

Thus, the SQL query built by Pythia has this skeleton:

1. **CTE list**: a CTE for each pairs set. Each set is named `sN` where `N` is the set number; inside each set, each pair is named `pN` WHERE `N` is the pair number. `WITH s1 AS (...), s2 AS (...), ...`.

2. a final **result CTE** named `r`, to combine the pair CTEs: `, r AS (...)`. This merges the CTEs using parentheses and set-operators.

3. a final `SELECT` from `occurrence`, `token` and `document` using `r` as a filter.

The skeleton is thus e.g.:

```sql
-- CTE list: lists CTEs, one for each pair
WITH s1 AS
(
  --... pair query
)
, s2 AS
(
  --...pair query
)
-- etc

-- result CTE: combines the listed CTEs as defined by query
, r AS
(
  SELECT * FROM s1
  INTERSECT
  SELECT * FROM s2
  ...
)

-- final select from token joined with document and filtered by r
SELECT DISTINCT ...
FROM occurrence
INNER JOIN token ON occurrence.token_id=token.id
INNER JOIN document ON occurrence.document_id=document.id
WHERE EXISTS
(
  SELECT * FROM r
  WHERE occurrence.document_id=r.document_id
  AND occurrence.position=r.position
)
ORDER BY document.sort_key,occurrence.position
LIMIT 20 OFFSET 0
```
