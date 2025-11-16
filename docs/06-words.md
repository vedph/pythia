# Word Index

Optionally, the database can include a superset of calculated data essentially related to word forms and their base form (lemma). The word index is built on top of spans data.

Spans are used as the base for building a list of **words**, representing all the unique combinations of each token's language, value, part of speech, and lemma. Each word also has its pre-calculated total count of the corresponding tokens.

Provided that your indexer uses some kind of lemmatizer, words are the base for building a list of **lemmata**, representing all the word forms belonging to the same base form (lemma). Each lemma also has its pre-calculated total count of word forms.

Both words and lemmata have a pre-calculated detailed distribution across documents, as grouped by each of the document's attribute's unique name=value pair.

## Building

Word data is calculated as follows:

- for each group of tokens as defined by combining into a unique set language, value, POS and lemma, store the group as a word.

Words are extracted from all the documents, so this operation must be executed once the documents indexing process has completed.

### Manual Build

If your database is not too large, you can do this manually by executing the following queries:

1. clear table:

    ```sql
    DELETE FROM word;
    ```

    >If starting fresh, you might want to reset the PK autonumber after clearing the table: `ALTER SEQUENCE word_id_seq RESTART WITH 1;`.

2. fill from tokens:

    ```sql
    INSERT INTO word (language, value, reversed_value, pos, lemma, count)
    SELECT 
        language, 
        value, 
        reverse(value) as reversed_value, 
        pos,
        lemma,
        COUNT(id) as "count"
    FROM span
    WHERE type = 'tok'
    GROUP BY language, value, pos, lemma
    ORDER BY language, value, pos, lemma;
    ```

💡 Should you need to get the links between words and their tokens, you might want to add a word_span table and fill it like this:

```sql
-- word_span
CREATE TABLE word_span (
 word_id int4 NOT NULL,
 span_id int4 NOT NULL,
 CONSTRAINT word_span_pk PRIMARY KEY (word_id, span_id)
);
-- word_span foreign keys
ALTER TABLE word_span ADD CONSTRAINT word_span_fk FOREIGN KEY (word_id) REFERENCES word(id) ON DELETE CASCADE ON UPDATE CASCADE;
ALTER TABLE word_span ADD CONSTRAINT word_span_fk1 FOREIGN KEY (span_id) REFERENCES span(id) ON DELETE CASCADE ON UPDATE CASCADE;

-- filling table
INSERT INTO word_span (word_id, span_id)
SELECT DISTINCT w.id AS word_id, s.id AS span_id
FROM word w
JOIN span s ON w.value = s.value
    AND (w.language IS NOT DISTINCT FROM s.language)
    AND w.pos = s.pos
    AND COALESCE(w.lemma, '') = COALESCE(s.lemma, '')
WHERE s.type = 'tok';
```

Once we have words, lemmata data is calculated as follows:

1. group word forms by language and lemma, while summing their counts; each group is a lemma:

    ```sql
    INSERT INTO lemma (language, value, reversed_value, count)
    SELECT 
        w.language,
        w.lemma AS value,
        reverse(w.lemma) AS reversed_value,
        SUM(w.count) AS count
    FROM word w
    WHERE w.lemma IS NOT NULL
    GROUP BY w.language, w.lemma
    ORDER BY w.lemma;
    ```

2. once we have inserted lemmata, fill `word.lemma_id`:

    ```sql
    UPDATE word w
    SET lemma_id = l.id
    FROM lemma l
    WHERE COALESCE (w.language,'') = COALESCE(l.language, '')
    AND w.lemma = l.value
    AND w.lemma IS NOT NULL;
    ```

Lemma index is built from the word index, by grouping word index rows based on their `lemma_id`.

### Software Build

In case of bigger databases, or whenever you want to avoid manual updates, a code-based process is used, which adopts data paging and client-side processing.

## Usage

Typically, word and lemmata are used to browse the index by focusing on single word forms or words.

For instance, a UI might provide a (paged) list of lemmata. The user might be able to expand any of these lemmata to see all the word forms referred to it. Alternatively, when there are no lemmata, the UI would directly provide a paged list of words.

In both cases, a user might have a quick glance at the distribution of each lemma or word with these steps:

(1) pick one or more document attributes to see the distribution of the selected lemma/word among all the values of each picked attribute. When picking numeric attributes, user can specify the desired number of "bins", so that all the numeric values are distributed in a set of contiguous ranges.

For instance, in a literary corpus one might pick attribute `genre` to see the distribution of a specific word among its values, like "epigrams", "rhetoric", "epic", etc.; or pick attribute `year` with 3 bins to see the distribution of all the works in different years. With 3 bins, the engine would find the minimum and maximum value for `year`, and then define 3 equal ranges to use as bins.

(2) for each picked attribute, display a pie chart with the frequency of the selected lemma/word for each value or (values bin) of the attribute.

## Creation Process

The word and lemma index is created as follows:

1. first, the index is cleared, because it needs to be globally computed on the whole dataset.

2. words are inserted grouping tokens by language, value, POS, and lemma. This means that we define as the same word form all the tokens having these properties equal. The word's count is the count of all the tokens belonging to it. Once words are inserted, their identifiers are updated in the corresponding spans.

3. lemmata are inserted grouping tokens by language and lemma, provided that there is one. The lemma has been assigned to tokens by a POS tagger. Thus each unique combination of language and lemma in a token is a lemma. The lemma's count is the sum of the count of all the words belonging to it. Once lemmata are inserted, their identifiers are updated in the corresponding words.

Their counts index is created as follows:

1. a list of all the combinations of name=value pairs in document attributes (both privileged and non privileged) is calculated from the database. Those attributes marked as numeric are grouped into bins corresponding to the ranges calculated from their minium and maximum values, split in a preset number of classes.

2. for each word, go through all the collected pairs and calculate the count of its spans in each of the document attribute's name=value pair.

3. the lemmata counts are just the sum of the words counts for each lemma.

## Examples

- [example words](example-words.md)
- [example counts](example-counts.md)
