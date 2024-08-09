# Example Documents

|          | token | structs |
| -------- | ----- | ------- |
| all      | 183   | 56      |
| Catullus | 74    | 23      |
| Horatius | 109   | 33      |

## Catullus

The unique `div` inside the TEI text element is reported below:

```xml
<div type="poem" n="84">
    <head>ad Arrium</head>
    <lg met="eleg" n="1">
        <l met="6da^" n="1"><quote>chommoda</quote> dicebat, si quando commoda vellet</l>
        <l met="pent" n="2">dicere, et insidias <persName>Arrius</persName> <quote>hinsidias</quote>,</l>
    </lg>
    <lg met="eleg" n="2">
        <l met="6da^" n="3">et tum mirifice sperabat se esse locutum,</l>
        <l met="pent" n="4">cum quantum poterat dixerat <quote>hinsidias</quote>.</l>
    </lg>
    <lg met="eleg" n="3">
        <l met="6da^" n="5">credo, sic mater, sic liber avunculus eius</l>
        <l met="pent" n="6">sic maternus avus dixerat atque avia.</l>
    </lg>
    <lg met="eleg" n="4">
        <l met="6da^" n="7">hoc misso in <geogName>Syriam</geogName> requierant omnibus
            aures:</l>
        <l met="pent" n="8">audibant eadem haec leniter et leviter,</l>
    </lg>
    <lg met="eleg" n="5">
        <l met="6da^" n="9">nec sibi postilla metuebant talia verba,</l>
        <l met="pent" n="10">cum subito affertur nuntius horribilis,</l>
    </lg>
    <lg met="eleg" n="6">
        <l met="6da^" n="11"><geogName>Ionios</geogName> fluctus, postquam illuc <persName>Arrius</persName> isset,</l>
        <l met="pent" n="12">iam non <geogName>Ionios</geogName> esse sed <quote><geogName>Hionios</geogName></quote>.</l>
    </lg>
</div>
```

In the following text we represent just the tokens prefixed by their ordinal:

```txt
1ad 2Arrium

3chommoda 4dicebat, 5si 6quando 7commoda 8vellet
9dicere, 10et 11insidias 12Arrius 13hinsidias,

14et 15tum 16mirifice 17sperabat 18se 19esse 20locutum,
21cum 22quantum 23poterat 24dixerat 25hinsidias.

26credo, 27sic 28mater, 29sic 30liber 31avunculus 32eius
33sic 34maternus 35avus 36dixerat 37atque 38avia.

39hoc 40misso 41in 42Syriam 43requierant 44omnibus 45aures:
46audibant 47eadem 48haec 49leniter 50et 51leviter,

52nec 53sibi 54postilla 55metuebant 56talia 57verba,
58cum 59subito 60affertur 61nuntius 62horribilis,

63Ionios 64fluctus, 65postquam 66illuc 67Arrius 68isset,
69iam 70non 71Ionios 72esse 73sed 74Hionios.
```

So here we have (numbers refer to tokens ordinals):

- 1 poem (the `div`; 1-74).
- 6 strophes (`lg`): 3-13, 14-25, 26-38, 39-51, 52-62, 63-74.
- 12 verses (`l`): 3-8, 9-13, 14-20, 21-25, 26-32, 33-38, 39-45, 46-51, 52-57, 58-62, 63-68, 69-74.
- 74 tokens.
- 4 sentences:
  - "ad Arrium" (1-2).
  - vv.1-4 "chommoda... hinsidias": 3-25.
  - vv.5-6 "credo... avia": 26-38.
  - vv.7-12 "hoc misso... Hionios": 39-74.

The only text outside metrical structures is the title in `head`.

### Tokens

```sql
select id, p1, value, lemma, pos,
    index, length,
    lemma_id, word_id
from span
where type='tok' and document_id=1
order by p1;
```

| id  | p1  | value      | lemma      | pos   | index | length | lemma_id | word_id |
| --- | --- | ---------- | ---------- | ----- | ----- | ------ | -------- | ------- |
| 1   | 1   | ad         | ad         | ADP   | 1019  | 2      | 1        | 1       |
| 2   | 2   | arrium     | arrium     | NOUN  | 1022  | 6      | 4        | 4       |
| 3   | 3   | chommoda   | chommodus  | NOUN  | 1123  | 8      | 20       | 20      |
| 4   | 4   | dicebat    | dico       | VERB  | 1140  | 8      | 29       | 33      |
| 5   | 5   | si         | si         | SCONJ | 1149  | 2      | 122      | 136     |
| 6   | 6   | quando     | quando     | SCONJ | 1152  | 6      | 108      | 121     |
| 7   | 7   | commoda    | commodus   | ADJ   | 1159  | 7      | 23       | 23      |
| 8   | 8   | vellet     | volo       | VERB  | 1167  | 6      | 149      | 159     |
| 9   | 9   | dicere     | dico       | VERB  | 1219  | 7      | 29       | 34      |
| 10  | 10  | et         | et         | CCONJ | 1227  | 2      | 36       | 45      |
| 11  | 11  | insidias   | insidius   | NOUN  | 1230  | 8      | 56       | 65      |
| 12  | 12  | arrius     | arrius     | NOUN  | 1249  | 6      | 5        | 6       |
| 13  | 13  | hinsidias  | hinsidius  | NOUN  | 1274  | 9      | 47       | 56      |
| 14  | 14  | et         | et         | CCONJ | 1400  | 2      | 36       | 45      |
| 15  | 15  | tum        | tum        | ADV   | 1403  | 3      | 139      | 153     |
| 16  | 16  | mirifice   | mirifice   | ADV   | 1407  | 8      | 79       | 92      |
| 17  | 17  | sperabat   | spero      | VERB  | 1416  | 8      | 127      | 142     |
| 18  | 18  | se         | se         | PRON  | 1425  | 2      | 120      | 133     |
| 19  | 19  | esse       | sum        | AUX   | 1428  | 4      | 130      | 44      |
| 20  | 20  | locutum    | loquor     | VERB  | 1433  | 8      | 72       | 80      |
| 21  | 21  | cum        | cum        | SCONJ | 1487  | 3      | 27       | 28      |
| 22  | 22  | quantum    | quantum    | ADV   | 1491  | 7      | 109      | 122     |
| 23  | 23  | poterat    | possum     | VERB  | 1499  | 7      | 99       | 115     |
| 24  | 24  | dixerat    | dico       | VERB  | 1507  | 7      | 29       | 36      |
| 25  | 25  | hinsidias  | hinsidia   | NOUN  | 1522  | 9      | 46       | 55      |
| 26  | 26  | credo      | credo      | VERB  | 1648  | 6      | 25       | 25      |
| 27  | 27  | sic        | sic        | ADV   | 1655  | 3      | 123      | 138     |
| 28  | 28  | mater      | mater      | NOUN  | 1659  | 6      | 75       | 85      |
| 29  | 29  | sic        | sic        | ADV   | 1666  | 3      | 123      | 138     |
| 30  | 30  | liber      | liber      | ADJ   | 1670  | 5      | 69       | 78      |
| 31  | 31  | avunculus  | avunculus  | NOUN  | 1676  | 9      | 10       | 11      |
| 32  | 32  | eius       | is         | PRON  | 1686  | 4      | 61       | 41      |
| 33  | 33  | sic        | sic        | ADV   | 1736  | 3      | 123      | 138     |
| 34  | 34  | maternus   | maternus   | ADJ   | 1740  | 8      | 76       | 86      |
| 35  | 35  | avus       | avus       | NOUN  | 1749  | 4      | 11       | 12      |
| 36  | 36  | dixerat    | dico       | VERB  | 1754  | 7      | 29       | 36      |
| 37  | 37  | atque      | atque      | CCONJ | 1762  | 5      | 6        | 7       |
| 38  | 38  | avia       | avia       | NOUN  | 1768  | 5      | 9        | 10      |
| 39  | 39  | hoc        | hic        | DET   | 1881  | 3      | 44       | 58      |
| 40  | 40  | misso      | mitto      | VERB  | 1885  | 5      | 80       | 93      |
| 41  | 41  | in         | in         | ADP   | 1891  | 2      | 55       | 64      |
| 42  | 42  | syriam     | syrius     | NOUN  | 1904  | 6      | 131      | 144     |
| 43  | 43  | requierant | requiero   | VERB  | 1922  | 10     | 114      | 127     |
| 44  | 44  | omnibus    | omnis      | DET   | 1933  | 7      | 91       | 104     |
| 45  | 45  | aures      | auris      | NOUN  | 1966  | 5      | 8        | 9       |
| 46  | 46  | audibant   | audibo     | VERB  | 2017  | 8      | 7        | 8       |
| 47  | 47  | eadem      | idem       | DET   | 2026  | 5      | 52       | 39      |
| 48  | 48  | haec       | hic        | DET   | 2032  | 4      | 44       | 53      |
| 49  | 49  | leniter    | leniter    | ADV   | 2037  | 7      | 65       | 74      |
| 50  | 50  | et         | et         | CCONJ | 2045  | 2      | 36       | 45      |
| 51  | 51  | leviter    | leviter    | ADV   | 2048  | 8      | 67       | 77      |
| 52  | 52  | nec        | nec        | CCONJ | 2164  | 3      | 84       | 97      |
| 53  | 53  | sibi       | se         | PRON  | 2168  | 4      | 120      | 137     |
| 54  | 54  | postilla   | postilla   | NOUN  | 2173  | 8      | 101      | 112     |
| 55  | 55  | metuebant  | metuo      | VERB  | 2182  | 9      | 77       | 89      |
| 56  | 56  | talia      | talis      | DET   | 2192  | 5      | 132      | 145     |
| 57  | 57  | verba      | verba      | NOUN  | 2198  | 6      | 145      | 160     |
| 58  | 58  | cum        | cum        | SCONJ | 2251  | 3      | 27       | 28      |
| 59  | 59  | subito     | subito     | ADV   | 2255  | 6      | 129      | 143     |
| 60  | 60  | affertur   | affero     | VERB  | 2262  | 8      | 3        | 3       |
| 61  | 61  | nuntius    | nunte      | ADV   | 2271  | 7      | 90       | 103     |
| 62  | 62  | horribilis | horribilis | ADJ   | 2279  | 11     | 49       | 59      |
| 63  | 63  | ionios     | ionius     | NOUN  | 2409  | 6      | 59       | 69      |
| 64  | 64  | fluctus    | fluctus    | VERB  | 2427  | 8      | 40       | 48      |
| 65  | 65  | postquam   | postquam   | SCONJ | 2436  | 8      | 102      | 113     |
| 66  | 66  | illuc      | illuc      | ADV   | 2445  | 5      | 53       | 62      |
| 67  | 67  | arrius     | arrius     | ADV   | 2461  | 6      | 5        | 5       |
| 68  | 68  | isset      | issum      | VERB  | 2479  | 6      | 62       | 71      |
| 69  | 69  | iam        | iam        | ADV   | 2532  | 3      | 51       | 61      |
| 70  | 70  | non        | non        | PART  | 2536  | 3      | 87       | 100     |
| 71  | 71  | ionios     | ionius     | ADJ   | 2550  | 6      | 59       | 68      |
| 72  | 72  | esse       | sum        | AUX   | 2568  | 4      | 130      | 44      |
| 73  | 73  | sed        | sed        | CCONJ | 2573  | 3      | 121      | 134     |
| 74  | 74  | hionios    | hionius    | ADJ   | 2594  | 7      | 48       | 57      |

As you can see P1 is the ordinal token position. P2 is always equal to P1 for tokens, so it's not reported here. Lemma, word ID and lemma ID have been added by postprocessing spans. POS is the result of a UDPipe Latin tagger, while index and length are the character-based position of the portion of text corresponding to each token in the source text.

>Some of the POS tagger results are unreliable (`requiero` instead of `requiesco`, `chommoda` and other H-forms -really not existing- vs. `commoda` and its oscillation between noun and adjective, etc.), but in most cases it is correct.

### Structures

```sql
select id, type, p1, p2, text, index, length
from span
where type<>'tok' and document_id=1
order by p1;
```

| id  | type | p1  | p2  | text                                                                                                               | index | length |
| --- | ---- | --- | --- | ------------------------------------------------------------------------------------------------------------------ | ----- | ------ |
| 94  | snt  | 1   | 2   | Lafaye, 1922 ad Arrium                                                                                             | 847   | 181    |
| 75  | div  | 1   | 74  | ad Arrium chommoda dicebat, si quando commoda vellet dicere...illuc Arrius isset, iam non Ionios esse sed Hionios. | 971   | 994    |
| 82  | l    | 3   | 8   | chommoda dicebat, si quando commoda vellet                                                                         | 1096  | 42     |
| 76  | lg   | 3   | 13  | chommoda dicebat, si quando commoda vellet dicere, et insidias Arrius hinsidias,                                   | 1053  | 138    |
| 95  | snt  | 3   | 25  | chommoda dicebat, si quando commoda vellet dicere...se esse locutum, cum quantum poterat dixerat hinsidias .       | 1123  | 417    |
| 83  | l    | 9   | 13  | dicere, et insidias Arrius hinsidias,                                                                              | 1199  | 37     |
| 77  | lg   | 14  | 25  | et tum mirifice sperabat se esse locutum, cum quantum poterat dixerat hinsidias.                                   | 1337  | 138    |
| 84  | l    | 14  | 20  | et tum mirifice sperabat se esse locutum,                                                                          | 1380  | 41     |
| 85  | l    | 21  | 25  | cum quantum poterat dixerat hinsidias.                                                                             | 1467  | 38     |
| 86  | l    | 26  | 32  | credo, sic mater, sic liber avunculus eius                                                                         | 1628  | 42     |
| 78  | lg   | 26  | 38  | credo, sic mater, sic liber avunculus eius sic maternus avus dixerat atque avia.                                   | 1585  | 138    |
| 96  | snt  | 26  | 38  | credo, sic mater, sic liber avunculus eius sic maternus avus dixerat atque avia.                                   | 1648  | 125    |
| 87  | l    | 33  | 38  | sic maternus avus dixerat atque avia.                                                                              | 1716  | 37     |
| 88  | l    | 39  | 45  | hoc misso in Syriam requierant omnibus aures                                                                       | 1861  | 68     |
| 97  | snt  | 39  | 74  | hoc misso in Syriam requierant omnibus aures...illuc Arrius isset, iam non Ionios esse sed Hionios .               | 1881  | 740    |
| 79  | lg   | 39  | 51  | hoc misso in Syriam requierant omnibus aures audibant eadem haec leniter et leviter,                               | 1818  | 166    |
| 89  | l    | 46  | 51  | audibant eadem haec leniter et leviter,                                                                            | 1997  | 39     |
| 80  | lg   | 52  | 62  | nec sibi postilla metuebant talia verba, cum subito affertur nuntius horribilis,                                   | 2101  | 138    |
| 90  | l    | 52  | 57  | nec sibi postilla metuebant talia verba,                                                                           | 2144  | 40     |
| 91  | l    | 58  | 62  | cum subito affertur nuntius horribilis,                                                                            | 2230  | 39     |
| 81  | lg   | 63  | 74  | Ionios fluctus, postquam illuc Arrius isset, iam non Ionios esse sed Hionios.                                      | 2335  | 135    |
| 92  | l    | 63  | 68  | Ionios fluctus, postquam illuc Arrius isset,                                                                       | 2378  | 44     |
| 93  | l    | 69  | 74  | iam non Ionios esse sed Hionios.                                                                                   | 2511  | 32     |

>Note that structures have no value, but they have a text, used as a human-friendly label and consisting in the first and last portions of its source text, or in the full text when it's short enough.

⏮️ [simple example](example.md)
⏭️ [simple example - Catullus](example-dump-1.md)
