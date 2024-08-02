# Word Index

Optionally, the database can include a superset of calculated data essentially related to word forms and their base form (lemma). The word index is built on top of spans data (see [storage](storage.md) for more details).

Spans are used as the base for building a list of **words**, representing all the unique combinations of each token's language, value, part of speech, and lemma. Each word also has its pre-calculated total count of the corresponding tokens.

Provided that your indexer uses some kind of lemmatizer, words are the base for building a list of **lemmata**, representing all the word forms belonging to the same base form (lemma). Each lemma also has its pre-calculated total count of word forms.

Both words and lemmata have a pre-calculated detailed distribution across documents, as grouped by each of the document's attribute's unique name=value pair.

## Usage Story

Typically, word and lemmata are used to browse the index by focusing on single word forms or words.

For instance, a UI might provide a (paged) list of lemmata. The user might be able to expand any of these lemmata to see all the word forms referred to it. Alternatively, when there are no lemmata, the UI would directly provide a paged list of words.

In both cases, a user might have a quick glance at the distribution of each lemma or word with these steps:

(1) pick one or more document attributes to see the distribution of the selected lemma/word among all the values of each picked attribute. When picking numeric attributes, user can specify the desired number of "bins", so that all the numeric values are distributed in a set of contiguous ranges.

For instance, in a literary corpus one might pick attribute `genre` to see the distribution of a specific word among its values, like "epigrams", "rhetoric", "epic", etc.; or pick attribute `year` with 3 bins to see the distribution of all the works in different years. With 3 bins, the engine would find the minimum and maximum value for `year`, and then define 3 equal ranges to use as bins.

(2) for each picked attribute, display a pie chart with the frequency of the selected lemma/word for each value or (values bin) of the attribute.
