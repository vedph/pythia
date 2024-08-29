# Query Samples

- [Query Samples](#query-samples)
  - [Single Token](#single-token)
  - [Single Structure](#single-structure)
  - [Multiple Pairs](#multiple-pairs)
  - [Collocations](#collocations)
  - [Scopes](#scopes)
  - [Ghost Structures in Search](#ghost-structures-in-search)

All the samples refer to this TEI document:

```xml
<?xml version="1.0" encoding="utf-8"?>
<TEI>
<teiHeader>
<fileDesc>
<titleStmt>
<author>Catullus</author>
<title type="poetry">carmina</title>
<date when="-54">I a.C.</date>
</titleStmt>
<publicationStmt>
<p>test</p>
</publicationStmt>
<sourceDesc>
<p>web</p>
</sourceDesc>
</fileDesc>
</teiHeader>
<text>
<body>
<div type="poem" n="84">
<head>ad Arrium</head>
<lg type="eleg" n="1">
<l n="1" type="h"><quote>chommoda</quote> dicebat, si quando commoda vellet</l>
<l n="2" type="p">dicere, et insidias <persName>Arrius</persName> <quote>hinsidias</quote>,</l>
</lg>
<lg type="eleg" n="2">
<l n="3" type="h">et tum mirifice sperabat se esse locutum,</l>
<l n="4" type="p">cum quantum poterat dixerat <quote>hinsidias</quote>.</l>
</lg>
<lg type="eleg" n="3">
<l n="5" type="h">credo, sic mater, sic liber avunculus eius</l>
<l n="6" type="p">sic maternus avus dixerat atque avia.</l>
</lg>
<lg type="eleg" n="4">
<l n="7" type="h">hoc misso in <geogName>Syriam</geogName> requierant omnibus aures</l>
<l n="8" type="p">audibant eadem haec leniter et leviter,</l>
</lg>
<lg type="eleg" n="5">
<l n="9" type="h">nec sibi postilla metuebant talia verba,</l>
<l n="10" type="p">cum subito affertur nuntius horribilis,</l>
</lg>
<lg type="eleg" n="6">
<l n="11" type="h"><geogName>Ionios</geogName> fluctus, postquam illuc <persName>Arrius</persName> isset,</l>
<l n="12" type="p">iam non <geogName>Ionios</geogName> esse sed <quote><geogName>Hionios</geogName></quote>.</l>
</lg>
</div>
</body>
</text>
</TEI>
```

The corresponding profile is:

```json
{
  "SourceCollector": {
    "Id": "source-collector.file",
    "Options": {
      "IsRecursive": false
    }
  },
  "TextFilters": [
    {
      "Id": "text-filter.tei"
    },
    {
      "Id": "text-filter.quotation-mark"
    }
  ],
  "AttributeParser": {
    "Id": "attribute-parser.xml",
    "Options": {
      "Mappings": [
        "author=/TEI/teiHeader/fileDesc/titleStmt/author",
        "title=/TEI/teiHeader/fileDesc/titleStmt/title",
        "category=/TEI/teiHeader/fileDesc/titleStmt/title/@type",
        "date=/TEI/teiHeader/fileDesc/titleStmt/date",
        "date-value=/TEI/teiHeader/fileDesc/titleStmt/date/@when [N]"
      ]
    }
  },
  "DocSortKeyBuilder": {
    "Id": "doc-sortkey-builder.standard"
  },
  "DocDateValueCalculator": {
    "Id": "doc-datevalue-calculator.standard",
    "Options": {
      "Attribute": "date-value"
    }
  },
  "Tokenizer": {
    "Id": "tokenizer.standard",
    "Options": {
      "TokenFilters": [
        {
          "Id": "token-filter.ita"
        },
        {
          "Id": "token-filter.len-supplier",
          "Options": {
            "LetterOnly": true
          }
        },
        {
          "Id": "token-filter.sylc-supplier-lat"
        }
      ]
    }
  },
  "StructureParsers": [
    {
      "Id": "structure-parser.xml",
      "Options": {
        "RootPath": "/TEI/text/body",
        "Definitions": [
          "div=/div @n head$",
          "p=//p",
          "lg=//lg @n$",
          "l=//l @n$",
          "quote:q:1=//quote",
          "persName:pn=//persName",
          "geogName:gn=//geogName"
        ]
      },
      "Filters": [
        {
          "Id": "struct-filter.standard"
        }
      ]
    },
    {
      "Id": "structure-parser.xml-sentence",
      "Options": {
        "RootPath": "TEI//body",
        "StopTags": ["head"]
      }
    }
  ],
  "TextRetriever": {
    "Id": "text-retriever.ef"
  },
  "TextMapper": {
    "Id": "text-mapper.xml",
    "Options": {
      "RootPath": "/TEI/text/body",
      "MappedPaths": ["body/div /@type /@n /head"]
    }
  },
  "TextPicker": {
    "Id": "text-picker.xml",
    "Options": {
      "HitOpen": "<hi rend=\"hit\">",
      "HitClose": "</hi>"
    }
  },
  "TextRenderer": {
    "Id": "text-renderer.liz-html"
  }
}
```

## Single Token

- `[value="chommoda"]`: find the word `chommoda`. 1 result: `chommoda`.
- `[value<>"sic"]`: find any word different from `sic`. 71 results: `ad`, `arrium`, `chommoda`, `dicebat`, `si`... etc.
- `[value*="ommo"]`: find any word including `ommo`. 2 results: `chommoda`, `commoda`.
- `[value^="ch"]`: find any word starting with `ch`. 1 result: `chommoda`.
- `[value$="ter"]`: find any word ending with `ter`. 3 results: `mater`, `leniter`, `leviter`.
- `[value?="ch*da"]`: find any word beginning with `ch` and ending with `da` (wildcards). 1 result: `chommoda`.
- `[value?="?um"]`: find any word where a single character is followed by `um`. 3 results: `tum`, `cum`, `cum`.
- `[value~="ch?ommoda"]`: find words `chommoda` or `commoda` (regular expression). 2 results: `chommoda`, `commoda`
- `[value%="chommoda:0.5"]`: find words similar to `chommoda` using a similarity treshold equal to 0.5 (in a normalized scale comprised between 0 and 1). 2 results: `chommoda`, `commoda`.
- `[pn="arrius"]`: non-privileged (personal name): find any anthroponym equal to `arrius`. Here we are matching against an attribute `pn` representing the personal name, and derived from the TEI `persName` element in the input document. 2 results: `arrius`, `arrius`.

Numeric operators:

- `[len<"3"]`
- `[len<="2"]`
- `[len=="10"]`
- `[len!="2"]`
- `[len>"9"]`
- `[len>="5"]`

Note that in this example the `len` attribute refers to the word's values as filtered, excluding noise characters like punctuation, diacritics, etc. For instance, for `[len>"9"]` results are 2: `requierant`, `horribilis`.

- `[sylc="4"]`: find any word counting 4 syllables. This attribute relies on the parser's integration with the Chiron system, so that the data are not derived from the document, but from software analysis performed in real time during indexing. 8 results: `insidias`, `hinsidias`, `mirifice`, `hinsidias`, `avunculus`, `metuebant`, `horribilis`, `hionios`.

## Single Structure

- `[$name="lg"]` or `[$lg]`: the shorter form is available only for non-privileged attributes. find all the structures having name `lg` (TEI line group = strophe): 6 results. Note that the title's words ("ad Arrium") do not appear among the results, as they are not included inside a strophe.
- `[$name="l"]` or `[$l]`: find all the structures having name `l` (line): 12 results.
- `[$name="snt"]` or `[$snt]`: find all the structures having name `snt` (sentence): 4 results, including the title ("ad Arrium").
- `[$name="div"]` or `[$div]`: find all the structures having name `div` (here corresponding to a single poem): 1 result.

## Multiple Pairs

- OR: `[value="chommoda"] OR [value="commoda"]` (this is better accomplished by using a single pair with a regular expression).
- AND: `[value="ionios"] AND [gn]` (geographic name): find the word Ionios when it's a toponym. Here we are matching against an attribute `gn` representing the geographic name, and derived from the TEI `geogName` element in the input document. 2 results: `ionios`, `ionios`.
- AND NOT: `[value="ionios"] AND NOT [gn]`

## Collocations

- **NEAR**: `[value="sic"] NEAR(m=0,s=l) [value="mater"]`: `sic` at either side of `mater`, with a maximum distance of 0, when both tokens are inside the same structure named `l` (verse). 2 results: `sic`, `sic`.
- **NOT NEAR**: `[value="sic"] NOT NEAR(m=0) [value="mater"]`: find the word `sic` not immediately before/after the word `mater`. 1 result: `sic` at verse 6.
- **BEFORE**: `[value="sic"] BEFORE(m=0,s=l) [value="mater"]`: find the word `sic` at the left side of `mater`, with a maximum distance of 0, when both tokens are inside the same structure named `l` (verse). 1 result: `sic`.
- **NOT BEFORE**: `[value="sic"] NOT BEFORE(m=0) [value="mater"]`: find the word `sic` when it is not immediately followed by `mater`. 2 results: `sic` (all the 3 `sic` words in the sample text, except for the one before `mater`).
- **AFTER**: `[value="sic"] AFTER(m=0,s=l) [value="mater"]`: find the word `sic` immediately following (minimum distance=0) the word `mater`, only when both tokens are inside the same structure named `l` (verse). 1 result: `sic`.
- **NOT AFTER**: `[value="sic"] NOT AFTER(m=0) [value="mater"]`. 2 results.
- **INSIDE**: `[value$="ter"] INSIDE(me=0) [$l]`: find any word ending with `ter` at verse end, i.e. inside a structure named `l`, with a maximum distance of 0 to the end of that structure. 1 result: `leviter`.
- **NOT INSIDE**: `[len="2"] NOT INSIDE() [$lg]`: find any word consisting of 2 letters and not included in a stanza. 1 result: `ad` (from the title `ad Arrium`).
- **OVERLAPS**: `[pn] OVERLAPS() [$l]`: find any person name overlapping with a verse structure. 2 results: `Arrius` (excluding the third `Arrius` which is found in the title).
- **LALIGN**: `[$name="l"] LALIGN(m=0) [$name="snt"]`: find any verse whose beginning coincides with the beginning of a sentence. 3 results for verses starting with `chommoda`, `credo`, `hoc`.
- **RALIGN**: `[$name="l"] RALIGN(m=0) [$name="snt"]`: find any verse whose end coincides with a sentence end. 3 results for verses ending with `hinsidias`, `avias`, `Hionios`.

## Scopes

- **corpus**: `@@[neoteroi rhetoric][value="chommoda"]`: find the word `chommoda` only in the documents belonging to any of the corpora named `neoteoroi` and `rhetoric` (assume that the sample document belongs to the former). 1 result: `chommoda`.
- **document**: `@[author="Catullus" AND (date_value>="0" OR category="poetry")][value="chommoda"]`: find the word `chommoda` only in those documents having as author `Catullus` and being either dated A.D. or included in the poetry category. Here `author`, `date_value`, and `category` are all document attributes. 1 result: `chommoda`.
- **corpus and document**: `@@[neoteroi rhetoric]@[author="Catullus" AND (date_value>="0" OR category="poetry")][value="chommoda"]`: find the word `chommoda` only in the documents belonging to any of the corpora named `neoteoroi` and `rhetoric` (assume that the sample document belongs to the former), only in those documents having as author `Catullus` and being either dated A.D. or included in the poetry category. 1 result: `chommoda`.

## Ghost Structures in Search

This sample refers to ghost structures, i.e. those structures defined only for the purpose of decorating the tokens they include with some attributes.

For instance, to find all the foreign Latin words in an Italian corpus one could write a pair like `[frgn=lat]`. This implies a structure parser defined in the profile like this:

```json
"StructureParsers": [
  {
    "Id": "structure-parser.xml",
    "Options": {
      "RootPath": "/TEI/text/body",
      "**Definitions**": [
        "div=/div @n head$",
        "p=//p",
        "lg=//lg",
        "l=//l @n$",
        "foreign:frgn=//foreign @lang"
      ]
    }
  }
],
```

Note the `foreign:frgn=//foreign @lang` definition, where `foreign` is the name of the structure (which is not stored but only used to add token's attributes), `frgn` the name of the token attribute to add, and `//foreign @lang` the path to the structure value. The latter will be used as the value for the `frgn` token attribute. Should you want a fixed value (e.g. `1`), you might do something like: `foreign:frgn:1=//foreign @lang`.

---

⏮️ [query](query.md)

⏭️ [storage](storage.md)
