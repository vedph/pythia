# Real-World Example

This is a real-world example about using Pythia. Please note that the document rendition styles used in the demo frontend application (in `styles.css`) target this corpus.

## Corpus Description

The corpus sample here is from a research project collecting a number of Italian legal documents for linguistic analysis. These documents have already been anonymized by a [tool](https://ntplusdiritto.ilsole24ore.com/art/atti-chiari-chiarezza-e-concisione-scrittura-forense-AEya5kL) of my own, which replaces all the sensitive information with random data, according to a number of rules (more on this on an upcoming paper). The final outcome is a set of documents which look like real documents, and can thus be read fluently with no raw deletions or dumb replacements. Of course, this is a requirement for linguistic analysis, and the anonymizer also takes care of a number of details, like e.g. preserving the original usage of Italian euphonic /d/ by replacing the original forms with others beginning with the same letter.

Even if the tool is format-agnostic, in this project it directly ingests Word documents ([DOCX](https://en.wikipedia.org/wiki/Office_Open_XML), and produces as its final output XML TEI documents (plus their HTML renditions for diagnostic purposes).

These documents are then collected into a central, web-based repository using a [BLOB store](https://github.com/vedph/simple-blob), and then downloaded from it to be indexed with Pythia. In this example we will just provide some of these TEI documents to show how Pythia is used in a real-world project.

The TEI documents have a minimalist header, as no sensitive information should be found in them. So, all what the header includes is the original file name. Then, the document's body is the outcome of a conversion process starting from a Word document with no headings or other structural divisions, given the nature of these texts. As such, the `body` element just includes paragraphs. Some essential features of the original document styles are preserved in TEI, like bold, italic, underline, and text alignment. Also, all the portions processed by the anonymizer tool have been marked, e.g. as abbreviations, personal names, place names, juridic person names, dates, alphanumeric codes, numbers, foreign language parts, etc., using the proper TEI elements.

For instance, here is a sample paragraph:

```xml
<p rend="j"><hi rend="b"><orgName type="f">COMUNARDA</orgName> <choice><abbr>S.p.A.</abbr><expan xml:lang="ita">Società per Azioni</expan></choice></hi>, in persona del legale rappresentante <choice><abbr>p.t.</abbr><expan xml:lang="lat">pro tempore</expan></choice>, elettivamente domiciliata in <placeName>Leivi</placeName>, alla Via <address><addrLine>Yenna Vicenzo, 89</addrLine></address>, presso e nello studio dell’<choice><abbr>avv.</abbr><expan xml:lang="ita">avvocato</expan></choice> <persName type="mn">Almirante</persName> <persName type="s">Masuero</persName>, che la rappresenta e difende</p>
```

For security reasons, metadata about each document (even though not including any personal information) are stored in a separate file, which happens to be an Excel (XLSX) file having a single sheet with 2 columns. Each row in this file represents a name=value pair. For instance, here are some rows, here represented with CSV:

```txt
materia,civile
sede di raccolta,Lecce
organo giurisdizionale,Corte d’Appello
sede organo giurisdizionale,Lecce
atto,citazione
data,20100409
grado,2
```

We want to extract these metadata, remap them to shorter English names, and attach them to the corresponding document for indexing purposes. This will allow filtering documents by their metadata, and including these filters also in text searches.

## Procedure

Our procedure will follow these 3 main steps:

1. define a profile for the Pythia system.
2. create the database for the index, adding the profile in it.
3. index the files.

### 1. Profile

The profile is just a JSON file. You can write it with your favorite text/code editor (mine is VSCode).

(1) **source**: in this sample, the source for documents is just the file system, assuming that we have downloaded a few documents in some folder. So we use a `source-collector.file`:

```json
"SourceCollector": {
  "Id": "source-collector.file",
  "Options": {
    "IsRecursive": false
  }
},
```

(2) **text filters**:

- `text-filter.xml-tag-filler` is used to blank-fill the whole `expan` element as we do not want its text to be handled as document's text. In fact, the content of `expan` is just the expansion of an abbreviation in the text, so we just exclude it from indexing. In `Tags` we list all the tag names of elements to be filled. As `expan` belongs to the TEI namespace, we also have to add it among `Namespaces`, so that `tei:expan` gets correctly resolved.
- `text-filter.tei` is used to discard tags and header from the text index.
- `text-filter.quotation-mark` is used to ensure that apostrophes are handled correctly by replacing smart quotes with apostrophe character codes proper.

```json
"TextFilters": [
  {
    "Id": "text-filter.xml-tag-filler",
    "Options": {
      "Tags": ["tei:expan"],
      "Namespaces": ["tei=http://www.tei-c.org/ns/1.0"]
    }
  },
  { "Id": "text-filter.tei" },
  { "Id": "text-filter.quotation-mark" }
],
```

(3) **attribute parsers**:

- `attribute-parser.xml` is used to extract metadata from the TEI header. The only datum here is the document's title, because as explained above all the metadata are stored separately in an Excel file.
- `attribute-parser.fs-excel` is then used to parse the Excel file corresponding to the document file. This happens to have the same name of the document file, with the additional suffix `_hdr` and extension `.xlsx`. So, here we let the parser use the document's source itself as the source for metadata, while adding a regular expression-based replacement (in `SourceFind` and `SourceReplace`). Also, we tell the parser to look in the first sheet, using the first column for names and the second for values. Also, we want values to be trimmed to avoid accidentally typed spaces, and to remap all the names into something more compact and using English.

```json
"AttributeParsers": [
  {
    "Id": "attribute-parser.xml",
    "Options": {
      "Mappings": [
        "title=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:title"
      ],
      "DefaultNsPrefix": "tei",
      "Namespaces": ["tei=http://www.tei-c.org/ns/1.0"]
    }
  },
  {
    "Id": "attribute-parser.fs-excel",
    "Options": {
      "SourceFind": "\\.xml$",
      "SourceReplace": "_hdr.xlsx",
      "SheetIndex": 0,
      "NameColumnIndex": 0,
      "ValueColumnIndex": 1,
      "ValueTrimming": true,
      "NameMappings": [
        "materia=subject",
        "sede di raccolta=coll-place",
        "sede raccolta=coll-place",
        "organo giurisdizionale=court",
        "sede organo giurisdizionale=court-place",
        "atto=act-type",
        "data=act-date",
        "grado#=degree"
      ]
    }
  }
],
```

We are thus collecting metadata for documents from two different sources: the document itself (from its TEI header), and an independent Excel file.

(4) **document sort key builder**: the standard sort key builder (`doc-sortkey-builder.standard`) is fine. It sorts documents by author, title, and date.

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
    "Attribute": "act-date",
    "YmdPattern": "(?<y>\\d{4})(?<m>\\d{2})(?<d>\\d{2})",
    "YmdAsInt": true
  }
},
```

(6) **tokenizer**: we use here a standard tokenizer (`tokenizer.standard`) which splits text at whitespace or apostrophes (while keeping the apostrophes with the token). Its **filters** are:

- `token-filter.ita`: this is properly for Italian, but here we can use it for Latin too, as it just removes all the characters which are not letters or apostrophe, strips from them all diacritics, and lowercases all the letters.

- `token-filter.len-supplier`: this filter does not touch the token, but adds metadata to it, related to the number of letters of each token. This is just to have some fancier metadata to play with. This way you will be able to search tokens by their letters counts.

```json
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
      }
    ]
  }
},
```

(7) **structure parsers**:

- `structure-parser.xml`: a general purpose filter for XML documents, used to extract a number of different structures corresponding to main text divisions. Unfortunately, these documents have no intrinsic structure. We can only rely on paragraphs, so this is the only structure detected. Yet, there are a number of "ghost" structures, i.e. text spans which are not to be indexed as such, but can be used to extract more metadata for the tokens they include. This happens e.g. for `persName` and all the others listed below, so that we can add metadata to the corresponding word(s) telling us that they are anthroponyms or whatever else. This will allow us searching for tokens which are e.g. person names, or geographic names, etc. Finally, the structure parser also uses a standard structure filter (`struct-filter.standard`) to properly extract their names from the original text by stripping out rumor features (diacritics, case, etc.).

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

As you can see from the XPath expressions used to select structures, some of them also rely on the value of attributes to detect a specific token type. For instance, `persName` has `@type`=`mn` for male name, `fn` for female name, `s` for surname. By using different mappings we thus ensure to preserve such finer distinctions for the index.

(8) **text retriever**: a file-system based text retriever (`text-retriever.file`) is all what we need to get the text from their source. In this case, the source is a directory, and each text is a file.

```json
"TextRetriever": {
  "Id": "text-retriever.file"
}
```

Note that this retriever is used to build the index. You can continue using it also once the index has been built, to retrieve the texts from their original source. If instead you chose to include the texts in the index itself, you can then replace this retriever with another targeting a RDBMS. In our case, this will be `text-retriever.sql.pg` (for PostgreSQL).

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

Technical note: in this script the XML document gets transformed into HTML, referring to external [CSS styles](./example/read.css). It would not be possible to embed styles in the output, as in the frontend the output body gets extracted from the full HTML output, and dynamically inserted in the UI page. So, should you embed styles in the HTML header, they would just be dropped. The correct approach is rather defining global styles for your frontend app; these styles will then be automatically applied to the HTML code generated by the renderer. The XSLT script here wraps the text output into an article element with class `rendition`, so that all the styles selecting `article.rendition` are meant to be applied to the text renderer output.

### 2. Create Database

(1) use the pythia CLI to create a Pythia database, named `pythia`:

```ps1
./pythia create-db pythia -c
```

(the `-c`lear option ensures that you start with a blank database should the database already be present, so you can repeat this command later if you want to reset the database and start from scratch).

(2) add the profile to this database (here I am placing the files under my desktop in a folder named `ac`; change the directory names as required for your own machine):

```ps1
./pythia add-profiles c:\users\dfusi\desktop\ac\atti-chiari.json pythia
```

### 3. Index Files

Index the XML documents (here too, change the names as required):

```ps1
./pythia index atti-chiari c:\users\dfusi\desktop\ac\*.xml pythia -t factory-provider.xlsx -o
```

If you want to run a preflight indexing before modifying the existing index, add the `-d` (=dry run) option.

Also, option `-o` stores the content of each document into the index itself, so that we can later retrieve it by just looking at the index.