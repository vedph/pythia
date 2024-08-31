# Words and Lemmata Counts

Counts details are calculated for the distribution of each word or lemma in the groups defined by pairs of document attributes names and values.

Tables `word_count` and `lemma_count`, provide the counts of words and lemmata distributed across various document groups. For each word or lemma, the document groups are defined by collecting all the combination of selected document attribute names with their values. For each of these combinations, the total count of words or lemmata in that group is calculated.

For instance, say we look at these words as extracted from token spans:

- `dicebat`
- `dicere`
- `dixerat`
- `et`

The first 3 words correspond to a single lemma, `dīco`, and the fourth one to another lemma, `et`.

## Words Counts

For example, this query targets the word forms of `dīco`:

```sql
select word.value as w, lemma.value as l, doc_attr_name, doc_attr_value, word_count.count
from word_count
inner join word on word_count.word_id = word.id
inner join lemma on word_count.lemma_id = lemma.id
where lemma.value='dīco'
order by l, w, doc_attr_name, doc_attr_value;
```

The words are 3, all found in Catullus:

- `dicebat` (Cat.1)
- `dicere` (Cat.2)
- `dixerat` (twice: Cat.4, Cat.6)

| w       | l    | doc_attr_name | doc_attr_value | count |
| ------- | ---- | ------------- | -------------- | ----- |
| dicebat | dico | author        | Catullus       | 1     |
| dicebat | dico | author        | Horatius       | 0     |
| dicebat | dico | category      | poetry         | 1     |
| dicebat | dico | date_value    | -38,00:-30,00  | 0     |
| dicebat | dico | date_value    | -46,00:-38,00  | 0     |
| dicebat | dico | date_value    | -54,00:-46,00  | 1     |
| dicere  | dico | author        | Catullus       | 1     |
| dicere  | dico | author        | Horatius       | 0     |
| dicere  | dico | category      | poetry         | 1     |
| dicere  | dico | date_value    | -38,00:-30,00  | 0     |
| dicere  | dico | date_value    | -46,00:-38,00  | 0     |
| dicere  | dico | date_value    | -54,00:-46,00  | 1     |
| dixerat | dico | author        | Catullus       | 2     |
| dixerat | dico | author        | Horatius       | 0     |
| dixerat | dico | category      | poetry         | 2     |
| dixerat | dico | date_value    | -38,00:-30,00  | 0     |
| dixerat | dico | date_value    | -46,00:-38,00  | 0     |
| dixerat | dico | date_value    | -54,00:-46,00  | 2     |

For each form the pairs and their counts are:

- `author`:
  - `Catullus`: always 1 occurrence per form of `dīco`, except 2 for `dixerat` which occurs twice in Catullus.
  - `Horatius`: always 0. There is no such word form in Horatius.
- `category`:
  - `poetry`: always 1 except 2 for `dixerat`. Both Catullus and Horatius belong to the poetry category, but only Catullus has forms of `dīco`.
- `date_value`: this is a numeric attribute, so in most cases grouping by its exact value would lend to too much groups and provide confusing results. Rather, when dealing with numeric attributes we calculate the minimum and maximum value for that attribute, and then divide it by a preset number of equal ranges (bins). Then, each value is distributed in one of these bins, and the counts refer to them. Here the bins are:
  - `-54:-46`
  - `-46:-38`
  - `-38:-30`

The two documents are dated -54 (Catullus) and -30 (Horatius carmina liber I). When indexing words we requested 3 "bins" for numeric values (option `-c date_value=3`), so here the value pairs are calculated as follows:

1. min=-54, max=-30.
2. size=|54-30 = 24/3 = 8

We thus have bins -54:-46, -46:-38, -38:-30.
