# Words

The list of words is built when lemmatization data is available. In our case, the Latin POS tagger provides lemma and POS for each token. Words are built by grouping all the tokens by each unique combination of language (not used in our example), value, lemma, and POS. The total count is the count of all the matching tokens.

> Of course, depending on tagging errors, some words can be wrong, like an "Arrius" token misinterpreted as an adverb, "hiemes", "brevum", etc. At any rate, this being a standard database we can fix issues later if this is feasible, and in this example we just ignore errors focusing on model and engine capabilities.

```sql
select id, value, pos, lemma, count
from word
order by lemma, value, pos;
```

| id  | value      | pos   | lemma      | count |
| --- | ---------- | ----- | ---------- | ----- |
| 1   | ad         | ADP   | ad         | 1     |
| 2   | aetas      | NOUN  | aetas      | 1     |
| 3   | affertur   | VERB  | affero     | 1     |
| 4   | arrium     | NOUN  | arrium     | 1     |
| 5   | arrius     | ADV   | arrius     | 1     |
| 6   | arrius     | NOUN  | arrius     | 1     |
| 7   | atque      | CCONJ | atque      | 1     |
| 8   | audibant   | VERB  | audibo     | 1     |
| 9   | aures      | NOUN  | auris      | 1     |
| 10  | avia       | NOUN  | avia       | 1     |
| 11  | avunculus  | NOUN  | avunculus  | 1     |
| 12  | avus       | NOUN  | avus       | 1     |
| 13  | babylonios | NOUN  | babylonius | 1     |
| 14  | bibes      | VERB  | bibo       | 1     |
| 88  | melius     | ADJ   | bonus      | 1     |
| 15  | brevi      | ADJ   | brevum     | 1     |
| 16  | caecubum   | NOUN  | caecubus   | 1     |
| 17  | caleno     | ADJ   | caleno     | 1     |
| 18  | cantharis  | NOUN  | cantharis  | 1     |
| 19  | carpe      | NOUN  | caro       | 1     |
| 20  | chommoda   | NOUN  | chommodus  | 1     |
| 21  | clare      | ADJ   | claris     | 1     |
| 22  | colles     | NOUN  | collis     | 1     |
| 23  | commoda    | ADJ   | commodus   | 1     |
| 24  | conditum   | VERB  | condo      | 1     |
| 25  | credo      | VERB  | credo      | 1     |
| 26  | credula    | NOUN  | credula    | 1     |
| 27  | cum        | ADP   | cum        | 1     |
| 28  | cum        | SCONJ | cum        | 2     |
| 30  | debilitat  | VERB  | debilito   | 1     |
| 33  | dicebat    | VERB  | dico       | 1     |
| 34  | dicere     | VERB  | dico       | 1     |
| 36  | dixerat    | VERB  | dico       | 2     |
| 35  | diem       | NOUN  | dies       | 1     |
| 29  | datus      | VERB  | do         | 1     |
| 31  | dederint   | VERB  | do         | 1     |
| 32  | di         | VERB  | do         | 1     |
| 37  | domitam    | VERB  | domo       | 1     |
| 38  | dum        | SCONJ | dum        | 1     |
| 40  | ego        | PRON  | ego        | 1     |
| 90  | mihi       | PRON  | ego        | 1     |
| 42  | eques      | AUX   | eques      | 1     |
| 45  | et         | CCONJ | et         | 6     |
| 51  | fugerit    | VERB  | facio      | 1     |
| 46  | falernae   | ADJ   | falernus   | 1     |
| 47  | finem      | NOUN  | finis      | 1     |
| 48  | fluctus    | VERB  | fluctus    | 1     |
| 49  | fluminis   | NOUN  | flumen     | 1     |
| 50  | formiani   | NOUN  | formianus  | 1     |
| 52  | graeca     | NOUN  | graecus    | 1     |
| 53  | haec       | DET   | hic        | 1     |
| 58  | hoc        | DET   | hic        | 1     |
| 54  | hiemes     | NOUN  | hiemes     | 1     |
| 55  | hinsidias  | NOUN  | hinsidia   | 1     |
| 56  | hinsidias  | NOUN  | hinsidius  | 1     |
| 57  | hionios    | ADJ   | hionius    | 1     |
| 59  | horribilis | ADJ   | horribilis | 1     |
| 60  | i          | NUM   | i          | 1     |
| 61  | iam        | ADV   | iam        | 1     |
| 39  | eadem      | DET   | idem       | 1     |
| 62  | illuc      | ADV   | illuc      | 1     |
| 63  | imago      | NOUN  | imago      | 1     |
| 64  | in         | ADP   | in         | 2     |
| 65  | insidias   | NOUN  | insidius   | 1     |
| 66  | invida     | ADJ   | invidus    | 1     |
| 67  | iocosa     | ADJ   | iocosus    | 1     |
| 68  | ionios     | ADJ   | ionius     | 1     |
| 69  | ionios     | NOUN  | ionius     | 1     |
| 70  | ipse       | DET   | ipse       | 1     |
| 41  | eius       | PRON  | is         | 1     |
| 71  | isset      | VERB  | issum      | 1     |
| 72  | iuppiter   | ADV   | iuppiter   | 1     |
| 73  | laudes     | NOUN  | laus       | 1     |
| 74  | leniter    | ADV   | leniter    | 1     |
| 75  | leuconoe   | NOUN  | leuconoe   | 1     |
| 77  | leviter    | ADV   | leviter    | 1     |
| 76  | levi       | NOUN  | levum      | 1     |
| 78  | liber      | ADJ   | liber      | 1     |
| 79  | liques     | NOUN  | liques     | 1     |
| 81  | longam     | ADJ   | longus     | 1     |
| 80  | locutum    | VERB  | loquor     | 1     |
| 82  | loquimur   | VERB  | loquor     | 1     |
| 83  | maecenas   | NOUN  | maecena    | 1     |
| 84  | mare       | NOUN  | mare       | 1     |
| 85  | mater      | NOUN  | mater      | 1     |
| 86  | maternus   | ADJ   | maternus   | 1     |
| 89  | metuebant  | VERB  | metuo      | 1     |
| 87  | mea        | DET   | meus       | 1     |
| 92  | mirifice   | ADV   | mirifice   | 1     |
| 93  | misso      | VERB  | mitto      | 1     |
| 94  | modicis    | ADJ   | modicus    | 1     |
| 95  | montis     | NOUN  | mons       | 1     |
| 96  | ne         | SCONJ | ne         | 1     |
| 97  | nec        | CCONJ | nec        | 3     |
| 98  | nefas      | NOUN  | nefas      | 1     |
| 99  | neque      | CCONJ | neque      | 1     |
| 100 | non        | PART  | non        | 1     |
| 101 | numeros    | NOUN  | numerus    | 1     |
| 102 | nunc       | ADV   | nunc       | 1     |
| 103 | nuntius    | ADV   | nunte      | 1     |
| 104 | omnibus    | DET   | omnis      | 1     |
| 105 | oppositis  | VERB  | oppono     | 1     |
| 91  | minimum    | NOUN  | paruus     | 1     |
| 106 | paterni    | ADJ   | paternus   | 1     |
| 107 | pati       | VERB  | patior     | 1     |
| 108 | plausus    | VERB  | plausus    | 1     |
| 109 | pluris     | DET   | pluris     | 1     |
| 110 | pocula     | NOUN  | poculus    | 1     |
| 115 | poterat    | VERB  | possum     | 1     |
| 111 | postero    | NOUN  | posterus   | 1     |
| 112 | postilla   | NOUN  | postilla   | 1     |
| 113 | postquam   | SCONJ | postquam   | 1     |
| 114 | potabis    | VERB  | poto       | 1     |
| 116 | prelo      | NOUN  | prelo      | 1     |
| 117 | pumicibus  | NOUN  | pumic      | 1     |
| 119 | quaesieris | VERB  | quaeso     | 1     |
| 120 | quam       | SCONJ | quam       | 1     |
| 121 | quando     | SCONJ | quando     | 1     |
| 122 | quantum    | ADV   | quantum    | 1     |
| 118 | quae       | PRON  | qui        | 1     |
| 123 | quem       | PRON  | qui        | 2     |
| 124 | quidquid   | PRON  | quisquis   | 1     |
| 125 | quod       | SCONJ | quod       | 1     |
| 126 | redderet   | VERB  | reddo      | 1     |
| 127 | requierant | VERB  | requiero   | 1     |
| 128 | reseces    | NOUN  | resex      | 1     |
| 129 | ripae      | NOUN  | ripa       | 1     |
| 130 | sabinum    | NOUN  | sabinus    | 1     |
| 131 | sapias     | NOUN  | sapius     | 1     |
| 132 | scire      | VERB  | scio       | 1     |
| 133 | se         | PRON  | se         | 1     |
| 137 | sibi       | PRON  | se         | 1     |
| 134 | sed        | CCONJ | sed        | 1     |
| 136 | si         | SCONJ | si         | 1     |
| 138 | sic        | ADV   | sic        | 3     |
| 139 | simul      | ADV   | simul      | 1     |
| 135 | seu        | CCONJ | siue       | 2     |
| 140 | spatio     | NOUN  | spatium    | 1     |
| 142 | sperabat   | VERB  | spero      | 1     |
| 141 | spem       | NOUN  | spes       | 1     |
| 143 | subito     | ADV   | subito     | 1     |
| 43  | erit       | AUX   | sum        | 1     |
| 44  | esse       | AUX   | sum        | 2     |
| 144 | syriam     | NOUN  | syrius     | 1     |
| 145 | talia      | DET   | talis      | 1     |
| 146 | temperant  | VERB  | tempero    | 1     |
| 147 | temptaris  | ADJ   | temptaris  | 1     |
| 148 | testa      | NOUN  | testa      | 1     |
| 149 | theatro    | NOUN  | theater    | 1     |
| 151 | tribuit    | VERB  | tribuo     | 1     |
| 150 | tibi       | PRON  | tu         | 3     |
| 152 | tu         | PRON  | tu         | 2     |
| 153 | tum        | ADV   | tum        | 1     |
| 154 | tyrrhenum  | NOUN  | tyrrhes    | 1     |
| 155 | ultimam    | ADJ   | ultimus    | 1     |
| 156 | ut         | SCONJ | ut         | 2     |
| 157 | uvam       | NOUN  | uva        | 1     |
| 158 | vaticani   | ADJ   | vaticanus  | 1     |
| 160 | verba      | NOUN  | verba      | 1     |
| 161 | vile       | NOUN  | vilis      | 1     |
| 162 | vina       | NOUN  | vina       | 1     |
| 163 | vites      | NOUN  | vis        | 1     |
| 159 | vellet     | VERB  | volo       | 1     |

Texts here are so short that most words have a single attestation, except for high frequency, often appositive words like "et", "nec", etc.

## Lemmata

Lemmata are extracted from words grouping them by their language and lemma. So, each unique combination of language and lemma is a lemma, and its count is equal to the sum of all the words belonging to it. For instance, lemma "dico" is deduced from tokens "dicebat", "dicere", and "dixerat" (twice), whence a total of 4 occurrences.

```sql
select id, value, count
from lemma
order by value;
```

| id  | value      | count |
| --- | ---------- | ----- |
| 1   | ad         | 1     |
| 2   | aetas      | 1     |
| 3   | affero     | 1     |
| 4   | arrium     | 1     |
| 5   | arrius     | 2     |
| 6   | atque      | 1     |
| 7   | audibo     | 1     |
| 8   | auris      | 1     |
| 9   | avia       | 1     |
| 10  | avunculus  | 1     |
| 11  | avus       | 1     |
| 12  | babylonius | 1     |
| 13  | bibo       | 1     |
| 14  | bonus      | 1     |
| 15  | brevum     | 1     |
| 16  | caecubus   | 1     |
| 17  | caleno     | 1     |
| 18  | cantharis  | 1     |
| 19  | caro       | 1     |
| 20  | chommodus  | 1     |
| 21  | claris     | 1     |
| 22  | collis     | 1     |
| 23  | commodus   | 1     |
| 24  | condo      | 1     |
| 25  | credo      | 1     |
| 26  | credula    | 1     |
| 27  | cum        | 3     |
| 28  | debilito   | 1     |
| 29  | dico       | 4     |
| 30  | dies       | 1     |
| 31  | do         | 3     |
| 32  | domo       | 1     |
| 33  | dum        | 1     |
| 34  | ego        | 2     |
| 35  | eques      | 1     |
| 36  | et         | 6     |
| 37  | facio      | 1     |
| 38  | falernus   | 1     |
| 39  | finis      | 1     |
| 40  | fluctus    | 1     |
| 41  | flumen     | 1     |
| 42  | formianus  | 1     |
| 43  | graecus    | 1     |
| 44  | hic        | 2     |
| 45  | hiemes     | 1     |
| 46  | hinsidia   | 1     |
| 47  | hinsidius  | 1     |
| 48  | hionius    | 1     |
| 49  | horribilis | 1     |
| 50  | i          | 1     |
| 51  | iam        | 1     |
| 52  | idem       | 1     |
| 53  | illuc      | 1     |
| 54  | imago      | 1     |
| 55  | in         | 2     |
| 56  | insidius   | 1     |
| 57  | invidus    | 1     |
| 58  | iocosus    | 1     |
| 59  | ionius     | 2     |
| 60  | ipse       | 1     |
| 61  | is         | 1     |
| 62  | issum      | 1     |
| 63  | iuppiter   | 1     |
| 64  | laus       | 1     |
| 65  | leniter    | 1     |
| 66  | leuconoe   | 1     |
| 67  | leviter    | 1     |
| 68  | levum      | 1     |
| 69  | liber      | 1     |
| 70  | liques     | 1     |
| 71  | longus     | 1     |
| 72  | loquor     | 2     |
| 73  | maecena    | 1     |
| 74  | mare       | 1     |
| 75  | mater      | 1     |
| 76  | maternus   | 1     |
| 77  | metuo      | 1     |
| 78  | meus       | 1     |
| 79  | mirifice   | 1     |
| 80  | mitto      | 1     |
| 81  | modicus    | 1     |
| 82  | mons       | 1     |
| 83  | ne         | 1     |
| 84  | nec        | 3     |
| 85  | nefas      | 1     |
| 86  | neque      | 1     |
| 87  | non        | 1     |
| 88  | numerus    | 1     |
| 89  | nunc       | 1     |
| 90  | nunte      | 1     |
| 91  | omnis      | 1     |
| 92  | oppono     | 1     |
| 93  | paruus     | 1     |
| 94  | paternus   | 1     |
| 95  | patior     | 1     |
| 96  | plausus    | 1     |
| 97  | pluris     | 1     |
| 98  | poculus    | 1     |
| 99  | possum     | 1     |
| 100 | posterus   | 1     |
| 101 | postilla   | 1     |
| 102 | postquam   | 1     |
| 103 | poto       | 1     |
| 104 | prelo      | 1     |
| 105 | pumic      | 1     |
| 106 | quaeso     | 1     |
| 107 | quam       | 1     |
| 108 | quando     | 1     |
| 109 | quantum    | 1     |
| 110 | qui        | 3     |
| 111 | quisquis   | 1     |
| 112 | quod       | 1     |
| 113 | reddo      | 1     |
| 114 | requiero   | 1     |
| 115 | resex      | 1     |
| 116 | ripa       | 1     |
| 117 | sabinus    | 1     |
| 118 | sapius     | 1     |
| 119 | scio       | 1     |
| 120 | se         | 2     |
| 121 | sed        | 1     |
| 122 | si         | 1     |
| 123 | sic        | 3     |
| 124 | simul      | 1     |
| 125 | siue       | 2     |
| 126 | spatium    | 1     |
| 127 | spero      | 1     |
| 128 | spes       | 1     |
| 129 | subito     | 1     |
| 130 | sum        | 3     |
| 131 | syrius     | 1     |
| 132 | talis      | 1     |
| 133 | tempero    | 1     |
| 134 | temptaris  | 1     |
| 135 | testa      | 1     |
| 136 | theater    | 1     |
| 137 | tribuo     | 1     |
| 138 | tu         | 5     |
| 139 | tum        | 1     |
| 140 | tyrrhes    | 1     |
| 141 | ultimus    | 1     |
| 142 | ut         | 2     |
| 143 | uva        | 1     |
| 144 | vaticanus  | 1     |
| 145 | verba      | 1     |
| 146 | vilis      | 1     |
| 147 | vina       | 1     |
| 148 | vis        | 1     |
| 149 | volo       | 1     |
