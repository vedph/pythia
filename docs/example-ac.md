# Real-World Example

- [Real-World Example](#real-world-example)
  - [Corpus Description](#corpus-description)
  - [Procedure](#procedure)
    - [1. Profile](#1-profile)
    - [2. Create Database](#2-create-database)
    - [3. Index Files](#3-index-files)

This is a real-world example about using Pythia. Please note that the document rendition styles used in the demo frontend application (in `styles.css`) target this corpus.

## Corpus Description

The corpus sample here is from a research project collecting a number of Italian legal documents for linguistic analysis. These documents have already been anonymized by a [tool](https://ntplusdiritto.ilsole24ore.com/art/atti-chiari-chiarezza-e-concisione-scrittura-forense-AEya5kL) of my own, which replaces all the sensitive information with random data, according to a number of rules (more on this on an upcoming paper).

The final outcome is a set of documents which look like real documents, and can thus be read fluently with no raw deletions or dumb replacements. Of course, this is a requirement for linguistic analysis, and the anonymizer also takes care of a number of details, like e.g. preserving the original usage of Italian euphonic /d/ by replacing the original forms with others beginning with the same letter.

Even if the tool is format-agnostic, in this project it directly ingests Word documents ([DOCX](https://en.wikipedia.org/wiki/Office_Open_XML), and produces as its final output XML TEI documents (plus their HTML renditions for diagnostic purposes).

These documents are then collected into a central, web-based repository using a [BLOB store](https://github.com/vedph/simple-blob), and then downloaded from it to be indexed with Pythia. In this example we will just provide some of these TEI documents to show how Pythia is used in a real-world project.

The TEI documents have a minimalist header, as no sensitive information should be found in them (a separate file with metadata is stored independently). So, all what the header includes is the original file name.

The TEI document's body is the outcome of a conversion process starting from a Word document with no headings or other structural divisions, given the nature of these texts. As such, the `body` element just includes paragraphs. Some essential features of the original document styles are preserved in TEI, like bold, italic, underline, and text alignment. Also, all the portions processed by the anonymizer tool have been marked, e.g. as abbreviations, personal names, place names, juridic person names, dates, alphanumeric codes, numbers, foreign language parts, etc., using the proper TEI elements.

For instance, here is a sample paragraph:

```xml
<p rend="j"><hi rend="b"><orgName type="f">COMUNARDA</orgName> <choice><abbr>S.p.A.</abbr><expan xml:lang="ita">Società per Azioni</expan></choice></hi>, in persona del legale rappresentante <choice><abbr>p.t.</abbr><expan xml:lang="lat">pro tempore</expan></choice>, elettivamente domiciliata in <placeName>Leivi</placeName>, alla Via <address><addrLine>Yenna Vicenzo, 89</addrLine></address>, presso e nello studio dell’<choice><abbr>avv.</abbr><expan xml:lang="ita">avvocato</expan></choice> <persName type="mn">Almirante</persName> <persName type="s">Masuero</persName>, che la rappresenta e difende</p>
```

Metadata about each document (not including any personal information) are stored in a separate file, which originally is an Excel (XLSX) file having a single sheet with 2 columns, as this format is preferred by operators more familiar with office-like applications. Each row in this file represents a name=value pair. For instance, here are some rows, here represented with CSV:

```txt
atto,Atto di citazione appello
data,20091100
giudicante,Corte di Appello
grado,2
gruppo-atto,GE_t
gruppo-nr,01
id,full|ge|civ-ge-app-cit342-200911_01.xml
materia,civile
nascita-avv,1955
sede-giudicante,Genova
sede-raccolta,ge
sesso-avv,M
```

These metadata are preprocessed by the same tool used to pseudonymize the documents. This converts XLSX into CSV, while checking that all the metadata listed are recognized, their values are valid, and that all the required metadata are present. Eventually, it can also rename metadata, merging differently named metadata into one, and map their values into other values where required. All the parameters for these checks are defined in an external JSON file, so that the tool can be reused.

So, in the end what Pythia has to ingest is a set of TEI files, each having a corresponding metadata CSV file. We want to extract these metadata, and attach them to the corresponding document for indexing purposes. This will allow filtering documents by their metadata, and including these filters also in text searches.

## Procedure

Our procedure will follow these 3 main steps:

1. define a profile for the Pythia system.
2. create the database for the index, adding the profile in it.
3. index the files.

### 1. Profile

The profile is just a JSON file. You can write it with your favorite text/code editor (mine is [VSCode](https://code.visualstudio.com)).

(1) **source**: in this sample, the source for documents is just the file system, assuming that we have downloaded a few documents in some folder. So we use a `source-collector.file`, which lists all the files found in a specific folder, here without recursion:

```json
"SourceCollector": {
  "Id": "source-collector.file",
  "Options": {
    "IsRecursive": false
  }
},
```

(2) **text filters**:

- `text-filter.xml-local-tag-list` is used to extracts a list of XML tags, one for each of the tags found in the source text, and listed in the filter's options. For each tag found it stores its name and position. Such data will be used later by other components in the pipeline. In this case, these are the UDPipe components, which must ignore any text inside TEI tags like `abbr` or `num`.
- `text-filter.xml-tag-filler` is used to blank-fill the whole TEI `expan` element, as we do not want its text to be handled as document's text. In fact, the content of `expan` is just the expansion of an abbreviation in the text, so we exclude it from indexing. In its `Tags` property, we list all the tag names of the elements to be blank-filled. As `expan` belongs to the TEI namespace, we also have to add it in `Namespaces`, so that `tei:expan` gets correctly resolved and the XML element gets its correct namespace.
- `text-filter.tei` is used to discard the whole header from the text index. This avoids indexing the header's text as document's text.
- `text-filter.replacer` is used to apply a minor adjustment to source texts by means of string or pattern replacements. In this case, the only adjustment is replacing `E’` (preceded by word boundary) to `È`. This is required because in some cases the documents authors have misused this quote as an accent marker, and failing to mark it properly would have negative effects on POS tagging (`è` being a verb, and `e` a conjunction).
- `text-filter.quotation-mark` is used to ensure that apostrophes are handled correctly, by replacing smart quotes with an apostrophe character proper.
- `text-filter-udp` is used to submit the whole text (as filtered by the preceding filters) to the UDPipe service for POS tagging. The options specify the model used for the Italian language, the blacklisted tags to exclude from tagging (`abbr` and `num` as collected by the first filter above), and the text chunks sizes and tail detection expression. The filter does not effectively changes the received text, but just collects the POS tagger results to be consumed later at the token level. We require to do this at the text level, as POS tagging works on whole sentences; but we will consume the results later, so that we can add metadata to each single token as we detect and filter them.

```json
"TextFilters": [
  {
    "Id": "text-filter.xml-local-tag-list",
    "Options": {
      "Names": [
        "abbr",
        "num"
      ]
    }
  },
  {
    "Id": "text-filter.xml-tag-filler",
    "Options": {
      "Tags": [
        "tei:expan"
      ],
      "Namespaces": [
        "tei=http://www.tei-c.org/ns/1.0"
      ]
    }
  },
  {
    "Id": "text-filter.tei"
  },
  {
    "Id": "text-filter.replacer",
    "Options": {
      "Replacements": [
        {
          "Source": "\\bE’",
          "IsPattern": true,
          "Target": "È "
        }
      ]
    }
  },
  {
    "Id": "text-filter.quotation-mark"
  },
  {
    "Id": "text-filter.udp",
    "Options": {
      "Model": "italian-isdt-ud-2.10-220711",
      "MaxChunkLength": 5000,
      "ChunkTailPattern": "(?<![0-9])[.?!](?![.?!])",
      "BlackTags": [
        "abbr",
        "num"
      ]
    }
  }
],
```

(3) **attribute parsers**:

- `attribute-parser.xml` is used to extract metadata from the TEI header. The only datum here is the document's title, because as explained above for security reasons all the metadata are stored separately.
- `attribute-parser.fs-csv` is used to parse the metadata CSV file corresponding to the document file. This happens to have the same name of the document file, with the additional suffix `.meta`; so, to get the metadata file corresponding to the source file, we instruct the parser to build the file path starting from the source file path and replacing its `.xml` extension with `.xml.meta`. Also, we tell the parser to look for metadata names in column 0, and for their values in column 1, eventually trimming them.

```json
"AttributeParsers": [
  {
    "Id": "attribute-parser.xml",
    "Options": {
      "Mappings": [
        "title=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:title"
      ],
      "DefaultNsPrefix": "tei",
      "Namespaces": [
        "tei=http://www.tei-c.org/ns/1.0"
      ]
    }
  },
  {
    "Id": "attribute-parser.fs-csv",
    "Options": {
      "NameColumnIndex": 0,
      "ValueColumnIndex": 1,
      "ValueTrimming": true,
      "SourceFind": "\\.xml$",
      "SourceReplace": ".xml.meta"
    }
  }
],
```

We are thus collecting metadata for documents from two different sources: the document itself (from its TEI header), and an independent CSV file.

>Pythia also provides an XSLX (Excel) attribute parser, but here the conversion from the original Excel files has already been done by the pseudonymizer tool, which is also in charge of ensuring that metadata are uniform and valid.

(4) **document sort key builder**: the standard sort key builder (`doc-sortkey-builder.standard`) is fine. It sorts documents by author (which for these documents is always empty), title, and date.

```json
"DocSortKeyBuilder": {
  "Id": "doc-sortkey-builder.standard"
},
```

(5) **document date value calculator**: the document's date is in metadata, keyed under `act-date` (renamed from `data`), in the form YYYYMMDD. So we can use the standard calculator to get the value from it (`doc-datevalue-calculator.standard`) by parsing it via a regular expression for YMD Unix-style dates. Also, we want to store this as an integer number, rather than a string, so that sorting and filtering will be easier.

```json
"DocDateValueCalculator": {
  "Id": "doc-datevalue-calculator.unix",
  "Options": {
    "Attribute": "data",
    "YmdPattern": "(?<y>\\d{4})(?<m>\\d{2})(?<d>\\d{2})",
    "YmdAsInt": true
  }
},
```

(6) **tokenizer**: we use here a standard tokenizer (`tokenizer.standard`) which splits text at whitespace or apostrophes (while keeping the apostrophes with the token). Its **filters** are:

- `token-filter.punctuation`: this filter collects metadata about punctuation at the left/right boundaries of each token. Here we provide a whitelist of punctuation characters to include (rather than letting the filter collect any character categorized as punctuation in Unicode): `ListType=1` means that the list is a whitelist rather than a blacklist (-1).
- `token-filter.udp`: this filter consumes the UDPipe analysis results collected by `text-filter-udp` and maps them to the tokens defined by the Pythia's pipeline's tokenizer; for each matched token it adds it a selection of its POS tags as attributes.
- `token-filter.ita`: this filter removes all the characters which are not letters or apostrophe, strips from them all diacritics, and lowercases all the letters.
- `token-filter.len-supplier`: this filter does not touch the token, but adds metadata to it, related to the number of letters of each token. This is just to have some fancier metadata to play with. This way you will be able to search tokens by their letters counts.

```json
"Tokenizer": {
  "Id": "tokenizer.standard",
  "Options": {
    "TokenFilters": [
      {
        "Id": "token-filter.punctuation",
        "Options": {
          "Punctuations": ".,;:!?",
          "ListType": 1
        }
      },
      {
        "Id": "token-filter.udp",
        "Options": {
          "Props": 43
        }
      },
      {
        "Id": "token-filter.ita"
      },
      {
        "Id": "token-filter.len-supplier",
        "Options": {
          "LetterOnly": true
        }
      }
    ]
  }
},
```

(7) **structure parsers**:

- `structure-parser.xml`: a general-purpose filter for XML documents, used to extract a number of different structures corresponding to main text divisions. Unfortunately, these documents have no intrinsic structure. We can only rely on paragraphs, so this is the only structure detected. Yet, there are a number of "ghost" structures, i.e. text spans which are not to be indexed as such, but can be used to extract more metadata for the tokens they include. This happens e.g. for `persName` and all the others listed below, so that we can add metadata to the corresponding word(s) telling us that they are anthroponyms or whatever else. This will allow us searching for tokens which are e.g. person names, or geographic names, etc. Finally, the structure parser also uses a standard structure filter (`struct-filter.standard`) to properly extract their names from the original text by stripping out rumor features (diacritics, case, etc.).

- `structure-parser.xml-sentence`: a sentence parser. This relies on punctuation (and some XML tags) to determine the extent of sentences in the document.

```json
"StructureParsers": [
  {
    "Id": "structure-parser.xml",
    "Options": {
      "Definitions": [
        {
          "Name": "p",
          "XPath": "/tei:TEI/tei:text/tei:body/tei:p",
          "ValueTemplate": "1"
        },
        {
          "Name": "abbr",
          "XPath": "//tei:abbr",
          "ValueTemplate": "1",
          "TokenTargetName": "abbr"
        },
        {
          "Name": "address",
          "XPath": "//tei:address",
          "ValueTemplate": "1",
          "TokenTargetName": "address"
        },
        {
          "Name": "date",
          "XPath": "//tei:date",
          "ValueTemplate": "{t}",
          "ValueTemplateArgs": [{ "Name": "t", "Value": "." }],
          "ValueTrimming": true,
          "TokenTargetName": "address"
        },
        {
          "Name": "email",
          "XPath": "//tei:email",
          "ValueTemplate": "1",
          "TokenTargetName": "email"
        },
        {
          "Name": "foreign",
          "XPath": "//tei:foreign",
          "ValueTemplate": "{l}",
          "ValueTemplateArgs": [{ "Name": "l", "Value": "./@xml:lang" }],
          "TokenTargetName": "foreign"
        },
        {
          "Name": "hi-b",
          "XPath": "//tei:hi[contains(@rend, 'b')]",
          "ValueTemplate": "1",
          "TokenTargetName": "b"
        },
        {
          "Name": "hi-i",
          "XPath": "//tei:hi[contains(@rend, 'i')]",
          "ValueTemplate": "1",
          "TokenTargetName": "i"
        },
        {
          "Name": "hi-u",
          "XPath": "//tei:hi[contains(@rend, 'u')]",
          "ValueTemplate": "1",
          "TokenTargetName": "u"
        },
        {
          "Name": "num",
          "XPath": "//tei:num",
          "ValueTemplate": "1",
          "TokenTargetName": "n"
        },
        {
          "Name": "org-name",
          "XPath": "//tei:orgName[@type='m']",
          "ValueTemplate": "{t}",
          "ValueTemplateArgs": [{ "Name": "t", "Value": "." }],
          "ValueTrimming": true,
          "TokenTargetName": "org-m"
        },
        {
          "Name": "org-name",
          "XPath": "//tei:orgName[@type='f']",
          "ValueTemplate": "{t}",
          "ValueTemplateArgs": [{ "Name": "t", "Value": "." }],
          "ValueTrimming": true,
          "TokenTargetName": "org-f"
        },
        {
          "Name": "pers-name",
          "XPath": "//tei:persName[@type='mn']",
          "ValueTemplate": "{t}",
          "ValueTemplateArgs": [{ "Name": "t", "Value": "." }],
          "ValueTrimming": true,
          "TokenTargetName": "pn-m"
        },
        {
          "Name": "pers-name",
          "XPath": "//tei:persName[@type='fn']",
          "ValueTemplate": "{t}",
          "ValueTemplateArgs": [{ "Name": "t", "Value": "." }],
          "ValueTrimming": true,
          "TokenTargetName": "pn-f"
        },
        {
          "Name": "pers-name",
          "XPath": "//tei:persName[@type='s']",
          "ValueTemplate": "{t}",
          "ValueTemplateArgs": [{ "Name": "t", "Value": "." }],
          "ValueTrimming": true,
          "TokenTargetName": "pn-s"
        },
        {
          "Name": "place-name",
          "XPath": "//tei:placeName",
          "ValueTemplate": "{t}",
          "ValueTemplateArgs": [{ "Name": "t", "Value": "." }],
          "ValueTrimming": true,
          "TokenTargetName": "tn"
        }
      ],
      "Namespaces": [
        "tei=http://www.tei-c.org/ns/1.0",
        "xml=http://www.w3.org/XML/1998/namespace"
      ]
    },
    "Filters": [{ "Id": "struct-filter.standard" }]
  },
  {
    "Id": "structure-parser.xml-sentence",
    "Options": {
      "RootXPath": "/tei:TEI/tei:text/tei:body",
      "StopTags": ["head"],
      "Namespaces": ["tei=http://www.tei-c.org/ns/1.0"]
    }
  }
],
```

>As you can see from the XPath expressions used to select structures, some of them also rely on the value of attributes to detect a specific token type. For instance, `persName` has `@type`=`mn` for male name, `fn` for female name, `s` for surname. So, by using different mappings we can preserve such finer distinctions for the index.

(8) **text retriever**: a file-system based text retriever (`text-retriever.file`) is all what we need to get the text from their source. In this case, the source is a directory, and each text is a file.

```json
"TextRetriever": {
  "Id": "text-retriever.file"
}
```

>Note that this retriever is used to *build* the index. Once the index has been built, you can continue using it to retrieve the texts from their original source. If instead you chose to include the texts in the index itself, you can then replace this retriever with another targeting a RDBMS. In our case, this will be `text-retriever.sql.pg` (for PostgreSQL). This is the preferred choice as it makes the database self-contained and the index independent from the host file system. In this case, you will just replace the ID of the text retriever after indexing all the files.

(9) **text mapper**: a map generator for XML documents (`text-mapper.xml`), based on the specified paths on the document tree. Here we just pick the body element as the map's root node, and its children `p` elements as children nodes, as in these documents they represent the only available divisions.

```json
"TextMapper": {
  "Id": "text-mapper.xml",
  "Options": {
    "Definitions": [
      {
        "Name": "body",
        "XPath": "/tei:TEI/tei:text/tei:body",
        "ValueTemplate": "act"
      },
      {
        "Name": "p",
        "ParentName": "body",
        "XPath": "./tei:p",
        "DefaultValue": "paragraph",
        "ValueTemplate": "{t}",
        "ValueTemplateArgs": [
          { "Name": "t", "Value": "." }
        ],
        "ValueMaxLength": 60,
        "ValueTrimming": true,
        "DiscardEmptyValue": true
      }
    ],
    "Namespaces": ["tei=http://www.tei-c.org/ns/1.0"],
    "DefaultNsPrefix": "tei"
  }
},
```

As paragraphs usually have a long text, we cut it at maximum 60 characters, while also normalizing all their whitespaces. This way they will serve as nodes in the document map, allowing users to browse across the document's content.

(10) **text picker**: the component in charge of extracting a somehow semantically meaningful portion from a text. Here we use a picker for XML documents (`text-picker.xml`). As the default namespace in these TEI documents is the TEI namespace, we must notify the picker about it using `DefaultNsPrefix`, which defines the default namespace prefix. In turn, this is mapped to the TEI namespace in `Namespaces`, among other namespaces found in the documents (like the XML namespace itself for attributes like `lang`). This way, even when the picker extracts a sub-portion of the original document like a single paragraph, it will be assigned the default TEI namespace defined in the root element.

```json
"TextPicker": {
  "Id": "text-picker.xml",
  "Options": {
    "HitOpen": "<hi rend=\"hit\">",
    "HitClose": "</hi>",
    "Namespaces": [
      "tei=http://www.tei-c.org/ns/1.0",
      "xml=http://www.w3.org/XML/1998/namespace"
    ],
    "DefaultNsPrefix": "tei"
  }
},
```

(11) **text renderer**: the component which renders the document into some presentational format, here from XML to HTML, using a specified XSLT script.

```json
"TextRenderer": {
  "Id": "text-renderer.xslt",
  "Options": {
    "Script": "c:\\users\\dfusi\\desktop\\atti\\read.xsl",
    "ScriptRootElement": "{http://www.tei-c.org/ns/1.0}body"
  }
}
```

> 🛠️ Technical note: in this script the XML document gets transformed into HTML, referring to external [CSS styles](./example/read.css). It would not be possible to embed styles in the output, as in the frontend the output body gets extracted from the full HTML output, and dynamically inserted in the UI page. So, should you embed styles in the HTML header, they would just be dropped. The correct approach is rather defining global styles for your frontend app; these styles will then be automatically applied to the HTML code generated by the renderer. The XSLT script here wraps the text output into an `article` element with class `rendition`, so that all the styles selecting `article.rendition` are meant to be applied to the text renderer output.

### 2. Create Database

(1) use the pythia CLI to create a Pythia database, named `pythia`:

```ps1
./pythia create-db -c
```

(the `-c`lear option ensures that you start with a blank database should the database already be present, so you can repeat this command later if you want to reset the database and start from scratch).

(2) add the profile to this database (here I am placing the files under my desktop in a folder named `ac`; change the directory names as required for your own machine):

```ps1
./pythia add-profiles c:\users\dfusi\desktop\ac\atti-chiari.json
```

### 3. Index Files

Index the XML documents (here too, change the names as required):

```ps1
./pythia index atti-chiari c:\users\dfusi\desktop\ac\*.xml -o
```

If you want to run a preflight indexing before modifying the existing index, add the `-d` (=dry run) option.

Also, option `-o` stores the content of each document into the index itself, so that we can later retrieve it by just looking at the index. To this end, once you have indexed the files you can adjust the profile so that texts are retrieved from the database:

(5) adjust the **profile** for production, by replacing the text retriever ID and text renderer script in the database profile:

```ps1
./pythia add-profiles c:\users\dfusi\desktop\ac\atti-chiari-prod.json -i atti-chiari
```

>Note the `-i atti-chiari` option, which assigns the ID `atti-chiari` to the profile loaded from file `atti-chiari-prod.json`. This has the effect of overwriting the profile with the new one, rather than automatically assigning an ID based on the source file name (which would result in adding a new profile with ID `atti-chiar-prod`).

This replaces profile `atti-chiari` with the one from file `atti-chiari-prod.json`, which was derived from the above illustrated `atti-chiari.json`, by applying the following changes:

- you use the *SQL-based retriever* which will get the document's content from the database rather than from the file system:

```json
"TextRetriever": {
  "Id":"text-retriever.sql.pg"
}
```

instead of `"TextRetriever":{"Id":"text-retriever.file"}`.

- you *embed the XSLT script* in the text renderer options rather than loading it from a file. To this end, copy the XSLT code, minify it (just to make it more compact), and paste it into the `Script` option replacing the XSLT file path, e.g.:

```json
"TextRenderer": {
  "Id": "text-renderer.xslt",
  "Options": {
    "Script": "...PASTE_HERE_YOUR_XSLT_CODE...",
    "ScriptRootElement": "{http://www.tei-c.org/ns/1.0}body"
  }
}
```

>Note that before pasting into JSON you must first escape any `"` as `\"` to avoid JSON syntax errors!

These changes make the database contents independent from their hosting environment, because no more references to the file system are required.

(6) build the **word index**:

```bash
./pythia index-w -c date_value=3 -x data -x path
```

(7) if you want to bulk export your database tables in a format ready to be automatically picked up and restored by the Pythia API, run the `bulk-write` command:

```ps1
./pythia bulk-write c:\users\dfusi\desktop\ac\bulk
```
