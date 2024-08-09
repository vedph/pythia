# Example Documents

|          | token | structs |
|----------|-------|---------|
| all      | 183   | 56      |
| Catullus | 74    | 23      |
| Horatius | 109   | 33      |

## Horatius

This file contains two poems (the second one is cut to keep the example short).

```xml
<div type="poem" n="11">
   <head>I.11</head>
   <l met="asclep" n="1">tu ne quaesieris, scire nefas, quem mihi, quem tibi</l>
   <l met="asclep" n="2">finem di dederint, <persName>Leuconoe</persName>, nec <geogName>Babylonios</geogName></l>
   <l met="asclep" n="3">temptaris numeros. ut melius, quidquid erit, pati.</l>
   <l met="asclep" n="4">seu pluris hiemes seu tribuit <persName>Iuppiter</persName> ultimam,</l>
   <l met="asclep" n="5">quae nunc oppositis debilitat pumicibus mare</l>
   <l met="asclep" n="6"><geogName>Tyrrhenum</geogName>: sapias, vina liques, et spatio brevi</l>
   <l met="asclep" n="7">spem longam reseces. dum loquimur, fugerit invida</l>
   <l met="asclep" n="8">aetas: carpe diem quam minimum credula postero.</l>
</div>
<div type="poem" n="20">
   <head>I.20</head>
   <lg met="sapph">
      <l met="11s" n="1">vile potabis modicis <geogName>Sabinum</geogName></l>
      <l met="11s" n="2">cantharis, <geogName>Graeca</geogName> quod ego ipse testa</l>
      <l met="11s" n="3">conditum levi, datus in theatro</l>
      <l met="adon" n="4">cum tibi plausus,</l>
   </lg>
   <lg met="sapph">
      <l met="11s" n="5">clare <persName>Maecenas</persName> eques, ut paterni</l>
      <l met="11s" n="6">fluminis ripae simul et iocosa</l>
      <l met="11s" n="7">redderet laudes tibi <geogName>Vaticani</geogName></l>
      <l met="adon" n="8">montis imago.</l>
   </lg>
   <lg met="sapph">
      <l met="11s" n="9"><geogName>Caecubum</geogName> et prelo domitam <geogName>Caleno</geogName></l>
      <l met="11s" n="10">tu bibes uvam: mea nec <geogName>Falernae</geogName></l>
      <l met="11s" n="11">temperant vites neque <geogName>Formiani</geogName></l>
      <l met="adon" n="12">pocula colles.</l>
   </lg>
</div>
```

Plain text:

```txt
1I.11

2tu 3ne 4quaesieris, 5scire 6nefas, 7quem 8mihi, 9quem 10tibi
11finem 12di 13dederint, 14Leuconoe, 15nec 16Babylonios
17temptaris 18numeros. 19ut 20melius, 21quidquid 22erit, 23pati.
24seu 25pluris 26hiemes 27seu 28tribuit 29Iuppiter 30ultimam,
31quae 32nunc 33oppositis 34debilitat 35pumicibus 36mare
37Tyrrhenum: 38sapias, 39vina 40liques, 41et 42spatio 43brevi
44spem 45longam 46reseces. 47dum 48loquimur, 49fugerit 50invida
51aetas: 52carpe 53diem 54quam 55minimum 56credula 57postero.

58I.20

59vile 60potabis 61modicis 62Sabinum
63cantharis, 64Graeca 65quod 66ego 67ipse 68testa
69conditum 70levi, 71datus 72in 73theatro
74cum 75tibi 76plausus,

77clare 78Maecenas 79eques, 80ut 81paterni
82fluminis 83ripae 84simul 85et 86iocosa
87redderet 88laudes 89tibi 90Vaticani
91montis 92imago.

93Caecubum 94et 95prelo 96domitam 97Caleno
98tu 99bibes 100uvam: 101mea 102nec 103Falernae
104temperant 105vites 106neque 107Formiani
108pocula 109colles.
```

Here we have:

- 2 poems: 1-57, 58-109.
- 3 strophes: 59-76, 77-92, 93-109.
- 20 verses: 2-10, 11-16, 17-23, 24-30, 31-36, 37-43, 44-50, 51-57; 59-62, 63-68, 69-73, 74-76, 77-81, 82-86, 87-90, 91-92, 93-97, 98-103, 104-107, 108-109.
- 109 tokens.
- 8 sentences:
  - "I.11": 1-1.
  - vv.1-3 "tu ne quaesieris... numeros": 2-18.
  - vv.3-3 "ut melius... pati": 19-23.
  - vv.4-7 "seu pluris... reseces": 24-46.
  - vv.7-8 "dum... postero": 47-47.
  - "I.20": 58-58.
  - vv.1-8 "vile... imago": 59-92.
  - vv.9-12 "Caecubum... colles": 93-109.

### Tokens

```sql
select id, p1, value, lemma, pos,
    index, length,
    lemma_id, word_id
from span
where type='tok' and document_id=2
order by p1;
```

| id  | p1  | value      | lemma      | pos   | index | length | lemma_id | word_id |
|-----|-----|------------|------------|-------|-------|--------|----------|---------|
| 98  | 1   | i          | i          | NUM   | 983   | 4      | 50       | 60      |
| 99  | 2   | tu         | tu         | PRON  | 1030  | 2      | 138      | 152     |
| 100 | 3   | ne         | ne         | SCONJ | 1033  | 2      | 83       | 96      |
| 101 | 4   | quaesieris | quaeso     | VERB  | 1036  | 11     | 106      | 119     |
| 102 | 5   | scire      | scio       | VERB  | 1048  | 5      | 119      | 132     |
| 103 | 6   | nefas      | nefas      | NOUN  | 1054  | 6      | 85       | 98      |
| 104 | 7   | quem       | qui        | PRON  | 1061  | 4      | 110      | 123     |
| 105 | 8   | mihi       | ego        | PRON  | 1066  | 5      | 34       | 90      |
| 106 | 9   | quem       | qui        | PRON  | 1072  | 4      | 110      | 123     |
| 107 | 10  | tibi       | tu         | PRON  | 1077  | 4      | 138      | 150     |
| 108 | 11  | finem      | finis      | NOUN  | 1121  | 5      | 39       | 47      |
| 109 | 12  | di         | do         | VERB  | 1127  | 2      | 31       | 32      |
| 110 | 13  | dederint   | do         | VERB  | 1130  | 9      | 31       | 31      |
| 111 | 14  | leuconoe   | leuconoe   | NOUN  | 1150  | 8      | 66       | 75      |
| 112 | 15  | nec        | nec        | CCONJ | 1171  | 3      | 84       | 97      |
| 113 | 16  | babylonios | babylonius | NOUN  | 1185  | 10     | 12       | 13      |
| 114 | 17  | temptaris  | temptaris  | ADJ   | 1246  | 9      | 134      | 147     |
| 115 | 18  | numeros    | numerus    | NOUN  | 1256  | 8      | 88       | 101     |
| 116 | 19  | ut         | ut         | SCONJ | 1265  | 2      | 142      | 156     |
| 117 | 20  | melius     | bonus      | ADJ   | 1268  | 7      | 14       | 88      |
| 118 | 21  | quidquid   | quisquis   | PRON  | 1276  | 8      | 111      | 124     |
| 119 | 22  | erit       | sum        | AUX   | 1285  | 5      | 130      | 43      |
| 120 | 23  | pati       | patior     | VERB  | 1291  | 5      | 95       | 107     |
| 121 | 24  | seu        | siue       | CCONJ | 1336  | 3      | 125      | 135     |
| 122 | 25  | pluris     | pluris     | DET   | 1340  | 6      | 97       | 109     |
| 123 | 26  | hiemes     | hiemes     | NOUN  | 1347  | 6      | 45       | 54      |
| 124 | 27  | seu        | siue       | CCONJ | 1354  | 3      | 125      | 135     |
| 125 | 28  | tribuit    | tribuo     | VERB  | 1358  | 7      | 137      | 151     |
| 126 | 29  | iuppiter   | iuppiter   | ADV   | 1376  | 8      | 63       | 72      |
| 127 | 30  | ultimam    | ultimus    | ADJ   | 1396  | 8      | 141      | 155     |
| 128 | 31  | quae       | qui        | PRON  | 1444  | 4      | 110      | 118     |
| 129 | 32  | nunc       | nunc       | ADV   | 1449  | 4      | 89       | 102     |
| 130 | 33  | oppositis  | oppono     | VERB  | 1454  | 9      | 92       | 105     |
| 131 | 34  | debilitat  | debilito   | VERB  | 1464  | 9      | 28       | 30      |
| 132 | 35  | pumicibus  | pumic      | NOUN  | 1474  | 9      | 105      | 117     |
| 133 | 36  | mare       | mare       | NOUN  | 1484  | 4      | 74       | 84      |
| 134 | 37  | tyrrhenum  | tyrrhes    | NOUN  | 1538  | 9      | 140      | 154     |
| 135 | 38  | sapias     | sapius     | NOUN  | 1560  | 7      | 118      | 131     |
| 136 | 39  | vina       | vina       | NOUN  | 1568  | 4      | 147      | 162     |
| 137 | 40  | liques     | liques     | NOUN  | 1573  | 7      | 70       | 79      |
| 138 | 41  | et         | et         | CCONJ | 1581  | 2      | 36       | 45      |
| 139 | 42  | spatio     | spatium    | NOUN  | 1584  | 6      | 126      | 140     |
| 140 | 43  | brevi      | brevum     | ADJ   | 1591  | 5      | 15       | 15      |
| 141 | 44  | spem       | spes       | NOUN  | 1636  | 4      | 128      | 141     |
| 142 | 45  | longam     | longus     | ADJ   | 1641  | 6      | 71       | 81      |
| 143 | 46  | reseces    | resex      | NOUN  | 1648  | 8      | 115      | 128     |
| 144 | 47  | dum        | dum        | SCONJ | 1657  | 3      | 33       | 38      |
| 145 | 48  | loquimur   | loquor     | VERB  | 1661  | 9      | 72       | 82      |
| 146 | 49  | fugerit    | facio      | VERB  | 1671  | 7      | 37       | 51      |
| 147 | 50  | invida     | invidus    | ADJ   | 1679  | 6      | 57       | 66      |
| 148 | 51  | aetas      | aetas      | NOUN  | 1725  | 6      | 2        | 2       |
| 149 | 52  | carpe      | caro       | NOUN  | 1732  | 5      | 19       | 19      |
| 150 | 53  | diem       | dies       | NOUN  | 1738  | 4      | 30       | 35      |
| 151 | 54  | quam       | quam       | SCONJ | 1743  | 4      | 107      | 120     |
| 152 | 55  | minimum    | paruus     | NOUN  | 1748  | 7      | 93       | 91      |
| 153 | 56  | credula    | credula    | NOUN  | 1756  | 7      | 26       | 26      |
| 154 | 57  | postero    | posterus   | NOUN  | 1764  | 8      | 100      | 111     |
| 155 | 58  | i          | i.20       | NUM   | 1848  | 4      |          |         |
| 156 | 59  | vile       | vilis      | NOUN  | 1925  | 4      | 146      | 161     |
| 157 | 60  | potabis    | poto       | VERB  | 1930  | 7      | 103      | 114     |
| 158 | 61  | modicis    | modicus    | ADJ   | 1938  | 7      | 81       | 94      |
| 159 | 62  | sabinum    | sabinus    | NOUN  | 1956  | 7      | 117      | 130     |
| 160 | 63  | cantharis  | cantharis  | NOUN  | 2014  | 10     | 18       | 18      |
| 161 | 64  | graeca     | graecus    | NOUN  | 2035  | 6      | 43       | 52      |
| 162 | 65  | quod       | quod       | SCONJ | 2053  | 4      | 112      | 125     |
| 163 | 66  | ego        | ego        | PRON  | 2058  | 3      | 34       | 40      |
| 164 | 67  | ipse       | ipse       | DET   | 2062  | 4      | 60       | 70      |
| 165 | 68  | testa      | testa      | NOUN  | 2067  | 5      | 135      | 148     |
| 166 | 69  | conditum   | condo      | VERB  | 2112  | 8      | 24       | 24      |
| 167 | 70  | levi       | levum      | NOUN  | 2121  | 5      | 68       | 76      |
| 168 | 71  | datus      | do         | VERB  | 2127  | 5      | 31       | 29      |
| 169 | 72  | in         | in         | ADP   | 2133  | 2      | 55       | 64      |
| 170 | 73  | theatro    | theater    | NOUN  | 2136  | 7      | 136      | 149     |
| 171 | 74  | cum        | cum        | ADP   | 2184  | 3      | 27       | 27      |
| 172 | 75  | tibi       | tu         | PRON  | 2188  | 4      | 138      | 150     |
| 173 | 76  | plausus    | plausus    | VERB  | 2193  | 8      | 96       | 108     |
| 174 | 77  | clare      | claris     | ADJ   | 2290  | 5      | 21       | 21      |
| 175 | 78  | maecenas   | maecena    | NOUN  | 2306  | 8      | 73       | 83      |
| 176 | 79  | eques      | eques      | AUX   | 2326  | 6      | 35       | 42      |
| 177 | 80  | ut         | ut         | SCONJ | 2333  | 2      | 142      | 156     |
| 178 | 81  | paterni    | paternus   | ADJ   | 2336  | 7      | 94       | 106     |
| 179 | 82  | fluminis   | flumen     | NOUN  | 2383  | 8      | 41       | 49      |
| 180 | 83  | ripae      | ripa       | NOUN  | 2392  | 5      | 116      | 129     |
| 181 | 84  | simul      | simul      | ADV   | 2398  | 5      | 124      | 139     |
| 182 | 85  | et         | et         | CCONJ | 2404  | 2      | 36       | 45      |
| 183 | 86  | iocosa     | iocosus    | ADJ   | 2407  | 6      | 58       | 67      |
| 184 | 87  | redderet   | reddo      | VERB  | 2453  | 8      | 113      | 126     |
| 185 | 88  | laudes     | laus       | NOUN  | 2462  | 6      | 64       | 73      |
| 186 | 89  | tibi       | tu         | PRON  | 2469  | 4      | 138      | 150     |
| 187 | 90  | vaticani   | vaticanus  | ADJ   | 2484  | 8      | 144      | 158     |
| 188 | 91  | montis     | mons       | NOUN  | 2544  | 6      | 82       | 95      |
| 189 | 92  | imago      | imago      | NOUN  | 2551  | 6      | 54       | 63      |
| 190 | 93  | caecubum   | caecubus   | NOUN  | 2656  | 8      | 16       | 16      |
| 191 | 94  | et         | et         | CCONJ | 2676  | 2      | 36       | 45      |
| 192 | 95  | prelo      | prelo      | NOUN  | 2679  | 5      | 104      | 116     |
| 193 | 96  | domitam    | domo       | VERB  | 2685  | 7      | 32       | 37      |
| 194 | 97  | caleno     | caleno     | ADJ   | 2703  | 6      | 17       | 17      |
| 195 | 98  | tu         | tu         | PRON  | 2761  | 2      | 138      | 152     |
| 196 | 99  | bibes      | bibo       | VERB  | 2764  | 5      | 13       | 14      |
| 197 | 100 | uvam       | uva        | NOUN  | 2770  | 5      | 143      | 157     |
| 198 | 101 | mea        | meus       | DET   | 2776  | 3      | 78       | 87      |
| 199 | 102 | nec        | nec        | CCONJ | 2780  | 3      | 84       | 97      |
| 200 | 103 | falernae   | falernus   | ADJ   | 2794  | 8      | 38       | 46      |
| 201 | 104 | temperant  | tempero    | VERB  | 2854  | 9      | 133      | 146     |
| 202 | 105 | vites      | vis        | NOUN  | 2864  | 5      | 148      | 163     |
| 203 | 106 | neque      | neque      | CCONJ | 2870  | 5      | 86       | 99      |
| 204 | 107 | formiani   | formianus  | NOUN  | 2886  | 8      | 42       | 50      |
| 205 | 108 | pocula     | poculus    | NOUN  | 2947  | 6      | 98       | 110     |
| 206 | 109 | colles     | collis     | NOUN  | 2954  | 7      | 22       | 22      |

### Structures

```sql
select id, type, p1, p2, text, index, length
from span
where type<>'tok' and document_id=2
order by p1;
```

| id  | type | p1  | p2  | text                                                                                                          | index | length |
|-----|------|-----|-----|---------------------------------------------------------------------------------------------------------------|-------|--------|
| 207 | div  | 1   | 57  | I.11 tu ne quaesieris, scire nefas, quem mihi... carpe diem quam minimum credula postero.                     | 939   | 509    |
| 232 | snt  | 1   | 1   | I.11                                                                                                          | 817   | 168    |
| 233 | snt  | 2   | 18  | tu ne quaesieris, scire nefas, quem mihi... Leuconoe , nec Babylonios temptaris numeros.                      | 1030  | 234    |
| 212 | l    | 2   | 10  | tu ne quaesieris, scire nefas, quem mihi, quem tibi                                                           | 1008  | 51     |
| 213 | l    | 11  | 16  | finem di dederint, Leuconoe, nec Babylonios                                                                   | 1099  | 43     |
| 214 | l    | 17  | 23  | temptaris numeros. ut melius, quidquid erit, pati.                                                            | 1224  | 50     |
| 234 | snt  | 19  | 23  | ut melius, quidquid erit, pati.                                                                               | 1265  | 31     |
| 235 | snt  | 24  | 46  | seu pluris hiemes seu tribuit Iuppiter ultimam... sapias, vina liques, et spatio brevi spem longam reseces.   | 1336  | 320    |
| 215 | l    | 24  | 30  | seu pluris hiemes seu tribuit Iuppiter ultimam,                                                               | 1314  | 47     |
| 216 | l    | 31  | 36  | quae nunc oppositis debilitat pumicibus mare                                                                  | 1422  | 44     |
| 217 | l    | 37  | 43  | Tyrrhenum: sapias, vina liques, et spatio brevi                                                               | 1506  | 47     |
| 218 | l    | 44  | 50  | spem longam reseces. dum loquimur, fugerit invida                                                             | 1614  | 49     |
| 236 | snt  | 47  | 57  | dum loquimur, fugerit invida aetas: carpe diem quam minimum credula postero.                                  | 1657  | 115    |
| 219 | l    | 51  | 57  | aetas: carpe diem quam minimum credula postero.                                                               | 1703  | 47     |
| 237 | snt  | 58  | 58  | I.20                                                                                                          | 1848  | 2      |
| 208 | div  | 58  | 109 | I.20 vile potabis modicis Sabinum cantharis...nec Falernae temperant vites neque Formiani pocula colles.      | 1804  | 621    |
| 209 | lg   | 59  | 76  | vile potabis modicis Sabinum cantharis, Graeca...ipse testa conditum levi, datus in theatro cum tibi plausus, | 1873  | 190    |
| 220 | l    | 59  | 62  | vile potabis modicis Sabinum                                                                                  | 1906  | 28     |
| 238 | snt  | 59  | 92  | vile potabis modicis Sabinum cantharis, Graeca...simul et iocosa redderet laudes tibi Vaticani montis imago.  | 1925  | 632    |
| 221 | l    | 63  | 68  | cantharis, Graeca quod ego ipse testa                                                                         | 1995  | 37     |
| 222 | l    | 69  | 73  | conditum levi, datus in theatro                                                                               | 2093  | 31     |
| 223 | l    | 74  | 76  | cum tibi plausus,                                                                                             | 2164  | 17     |
| 224 | l    | 77  | 81  | clare Maecenas eques, ut paterni                                                                              | 2271  | 32     |
| 210 | lg   | 77  | 92  | clare Maecenas eques, ut paterni fluminis...simul et iocosa redderet laudes tibi Vaticani montis imago.       | 2238  | 181    |
| 225 | l    | 82  | 86  | fluminis ripae simul et iocosa                                                                                | 2364  | 30     |
| 226 | l    | 87  | 90  | redderet laudes tibi Vaticani                                                                                 | 2434  | 29     |
| 227 | l    | 91  | 92  | montis imago.                                                                                                 | 2524  | 13     |
| 239 | snt  | 93  | 109 | Caecubum et prelo domitam Caleno tu bibes uvam...nec Falernae temperant vites neque Formiani pocula colles.   | 2656  | 305    |
| 228 | l    | 93  | 97  | Caecubum et prelo domitam Caleno                                                                              | 2627  | 32     |
| 211 | lg   | 93  | 109 | Caecubum et prelo domitam Caleno tu bibes uvam...nec Falernae temperant vites neque Formiani pocula colles.   | 2594  | 184    |
| 229 | l    | 98  | 103 | tu bibes uvam: mea nec Falernae                                                                               | 2741  | 31     |
| 230 | l    | 104 | 107 | temperant vites neque Formiani                                                                                | 2834  | 30     |
| 231 | l    | 108 | 109 | pocula colles.                                                                                                | 2926  | 14     |

⏮️ [simple example - Catullus](example-dump-1.md)
⏭️ [simple example -words](example-words.md)
