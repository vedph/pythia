# Example Documents

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
1 11

1tu 2ne 3quaesieris, 4scire 5nefas, 6quem 7mihi, 8quem 9tibi
10finem 11di 12dederint, 13Leuconoe, 14nec 15Babylonios
16temptaris 17numeros. 18ut 19melius, 20quidquid 21erit, 22pati.
23seu 24pluris 25hiemes 26seu 27tribuit 28Iuppiter 29ultimam,
30quae 31nunc 32oppositis 33debilitat 34pumicibus 35mare
36Tyrrhenum: 37sapias, 38vina 39liques, 40et 41spatio 42brevi
43spem 44longam 45reseces. 46dum 47loquimur, 48fugerit 49invida
50aetas: 51carpe 52diem 53quam 54minimum 55credula 56postero.

1 20

57vile 58potabis 59modicis 60Sabinum
61cantharis, 62Graeca 63quod 64ego 65ipse 66testa
67conditum 68levi, 69datus 70in 71theatro
72cum 73tibi 74plausus,

75clare 76Maecenas 77eques, 78ut 79paterni
80fluminis 81ripae 82simul 83et 84iocosa
85redderet 86laudes 87tibi 88Vaticani
89montis 90imago.

91Caecubum 92et 93prelo 94domitam 95Caleno
96tu 97bibes 98uvam: 99mea 100nec 101Falernae
102temperant 103vites 104neque 105Formiani
106pocula 107colles.
```

Here we have:

- 2 poems: 1-56, 57-107.
- 3 strophes: 57-74, 75-90, 91-107.
- 20 verses: 1-9, 10-15, 16-22, 23-29, 30-35, 36-42, 43-49, 50-56, 57-60, 61-66, 67-71, 72-74, 75-79, 80-84, 85-88, 89-90, 91-95, 96-101, 102-105, 106-107.
- 107 tokens.
- 6 sentences:
  - vv.1-3 "tu ne quaesieris... numeros": 1-17.
  - vv.3-3 "ut melius... pati": 18-22.
  - vv.4-7 "seu pluris... reseces": 23-45.
  - vv.7-8 "dum... postero": 46-56.
  - vv.1-8 "vile... imago": 57-90.
  - vv.9-12 "Caecubum... colles": 91-107.

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
| 98  | 1   | tu         | tū         | PRON  | 1030  | 2      | 140      | 151     |
| 99  | 2   | ne         | nē         | PART  | 1033  | 2      | 83       | 95      |
| 100 | 3   | quaesieris | quaeso     | VERB  | 1036  | 11     | 106      | 118     |
| 101 | 4   | scire      | scīo       | VERB  | 1048  | 5      | 119      | 131     |
| 102 | 5   | nefas      | nefās      | PROPN | 1054  | 6      | 85       | 97      |
| 103 | 6   | quem       | qui        | PRON  | 1061  | 4      | 110      | 122     |
| 104 | 7   | mihi       | ego        | PRON  | 1066  | 5      | 35       | 89      |
| 105 | 8   | quem       | qui        | PRON  | 1072  | 4      | 110      | 122     |
| 106 | 9   | tibi       | tu         | PRON  | 1077  | 4      | 139      | 149     |
| 107 | 10  | finem      | fīnis      | NOUN  | 1121  | 5      | 39       | 47      |
| 108 | 11  | di         | dī         | VERB  | 1127  | 2      | 29       | 32      |
| 109 | 12  | dederint   | do         | VERB  | 1130  | 9      | 32       | 31      |
| 110 | 13  | leuconoe   | leuconoē   | PROPN | 1150  | 8      | 65       | 74      |
| 111 | 14  | nec        | nec        | CCONJ | 1171  | 3      | 84       | 96      |
| 112 | 15  | babylonios | babylōnīus | NOUN  | 1185  | 10     | 12       | 13      |
| 113 | 16  | temptaris  | temptus    | ADJ   | 1246  | 9      | 135      | 146     |
| 114 | 17  | numeros    | numerōs    | NOUN  | 1256  | 8      | 88       | 100     |
| 115 | 18  | ut         | ut         | SCONJ | 1265  | 2      | 144      | 155     |
| 116 | 19  | melius     | bonus      | ADJ   | 1268  | 7      | 14       | 87      |
| 117 | 20  | quidquid   | quisquis   | PRON  | 1276  | 8      | 111      | 123     |
| 118 | 21  | erit       | sum        | AUX   | 1285  | 5      | 131      | 43      |
| 119 | 22  | pati       | patīor     | VERB  | 1291  | 5      | 94       | 106     |
| 120 | 23  | seu        | siue       | CCONJ | 1336  | 3      | 126      | 134     |
| 121 | 24  | pluris     | plūrīs     | DET   | 1340  | 6      | 96       | 108     |
| 122 | 25  | hiemes     | hiemēs     | NOUN  | 1347  | 6      | 46       | 54      |
| 123 | 26  | seu        | siue       | CCONJ | 1354  | 3      | 126      | 134     |
| 124 | 27  | tribuit    | tribuo     | VERB  | 1358  | 7      | 138      | 150     |
| 125 | 28  | iuppiter   | iuppiter   | ADV   | 1376  | 8      | 62       | 71      |
| 126 | 29  | ultimam    | ultimus    | ADJ   | 1396  | 8      | 143      | 154     |
| 127 | 30  | quae       | qui        | PRON  | 1444  | 4      | 110      | 117     |
| 128 | 31  | nunc       | nunc       | ADV   | 1449  | 4      | 89       | 101     |
| 129 | 32  | oppositis  | oppono     | VERB  | 1454  | 9      | 92       | 104     |
| 130 | 33  | debilitat  | dēbilito   | VERB  | 1464  | 9      | 28       | 30      |
| 131 | 34  | pumicibus  | pūmicus    | NOUN  | 1474  | 9      | 105      | 116     |
| 132 | 35  | mare       | mare       | NOUN  | 1484  | 4      | 74       | 83      |
| 133 | 36  | tyrrhenum  | tyrrhēs    | NOUN  | 1538  | 9      | 142      | 153     |
| 134 | 37  | sapias     | sapiās     | NOUN  | 1560  | 7      | 118      | 130     |
| 135 | 38  | vina       | vīna       | NOUN  | 1568  | 4      | 149      | 161     |
| 136 | 39  | liques     | liquēs     | NOUN  | 1573  | 7      | 69       | 78      |
| 137 | 40  | et         | et         | CCONJ | 1581  | 2      | 37       | 45      |
| 138 | 41  | spatio     | spatium    | NOUN  | 1584  | 6      | 127      | 139     |
| 139 | 42  | brevi      | brevī      | ADJ   | 1591  | 5      | 15       | 15      |
| 140 | 43  | spem       | spes       | NOUN  | 1636  | 4      | 129      | 140     |
| 141 | 44  | longam     | lōngus     | ADJ   | 1641  | 6      | 71       | 80      |
| 142 | 45  | reseces    | resecēs    | VERB  | 1648  | 8      | 115      | 127     |
| 143 | 46  | dum        | dum        | SCONJ | 1657  | 3      | 34       | 38      |
| 144 | 47  | loquimur   | loquor     | VERB  | 1661  | 9      | 72       | 81      |
| 145 | 48  | fugerit    | fūgo       | VERB  | 1671  | 7      | 43       | 51      |
| 146 | 49  | invida     | invidus    | ADJ   | 1679  | 6      | 57       | 65      |
| 147 | 50  | aetas      | aetās      | NOUN  | 1725  | 6      | 2        | 2       |
| 148 | 51  | carpe      | caro       | NOUN  | 1732  | 5      | 19       | 19      |
| 149 | 52  | diem       | dīes       | NOUN  | 1738  | 4      | 31       | 35      |
| 150 | 53  | quam       | quam       | SCONJ | 1743  | 4      | 107      | 119     |
| 151 | 54  | minimum    | paruus     | NOUN  | 1748  | 7      | 93       | 90      |
| 152 | 55  | credula    | crēdulā    | NOUN  | 1756  | 7      | 26       | 26      |
| 153 | 56  | postero    | posterō    | ADJ   | 1764  | 8      | 99       | 110     |
| 154 | 57  | vile       | vīle       | NOUN  | 1925  | 4      | 148      | 160     |
| 155 | 58  | potabis    | pōtāba     | NOUN  | 1930  | 7      | 102      | 113     |
| 156 | 59  | modicis    | modicus    | ADJ   | 1938  | 7      | 81       | 93      |
| 157 | 60  | sabinum    | sabīnus    | NOUN  | 1956  | 7      | 117      | 129     |
| 158 | 61  | cantharis  | cantharīs  | NOUN  | 2014  | 10     | 18       | 18      |
| 159 | 62  | graeca     | graecus    | ADJ   | 2035  | 6      | 44       | 52      |
| 160 | 63  | quod       | quod       | SCONJ | 2053  | 4      | 112      | 124     |
| 161 | 64  | ego        | ego        | PRON  | 2058  | 3      | 35       | 40      |
| 162 | 65  | ipse       | ipse       | DET   | 2062  | 4      | 60       | 69      |
| 163 | 66  | testa      | testā      | NOUN  | 2067  | 5      | 136      | 147     |
| 164 | 67  | conditum   | condo      | VERB  | 2112  | 8      | 24       | 24      |
| 165 | 68  | levi       | levī       | NOUN  | 2121  | 5      | 66       | 75      |
| 166 | 69  | datus      | do         | VERB  | 2127  | 5      | 32       | 29      |
| 167 | 70  | in         | īn         | ADP   | 2133  | 2      | 55       | 63      |
| 168 | 71  | theatro    | theāter    | NOUN  | 2136  | 7      | 137      | 148     |
| 169 | 72  | cum        | cum        | ADP   | 2184  | 3      | 27       | 27      |
| 170 | 73  | tibi       | tu         | PRON  | 2188  | 4      | 139      | 149     |
| 171 | 74  | plausus    | plausus    | VERB  | 2193  | 8      | 95       | 107     |
| 172 | 75  | clare      | clārē      | NOUN  | 2290  | 5      | 21       | 21      |
| 173 | 76  | maecenas   | maecēnās   | VERB  | 2306  | 8      | 73       | 82      |
| 174 | 77  | eques      | eques      | AUX   | 2326  | 6      | 36       | 42      |
| 175 | 78  | ut         | ut         | SCONJ | 2333  | 2      | 144      | 155     |
| 176 | 79  | paterni    | pssum      | VERB  | 2336  | 7      | 104      | 105     |
| 177 | 80  | fluminis   | flūmen     | NOUN  | 2383  | 8      | 41       | 49      |
| 178 | 81  | ripae      | rīpa       | NOUN  | 2392  | 5      | 116      | 128     |
| 179 | 82  | simul      | simul      | ADV   | 2398  | 5      | 125      | 138     |
| 180 | 83  | et         | et         | CCONJ | 2404  | 2      | 37       | 45      |
| 181 | 84  | iocosa     | iocōsus    | VERB  | 2407  | 6      | 58       | 66      |
| 182 | 85  | redderet   | reddo      | VERB  | 2453  | 8      | 113      | 125     |
| 183 | 86  | laudes     | laus       | NOUN  | 2462  | 6      | 63       | 72      |
| 184 | 87  | tibi       | tu         | PRON  | 2469  | 4      | 139      | 149     |
| 185 | 88  | vaticani   | vāticānus  | ADJ   | 2484  | 8      | 146      | 157     |
| 186 | 89  | montis     | mons       | NOUN  | 2544  | 6      | 82       | 94      |
| 187 | 90  | imago      | imāgō      | ADV   | 2551  | 6      | 53       | 61      |
| 188 | 91  | caecubum   | caecubus   | NOUN  | 2656  | 8      | 16       | 16      |
| 189 | 92  | et         | et         | CCONJ | 2676  | 2      | 37       | 45      |
| 190 | 93  | prelo      | prēlō      | NOUN  | 2679  | 5      | 103      | 115     |
| 191 | 94  | domitam    | domo       | VERB  | 2685  | 7      | 33       | 37      |
| 192 | 95  | caleno     | calēnō     | ADJ   | 2703  | 6      | 17       | 17      |
| 193 | 96  | tu         | tū         | PRON  | 2761  | 2      | 140      | 151     |
| 194 | 97  | bibes      | bibēs      | NOUN  | 2764  | 5      | 13       | 14      |
| 195 | 98  | uvam       | ūvus       | NOUN  | 2770  | 5      | 145      | 156     |
| 196 | 99  | mea        | meus       | DET   | 2776  | 3      | 78       | 86      |
| 197 | 100 | nec        | nec        | CCONJ | 2780  | 3      | 84       | 96      |
| 198 | 101 | falernae   | falernus   | ADJ   | 2794  | 8      | 38       | 46      |
| 199 | 102 | temperant  | tempero    | VERB  | 2854  | 9      | 134      | 145     |
| 200 | 103 | vites      | vītēs      | NOUN  | 2864  | 5      | 150      | 162     |
| 201 | 104 | neque      | neque      | CCONJ | 2870  | 5      | 86       | 98      |
| 202 | 105 | formiani   | formiānus  | NOUN  | 2886  | 8      | 42       | 50      |
| 203 | 106 | pocula     | pōculus    | NOUN  | 2947  | 6      | 97       | 109     |
| 204 | 107 | colles     | collēs     | NOUN  | 2954  | 7      | 22       | 22      |

### Structures

```sql
select id, type, p1, p2, text, index, length
from span
where type<>'tok' and document_id=2
order by p1;
```

| id  | type | p1  | p2  | text                                                                                                          | index | length |
|-----|------|-----|-----|---------------------------------------------------------------------------------------------------------------|-------|--------|
| 210 | l    | 1   | 9   | tū nē quaesieris, scīre nefās, quem mihi, quem tibi                                                           | 1008  | 51     |
| 230 | snt  | 1   | 17  | tū nē quaesieris, scīre nefās, quem mihi... Leuconoē , nec Babylōnīōs temptārīs numerōs.                      | 1030  | 234    |
| 205 | div  | 1   | 56  | 1 11 tū nē quaesieris, scīre nefās, quem mihi... carpe dīem quam minimum crēdulā posterō.                     | 939   | 509    |
| 211 | l    | 10  | 15  | fīnem dī dederint, Leuconoē, nec Babylōnīōs                                                                   | 1099  | 43     |
| 212 | l    | 16  | 22  | temptārīs numerōs. ut melius, quidquid erit, patī.                                                            | 1224  | 50     |
| 231 | snt  | 18  | 22  | ut melius, quidquid erit, patī.                                                                               | 1265  | 31     |
| 232 | snt  | 23  | 45  | seu plūrīs hiemēs seu tribuit Iuppiter ultimam... sapiās, vīna liquēs, et spatiō brevī spem lōngam resecēs.   | 1336  | 320    |
| 213 | l    | 23  | 29  | seu plūrīs hiemēs seu tribuit Iuppiter ultimam,                                                               | 1314  | 47     |
| 214 | l    | 30  | 35  | quae nunc oppositīs dēbilitat pūmicibus mare                                                                  | 1422  | 44     |
| 215 | l    | 36  | 42  | Tyrrhēnum: sapiās, vīna liquēs, et spatiō brevī                                                               | 1506  | 47     |
| 216 | l    | 43  | 49  | spem lōngam resecēs. dum loquimur, fūgerit invida                                                             | 1614  | 49     |
| 233 | snt  | 46  | 56  | dum loquimur, fūgerit invida aetās: carpe dīem quam minimum crēdulā posterō.                                  | 1657  | 115    |
| 217 | l    | 50  | 56  | aetās: carpe dīem quam minimum crēdulā posterō.                                                               | 1703  | 47     |
| 218 | l    | 57  | 60  | vīle pōtābis modicīs Sabīnum                                                                                  | 1906  | 28     |
| 207 | lg   | 57  | 74  | vīle pōtābis modicīs Sabīnum cantharīs, Graecā...ipse testā conditum levī, datus īn theātrō cum tibi plausus, | 1873  | 190    |
| 234 | snt  | 57  | 90  | vīle pōtābis modicīs Sabīnum cantharīs, Graecā...simul et iocōsa redderet laudēs tibi Vāticānī montis imāgō.  | 1925  | 632    |
| 206 | div  | 57  | 107 | 1 20 vīle pōtābis modicīs Sabīnum cantharīs...nec Falernae temperant vītēs neque Formiānī pōcula collēs.      | 1804  | 621    |
| 219 | l    | 61  | 66  | cantharīs, Graecā quod ego ipse testā                                                                         | 1995  | 37     |
| 220 | l    | 67  | 71  | conditum levī, datus īn theātrō                                                                               | 2093  | 31     |
| 221 | l    | 72  | 74  | cum tibi plausus,                                                                                             | 2164  | 17     |
| 208 | lg   | 75  | 90  | clārē Maecēnās eques, ut paternī flūminis...simul et iocōsa redderet laudēs tibi Vāticānī montis imāgō.       | 2238  | 181    |
| 222 | l    | 75  | 79  | clārē Maecēnās eques, ut paternī                                                                              | 2271  | 32     |
| 223 | l    | 80  | 84  | flūminis rīpae simul et iocōsa                                                                                | 2364  | 30     |
| 224 | l    | 85  | 88  | redderet laudēs tibi Vāticānī                                                                                 | 2434  | 29     |
| 225 | l    | 89  | 90  | montis imāgō.                                                                                                 | 2524  | 13     |
| 226 | l    | 91  | 95  | Caecubum et prēlō domitam Calēnō                                                                              | 2627  | 32     |
| 209 | lg   | 91  | 107 | Caecubum et prēlō domitam Calēnō tū bibēs ūvam...nec Falernae temperant vītēs neque Formiānī pōcula collēs.   | 2594  | 184    |
| 235 | snt  | 91  | 107 | Caecubum et prēlō domitam Calēnō tū bibēs ūvam...nec Falernae temperant vītēs neque Formiānī pōcula collēs.   | 2656  | 305    |
| 227 | l    | 96  | 101 | tū bibēs ūvam: mea nec Falernae                                                                               | 2741  | 31     |
| 228 | l    | 102 | 105 | temperant vītēs neque Formiānī                                                                                | 2834  | 30     |
| 229 | l    | 106 | 107 | pōcula collēs.                                                                                                | 2926  | 14     |

⏮️ [simple example - Catullus](example-dump-1.md)
⏭️ [simple example -words](example-words.md)
