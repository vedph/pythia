# SQL Queries

- [SQL Queries](#sql-queries)
  - [Query Translation](#query-translation)
    - [Walking the Tree](#walking-the-tree)
  - [Query Overview](#query-overview)
  - [Anatomy](#anatomy)
    - [1. CTE List](#1-cte-list)
    - [2. Result CTE](#2-result-cte)
    - [3. Merger Query](#3-merger-query)
  - [Examples](#examples)

Pythia default implementation relies on a RDBMS. So, querying a corpus means querying a relational database, which allows for a high level of customizations and usages by third-party systems. Of course, users are not required to enter complex SQL expressions; rather, they just type queries in a custom domain specific language (or compose them in a graphical UI), which gets automatically translated into SQL.

>The current Pythia implementation is based on PostgreSQL. Consequently, context-related functions are implemented in `PL/pgSQL` (you can look at this [tutorial](https://www.postgresqltutorial.com/) for more).

## Query Translation

A Pythia query, as defined by its own domain specific language ([via ANTLR](./antlr.md)), gets translated into SQL code, which is then executed to get the desired results. You can find the full grammar under `Pythia.Core/Assets`, and its corresponding C# generated code under `Pythia.Core/Query`.

🔬 The ANTLR grammar for the Pythia query language is in `Pythia.Core/Query/PythiaQuery.g4`.

To play with the grammar, you can use the [ANTLR4 lab](http://lab.antlr.org/):

1. paste the grammar in the left pane under the heading "Parser". Also, ensure to clear the "Lexer" pane completely.
2. in the "Start rule" field, enter `query`.
3. type your expression in the "Input" pane, and click the "Run" button.

>All the SQL components non specific to a particular SQL implementation are found in the `Pythia.Sql` project. PostgreSQL-specific components and overrides are found in `Pythia.Sql.PgSql`. The core of the SQL translation is found in `Pythia.Sql`, in `SqlPythiaListener`. A higher-level component leveraging this listener, `SqlQueryBuilder`, is used to build SQL queries from Pythia queries. Also, another query builder is `SqlTermsQueryBuilder`, which is not based on a text query, but rather on a simple POCO object representing a filter for terms in the Pythia index. This is used to browse the index of terms.

### Walking the Tree

🛠️ This is a technical section.

The target script skeleton is as follows:

```txt
-- (1: pair:exit 1st time)
WITH sN AS (SELECT...)
-- (2: pair:exit else)
, sN AS (SELECT...)*
-- (3: query:enter)
, r AS
(
    -- (4: pair:exit)
    SELECT * FROM s1
    -- (5a: terminal, non-locop operator)
    INTERSECT/etc
    -- (5b: terminal, locop operator; save its closing code to dct with key=parent teLocation)
    INNER JOIN (
      SELECT *
      FROM s2
      WHERE...
    -- (6: teLocation:exit; get the closing code saved by 5b and emit/push it)
    ) AS s1_s2
    ON s1...s2
)
-- (7: query:exit)
SELECT...INNER JOIN r ON... ORDER BY... LIMIT... OFFSET...;
```

(1) the CTE list includes at least 1 CTE, `s1`. Each pair produces one.
(2) all the other CTEs follow, numbered from `s2` onwards.
(3) then, the final CTE is `r`, the result. It connects all the CTEs in one of modes 4 or 5. The bounds of (3) are emitted by `query` enter/exit.
(4) is the `SELECT` which brings into `r` data from the CTE corresponding to the left pair.
(5a) is the case of "simple" `txtExpr` (handled by `pair` exit in context `txtExpr` of type different from `teLocation`); left is connected to right via a logical operator or bracket. The logical operator becomes a SQL operator, and the bracket becomes a SQL bracket. This is handled by a terminal handler for any operator which is not a `locop`.
(5b) is the case of location expression, where left is connected to right via a `locop`. In this case, we must select from s-left and `INNER JOIN` it with a subquery drawing data from s-right. As other locop's might follow, we cannot close the subquery immediately; rather, we must save its close code, because the next locop will nest inside this one with another (5b) (=another `INNER JOIN`). So, this is handled by a terminal handler for any `locop` operator. This should emit the code for (5b), and save it under a key corresponding to the `teLocation` being the parent of this `locop`.
(6) whenever we exit a `teLocation`, we emit its closing code by getting it from a dictionary (where it was saved by 5b above), using `teLocation` as the key. The composite name of the subquery (`s1_s2`) is not referenced from other parts of the SQL code, but is required by the SQL syntax. A corner case is when exiting a `teLocation` which is child of another one having `locop` as its operator, e.g.:

![a before b before c](img/a_before_b_before_c.png)

In this case, the ending SQL code should be pushed in a stack rather than emitted. It will then be emitted when exiting `teLocation` again, unless the same corner case happens again.

(7) finally, an end query joins `r` with additional data from other tables, orders the result, and extracts a single page.

Thus, our listener attaches to these points:

(a) **context**:

- `corset`:
  - _enter_: current set type=corpora;
  - _exit_: current set type=text, build filtering clause for corpora (`_corpusSql`).
- `docset`:
  - _enter_: current set type=document;
  - _exit_: current set type=text, finalize `_docSql`.

The SQL filters for corpus (`INNER JOIN`) and documents (`WHERE`), when present, will be appended to each pair emitted. This is why they always come before the text query.

(b) **text query**:

- `query`: this is for the "outer" query; it builds `r` start and end, and adds the final `SELECT`.
  - _enter_: reset and open result (`r AS (`: (3) above);
  - _exit_: close result (`)`) and add the final merger select ((7) above).

- `pair` (a `pair` is the parent of either a `tpair` (text pair) or `spair` (structure pair)):
  - _exit_ (only if type=text): add CTE to list (`sN`, (1) or (2) above); add `SELECT * from sN` to the `r` CTE ((4) above).

- **any terminal node**:
  - if in _corpora_, add the corpus ID to the list of collected IDs;
  - if in _document_, handle doc set terminal (operator or pair): this appends either a logical operator, bracket, or pair;
  - if in _text_:
    - if non-locop operator: add to `r` the corresponding SQL operator ((5a) above).
    - if locop operator: add to `r` `INNER JOIN (SELECT * FROM s-right WHERE...)` and store `) AS s-left_s-right` under key=parent `teLocation`.
  
- `locop`:
  - _enter_: clear locop args (`_locopArgs`), handle `NOT` if any, set `ARG_OP`;
  - _exit_: validate args and eventually supply defaults.
- `locnArg`:
  - _enter_: collect n arg value in `_locopArgs`.
- `locsArg`:
  - _enter_: collect s arg value in `_locopArgs`.
- `txtExpr#teLocation`:
  - _enter_: set location state context to this context and increase its number;
  - _exit_: reset location state context while keeping query-wide data (number and dictionary).

## Query Overview

- 💡see [storage](storage.md) for information about the RDBMS schema.

From the point of view of the database index, in a search we are essentially finding _positions_ inside _documents_. Positions are counted by tokens, and may indifferently refer to tokens or structures. In turn, documents, tokens and structures may all have metadata attributes.

In a search, each token or structure in the query gets 2 positions: a start position (`p1`), and an end position (`p2`). In the case of tokens, by definition `p1` is always equal to `p2`. This might seem redundant, but it allows for handling both object types with the same model.

We get to positions by means of metadata (_attributes_ in Pythia lingo) attached to tokens or structures: all the matching tokens/structures provide their documents positions.

Attributes are ultimately name=value pairs. Some of these are intrinsic to a token/structure, and are known as _intrinsic_ (or _privileged_) attributes; others can be freely added during analysis, without limits.

So, ultimately in a query we find rows of document IDs + token positions, together with their ID (`entity_id`, i.e. the ID of the token occurrence, or of a structure) and object type (`t`oken or `s`tructure: `entity_type`). Where do we find them in our database?

- for _tokens_, they come from `document_id`, `position` (table `occurrence`);
- for _structures_, they come from `document_id`, ranging from `start_position` to `end_position` (table `structure`). For convenience, all the positions included in each structure are expanded in table `document_structure`; usually anyway we just deal with the start and end positions, because these define the structure's extent.

Once we have found the document positions, in the end-user result we need the tokens with `document_id`, `position`, `index`, `length`, `value`, and `document`'s `author`, `title`, and `sort_key`. So, the query proceeds by different stages; first, it collects document positions; then, it joins these with data coming from other tables in the database. These stages are represented with SQL common table expressions (CTE).

## Anatomy

The anatomy of a Pythia query includes:

1. a list of data sets defined by CTEs (named like `s1`, `s2`, etc.), each representing a name-operator-value condition ("pair").
2. a result data set, defined by combining sN sets into one via a CTE (named `r`).
3. a final merger query which joins `r` data with additional information and provides paging.

### 1. CTE List

As we have seen, the core components of each query are represented by matching objects attributes, in the form `name operator value`. This is what we call a "pair", which joins a name and a value with some type of comparison operator. For instance, searching the word `chommoda` is equal to matching an expression like "token-value equals chommoda".

In the current Pythia syntax:

- each pair is wrapped in _square brackets_;
- values are delimited by _double quotes_ (whatever their data type).

So, the above sample would be represented as `[value="chommoda"]`, where:

1. `value` is the attribute name reserved to represent an intrinsic attribute of every token, i.e. its textual value;
2. `=` is the equality operator.
3. `"chommoda"` is the value we want to compare against the selected attribute, using the specified comparison operator.

📖 The _reserved attribute names_ for each entity type are:

1. _tokens_: `value`, `language`, `position` (token-based, as always), `length` (in characters).
2. _structures_: `name`, `start_position`, `end_position`.
3. _documents_: `author`, `title`, `date_value`, `sort_key`, `source`, `profile_id`.

Any other name refers to custom attributes. Apart from the reserved names, there is no distinction between intrinsic and custom attributes: you just place the attribute name in the pair, and filter it with some operator and value.

Each pair gets translated into a SQL CTE representing a single data set (named `sN`, i.e. `s1`, `s2`, etc.), which is appended to the list of CTEs consumed by the rest of the SQL query. Besides the pair, the set can also contain additional filters defining the document/corpus scope.

For instance, this is a set with its pair, corresponding to the query `[value="chommoda"]` (=find all the words equal to `chommoda`):

```sql
-- CTE list
WITH s1 AS
(
  -- s1: value EQ "chommoda"
  SELECT DISTINCT
  occurrence.document_id,
  occurrence.position AS p1,
  occurrence.position AS p2,
  't' AS entity_type,
  occurrence.id AS entity_id
  FROM occurrence
  INNER JOIN token ON occurrence.token_id=token.id
  WHERE
  LOWER(token.value)=LOWER('chommoda')
) -- s1
```

As you can see, set `s1` is a CTE selecting all the document's start (`p1`) and end (`p2`) positions for tokens having their `value` attribute equal to `chommoda`. Also, some runtime metadata are added, like the type of entity originating the matches (`entity_type`), and its ID (`entity_id`). This allows clients to do further manipulations once results are got.

To illustrate the additional content of a set, consider a query including also _document_ filters, like `@[author="Catullus"];[value="chommoda"]` (=find all the words equal to `chommoda` in all the documents whose author is `Catullus`). The query produces this set:

```sql
-- CTE list
WITH s1 AS
(
  -- s1: value EQ "chommoda"
  SELECT DISTINCT
  occurrence.document_id,
  occurrence.position AS p1,
  occurrence.position AS p2,
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
-- CTE list
WITH s1 AS
(
  -- s1: value EQ "chommoda"
  SELECT DISTINCT
  occurrence.document_id,
  occurrence.position AS p1,
  occurrence.position AS p2,
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

Until now, we have considered examples of tokens. The same syntax anyway can be used to find structures. For instance, the sample query `[$lg]` (=find all the stanzas; this is a shortcut for `[$name="lg"]`) produces this set:

```sql
-- CTE list
WITH s1 AS
(
  -- s1: $lg
  SELECT DISTINCT
  structure.document_id,
  structure.start_position AS p1,
  structure.end_position AS p2,
  's' AS entity_type,
  structure.id AS entity_id
  FROM structure
  WHERE
  EXISTS
  (
    SELECT * FROM structure_attribute sa
    WHERE sa.structure_id=structure.id
    AND LOWER(sa.name)=LOWER('lg')
  )
) -- s1
```

Notice that here the query draws from `structure` (rather than from `occurrence`). So, the results will include 1 row for each matching structure, having two points which define its start (`p1`) and end (`p2`) token positions (both inclusive). In the case of the sample document, the resulting structures will include are all the tokens except for the title, which is outside stanzas.

### 2. Result CTE

Multiple sets are connected with operators which get translated into SQL [set operations]([set operations](https://stackoverflow.com/questions/11542288/how-do-you-union-with-multiple-ctes)), like `INTERSECT`, `UNION`, `EXCEPT`. In the case of location operators (like `BEFORE`, `NEAR`, etc.), the CTEs are nested via `INNER JOIN`'s to subqueries.

The query builder walks the query syntax tree, and emits a CTE for each pair found. During all these steps, the only collected data are document positions (plus entity IDs and types); in the end, the final result from `r` will get sorted, paged, and joined with additional information from other tables.

For instance, say we have the query `[value="pesca"] AND [lemma="pescare"]`: here we have two pairs connected by AND. So, first we have a list of 2 CTEs for these pairs (`pesca` and `pescare`); then, the `r`esult CTE connects both via an `INTERSECT` set operator:

```sql
WITH s1 AS
(
  -- s1: value EQ "pesca"
  SELECT DISTINCT
  occurrence.document_id,
  occurrence.position AS p1,
  occurrence.position AS p2,
  't' AS entity_type,
  occurrence.id AS entity_id
  FROM occurrence
  INNER JOIN token ON occurrence.token_id=token.id
  WHERE
  LOWER(token.value)=LOWER('pesca')
) -- s1
, s2 AS
(
  -- s2: lemma EQ "pescare"
  SELECT DISTINCT
  occurrence.document_id,
  occurrence.position AS p1,
  occurrence.position AS p2,
  't' AS entity_type,
  occurrence.id AS entity_id
  FROM occurrence
  INNER JOIN token ON occurrence.token_id=token.id
  WHERE
  EXISTS
  (
    SELECT * FROM occurrence_attribute oa
    WHERE oa.occurrence_id=occurrence.id
    AND LOWER(oa.name)=LOWER('lemma')
    AND LOWER(oa.value)=LOWER('pescare')
  )
) -- s2
-- result
, r AS
(
SELECT s1.* FROM s1
INTERSECT
SELECT s2.* FROM s2
) -- r
```

>You may have noticed that in the second pair the filtering expression is different. This is because `lemma` is not an intrinsic attribute of tokens, but rather an optionally added metadatum, available when POS tagging has been performed on texts.

### 3. Merger Query

The final merger query is the one which collects all the previously defined sets and merges them with more information joined from other tables, while applying also sorting and paging.

For instance, the previous query about `pesca` and `pescare` can be completed with:

```sql
-- ... see above ...
--merger
SELECT DISTINCT
occurrence.document_id,
occurrence.position,
occurrence.index,
occurrence.length,
entity_type,
entity_id,
token.value,
document.author,
document.title,
document.sort_key
FROM occurrence
INNER JOIN token ON occurrence.token_id=token.id
INNER JOIN document ON occurrence.document_id=document.id
INNER JOIN r ON occurrence.document_id=r.document_id
AND (occurrence.position=r.p1 OR occurrence.position=r.p2)
ORDER BY document.sort_key, occurrence.position
LIMIT 20 OFFSET 0
```

Here we join the results with more details from documents and token's occurrences, and apply sorting and paging. Joining with occurrences is motivated by the fact that in the end positions, whatever object they refer to, always refer to tokens (a structure like a sentence has a start-token position and an end-token position).

## Examples

Please refer to these pages for some concrete examples of query translations:

- [query examples without location](sql-ex-non-locop.md)
- [query examples with location](sql-ex-locop.md)
