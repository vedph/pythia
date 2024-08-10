# Example

- [Example](#example)
  - [Corpus Description](#corpus-description)
  - [Procedure](#procedure)
    - [Profile](#profile)
      - [Source](#source)
      - [Text Filters](#text-filters)
      - [Document Metadata](#document-metadata)
      - [Tokens](#tokens)
      - [Structures](#structures)
      - [Text Reading](#text-reading)
    - [Indexing](#indexing)
  - [Inspecting Index](#inspecting-index)

## Corpus Description

In this example we will create a Pythia index from scratch, starting from a couple of very short and simple TEI documents. The files for this example are listed here:

- [Catullus](./example/catull-084.xml)
- [Horatius](./example/hor-carm1.xml)
- [profile](./example/example.json)
- [sample XSLT](./example/read.xsl)
- [sample CSS for XSLT](./example/read.css)

>For this example, I assume that profile and XML files to be indexed are located in my Windows desktop folder `c:\users\dfusi\desktop\pythia`. Just replace the directory name with yours in the following command lines.

The sample documents have their default namespace equal to the TEI namespace (`xmlns="http://www.tei-c.org/ns/1.0"`).

We want to get these metadata from their header:

- author (from `fileDesc/titleStmt/author`)
- title (from `fileDesc/titleStmt/title/title`)
- category (from `fileDesc/titleStmt/title/@type`)
- date (from `fileDesc/titleStmt/title/date`)
- date value (from `fileDesc/titleStmt/title/date`: a number at its end between brackets)

As for the text, it's very simple:

- `body` contains some `div`'s.
- each `div` usually has a `@type` and a number (`@n`).
- usually a title is found inside the `div`, in the `head` element.
- the `div` contains either `lg` (strophes) with `l` (verses), or just `l` elements. Verses typically have `@met` (metre) and `@n`.
- other elements are `geogName`, `persName`, and `quote`.

## Procedure

Our procedure will follow these 3 main steps:

1. define a profile for the Pythia system.
2. create the database for the index, adding the profile in it.
3. index the files.

### Profile

The indexing behavior is fully defined in the profile, which is just a JSON file. You can write it with your favorite text/code editor (mine is VSCode). Here I dissect it to explain its main sections.

#### Source

🎯 Define the source for our documents.

(1) **source**: the source for documents is just the file system. So we use a `source-collector.file`:

```json
"SourceCollector": {
  "Id": "source-collector.file",
  "Options": {
    "IsRecursive": false
  }
},
```

>`Options` here is redundant as `IsRecursive` is false by default. Yet, it is useful to insert it because it shows you what are some relevant options you might want to use.

#### Text Filters

🎯 Define some essential filtering to preprocess our documents or prepare for further processing later (like for UDP).

(2) **text filters**:

- `text-filter.tei` is used to discard XML tags and the whole TEI header while indexing the text.
- `text-filter.quotation-mark` is used to ensure that apostrophes are handled correctly, by replacing "smart quotes" with apostrophe character codes proper. Using smart quotes instead of apostrophe is against Unicode semantics and might interfere with tokenizers and filters.
- `text-filter.upd` is used to add POS tags to the indexed words via [UDPipe](https://lindat.mff.cuni.cz/services/udpipe/). See [UDP Integration](udp.md) for more.

```json
"TextFilters": [
  {
    "Id": "text-filter.tei"
  },
  {
    "Id": "text-filter.quotation-mark"
  },
  {
    "Id": "text-filter.udp",
    "Options": {
      "Model": "latin-ittb-ud-2.12-230717",
      "MaxChunkLength": 5000,
      "ChunkTailPattern": "(?<![0-9])[.?!](?=\\s|$)"
    }
  }
],
```

>As usual, Latin taggers are not perfect, but we don't care about some errors in our demo.

#### Document Metadata

🎯 Extract document metadata from their text.

(3) **attribute parsers**:

- `attribute-parser.xml` is used to extract metadata from the TEI header.

```json
"AttributeParsers": [
  {
    "Id": "attribute-parser.xml",
    "Options": {
      "Mappings": [
        "author=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:author",
        "title=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:title/tei:title",
        "category=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:title/@type",
        "date=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:title/tei:date",
        "date-value=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:title/tei:date [N] \\((-?\\d+)\\)\\s*$"
      ],
      "DefaultNsPrefix": "tei",
      "Namespaces": [
        "tei=http://www.tei-c.org/ns/1.0"
      ]
    }
  }
],
```

Here, `author`, `title` and `date` just get their value from the element's content; `category` gets its value from an element's attribute (`@type`); `date-value` parses its target element content as a numeric value (`[N]`), using a regular expression. This expression is anchored to the end of the element's text, and matches an integer number, either positive or negative, between brackets, eventually followed by whitespaces.

So in the end we will get from the TEI header the _document_ attributes named `author`, `title`, `date`, `date-value` (numeric), and `category`.

>Note that `date-value` is defined here as the numeric value extracted from the date in the document's content. This is different from [privileged attribute](model.md#objects-and-attributes) `date_value` (underscore), and it is used as the source for it. So in the end we will just use the privileged attribute rather than this.

(4) **document sort key builder**: the standard sort key builder (`doc-sortkey-builder.standard`) is fine for this example. It sorts documents by author, title, and date.

```json
"DocSortKeyBuilder": {
  "Id": "doc-sortkey-builder.standard"
},
```

(5) **document date value calculator**: the standard calculator which just gets the value from metadata (`doc-datevalue-calculator.standard`) can be used here, as we get the value from the TEI header.

```json
"DocDateValueCalculator": {
  "Id": "doc-datevalue-calculator.standard",
  "Options": {
    "Attribute": "date-value"
  }
},
```

>Here the attribute name expected to hold the numeric date value is `date-value`, just like the attribute to be extracted by the XML attribute parser at nr.3 above. So, this parser will extract the attribute from the TEI header, and then the calculator will use it.

#### Tokens

🎯 Tokenize text.

(6) **tokenizer**: we use here a standard tokenizer (`tokenizer.standard`) which splits text at whitespace or apostrophes (while keeping the apostrophes with the token). Its **filters** are:

- `token-filter.punctuation`: this adds punctuation metadata to the indexed words.
- `token-filter.udp`: this adds UDPipe analysis results to the indexed words.
- `token-filter.ita`: this is properly for Italian, but here we can use it for Latin too, as it just removes all the characters which are not letters or apostrophe, strips from them all diacritics, and lowercases all the letters.
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

#### Structures

🎯 Extract textual structures larger than tokens.

(7) **structure parsers**:

- `structure-parser.xml`: a general-purpose filter for XML documents, used to extract a number of different structures corresponding to main text divisions (TEI `div`), paragraphs, strophes, verses, and quotes. It is also used to extract a couple of "ghost" structures, i.e. text spans which are not to be indexed as such, but can be used to extract more metadata for the tokens they include. This happens for `persName` and `geogName`, so that we can add metadata to the corresponding word(s) telling us that they are anthroponyms or choronyms. This will allow us searching for tokens which are e.g. person names, or geographic names, etc. Finally, the structure parser also uses a standard structure filter (`struct-filter.standard`) to properly extract their names from the original text by stripping out rumor features (diacritics, case, etc.).

- `structure-parser.xml-sentence`: a sentence parser. This relies on punctuation (and some XML tags) to determine the extent of sentences in the document.

```json
"StructureParsers": [
  {
    "Id": "structure-parser.xml",
    "Options": {
      "Definitions": [
        {
          "Name": "div",
          "XPath": "/tei:TEI/tei:text/tei:body/tei:div",
          "ValueTemplate": "{n}{$_}{head}",
          "ValuteTemplateArgs": [
            {
              "Name": "n",
              "Value": "./@n"
            },
            {
              "Name": "head",
              "Value": "./head"
            }
          ]
        },
        {
          "Name": "lg",
          "XPath": "//tei:lg",
          "ValueTemplate": "{n}",
          "ValuteTemplateArgs": [
            {
              "Name": "n",
              "Value": "./@n"
            }
          ]
        },
        {
          "Name": "l",
          "XPath": "//tei:l",
          "ValueTemplate": "{n}",
          "ValuteTemplateArgs": [
            {
              "Name": "n",
              "Value": "./@n"
            }
          ]
        },
        {
          "Name": "quote",
          "XPath": "//tei:quote",
          "ValueTemplate": "1",
          "TokenTargetName": "q"
        },
          {
            "Name": "persName",
            "XPath": "//tei:persName",
            "ValueTemplate": "{t}",
            "ValuteTemplateArgs": [
              {
                "Name": "t",
                "Value": "./text()"
              }
            ],
            "TokenTargetName": "pn"
          },
          {
            "Name": "geogName",
            "XPath": "//tei:geogName",
            "ValueTemplate": "{t}",
            "ValuteTemplateArgs": [
              {
                "Name": "t",
                "Value": "./text()"
              }
            ],
            "TokenTargetName": "gn"
          }
      ],
      "Namespaces": [ "tei=http://www.tei-c.org/ns/1.0" ]
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
      "RootPath": "tei:TEI//tei:body",
      "StopTags": [ "head" ],
      "Namespaces": [ "tei=http://www.tei-c.org/ns/1.0" ]
    }
  }
],
```

The XML structure parser uses a number of structure definitions for the text spans contained by XML elements `div`, `lg`, `l`, `quote`, `persName`, and `geogName`. Each structure type gets a name, which here to make things more readable is equal to the XML element; but this is just a convention, not a requirement. In detail:

```json
{
  "Name": "div",
  "XPath": "/tei:TEI/tei:text/tei:body/tei:div",
  "ValueTemplate": "{n}{$_}{head}",
  "ValuteTemplateArgs": [
    {
      "Name": "n",
      "Value": "./@n"
    },
    {
      "Name": "head",
      "Value": "./head"
    }
  ]
},
```

defines a structure type named `div`, covering all the text spans contained by all the `div` elements which are direct children of the root `body` element (thus building the path `/TEI/text/body/div`). These structures will be named `div`, and each will get a value equal to its target element's attribute `n` and/or its target element's child element `head`, separated by a space.

Then, `lg` and `l` have a similar definition, like:

```json
{
  "Name": "lg",
  "XPath": "//tei:lg",
  "ValueTemplate": "{n}",
  "ValuteTemplateArgs": [
    {
      "Name": "n",
      "Value": "./@n"
    }
  ]
}
```

where a structure named `lg` is defined for each `lg` element, whatever its position in the document. Its value is equal to the value of its `n` attribute.

Finally there are a number of _ghost structures_, used to provide additional metadata to the tokens they include. For instance:

```json
{
  "Name": "persName",
  "XPath": "//tei:persName",
  "ValueTemplate": "{t}",
  "ValuteTemplateArgs": [
    {
      "Name": "t",
      "Value": "./text()"
    }
  ],
  "TokenTargetName": "pn"
}
```

where a ghost structure `persName` is defined for all the `persName` elements, whatever their position in the document. The presence of `TokenTargetName` tells Pythia that this is a ghost structure, and that all the tokens contained in it will get an attribute with name = `pn`; the value will be that of the structure, as defined by its value template. In this case we want to use the person's name itself as the value, so the template has a single argument `t` corresponding to the inner element's text. Note that as for any of the values this will be filtered (by `struct-filter-standard`), which ensures that in the end we get a more normalized form (e.g. without diacritics or case differences).

As for quotations instead, the ghost structure named `quote` is similar, but it just provides a constant value (`1`) for its tokens:

```json
{
  "Name": "quote",
  "XPath": "//tei:quote",
  "ValueTemplate": "1",
  "TokenTargetName": "q"
}
```

This is because we are not interested in the single words of a quotation, but we just want to mark a quoted word as "quoted". The corresponding attribute for its token will thus be named `q` and have a constant value of `1`, which mimicks a typical boolean value, and just tells us that this token appears inside a quotation.

So, as you can see here these structures allow to get either more entities (the text spans) or more metadata for some of the tokens. In both cases they often draw their data from the XML markup; but a key difference is that there is no limit to the structures detected, which can freely overlap. In fact, here we are detecting and indexing structures which are not compatible and often overlap, like sentences and verses or other colometric structures. Also, here we are dealing with XML documents and thus using components built for them; but the same detection could be done using any other digital format. Whatever the source details and format, we are extracting and consolidating all the information from both the text and its metatextual data into a single index, with a uniform modeling.

In fact, to detect sentences we use a specialized _sentences detector_ which combines its own logic with eventual information coming from XML markup. This is because the logic is based on punctuation, but in some cases it is the markup only which delimits what can be considered a single sentence. This happens e.g. for a title in a `head` element, which usually does not end with a punctuation, but can be treated as a single sentence, separate from what follows. This is why there are a number of "stop tags" (here including `head`). A stop tag is an XML tag implying a sentence stop when closed. This way, even though a title does not end with a sentence end marker punctuation, we can treat it as a single, independent sentence.

Also, not all the text content in the XML document needs to be processed for sentence detection: for instance in a TEI document we must exclude the header. So, the detector also provides a root path to start its processing from, which in our sample is the TEI `body` element.

#### Text Reading

🎯 Define components to use for reading texts in the search environment.

(8) **text retriever**: a file-system based text retriever (`text-retriever.file`) is all what we need to get the text from their source. In this case, the source is a directory, and each text is a file.

```json
"TextRetriever": {
  "Id": "text-retriever.file"
}
```

>Note that this retriever is used to _build_ the index. You can continue using it also once the index has been built, to retrieve the texts from their original source. If instead you chose to include the texts in the index itself (which is recommended to provide a self-contained database), you can then replace this retriever with another targeting a RDBMS. In our case, this will be `text-retriever.sql.pg` (for PostgreSQL).

(9) **text mapper**: a map generator for XML documents (`text-mapper.xml`), based on the specified paths on the document tree. Here we just pick the body element as the map's root node, and its children `div` elements as children nodes, as in these documents they represent the major divisions (poems).

```json
"TextMapper": {
  "Id": "text-mapper.xml",
  "Options": {
    "Definitions": [
      {
        "Name": "root",
        "XPath": "/tei:TEI/tei:text/tei:body",
        "ValueTemplate": "document"
      },
      {
        "Name": "poem",
        "ParentName": "root",
        "XPath": "./tei:div",
        "DefaultValue": "poem",
        "ValueTemplate": "{type}{$_}{n}",
        "ValueTemplateArgs": [
          {
            "Name": "type",
            "Value": "./@type"
          },
          {
            "Name": "n",
            "Value": "./@n"
          }
        ]
      }
    ],
    "Namespaces": [
      "tei=http://www.tei-c.org/ns/1.0"
    ],
    "DefaultNsPrefix": "tei"
  }
}
```

Here, the first definition refers to the root node, and the second to the children nodes. The root node has a constant value, while children nodes get their value from the document, extracting the `div`'s `type` and/or `n` attributes.

(10) **text picker**: the component in charge of extracting a somehow semantically meaningful portion from a text. Here we use a picker for XML documents (`text-picker.xml`).

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
}
```

(11) **text renderer**: the component which renders the document into some presentational format, here from XML to HTML, using a specified XSLT script.

```json
"TextRenderer": {
  "Id": "text-renderer.xslt",
  "Options": {
    "Script": "c:\\users\\dfusi\\desktop\\pythia\\read.xsl",
    "ScriptRootElement": "{http://www.tei-c.org/ns/1.0}body"
  }
}
```

>🛠️ Technical note: in this script the XML document gets transformed into HTML, referring to external [CSS styles](./example/read.css). It would not be possible to embed styles in the output, as in the frontend the output body gets extracted from the full HTML output, and dynamically inserted in the UI page. So, should you embed styles in the HTML header, they would just be dropped. The correct approach is rather _defining global styles for your frontend app_; these styles will then be automatically applied to the HTML code generated by the renderer. The XSLT script here wraps the text output into an article element with class `rendition`, so that all the styles selecting `article.rendition` are meant to be applied to the text renderer output.

### Indexing

>💡 The full indexing procedure can be executed at once using a batch script, like the [example provided for Windows](./example/go.bat). In this script the database name is rather `pythia-demo`.

▶️ (1) use the pythia CLI to **create a Pythia database**, named `pythia`:

```bash
./pythia create-db pythia -c
```

>The `-c`lear option ensures that you start with a blank database should the database already be present, so you can repeat this command later if you want to reset the database and start from scratch.

▶️ (2) **add the profile** to this database:

```bash
./pythia add-profiles c:/users/dfusi/desktop/pythia/example.json pythia
```

▶️ (3) **index documents**:

```bash
./pythia index example c:/users/dfusi/desktop/pythia/*.xml pythia -o
```

>If you want to run a preflight indexing before modifying the existing index, add the `-d` (=dry run) option. Also, option `-o` stores the content of each document into the index itself as recommended, so that we can later retrieve it by just looking at the index.

⚠️ If using additional components in the profile, you must add a -t option with the tag name of their plugin, e.g. `-t factory-provider.chiron` for a Chiron-based Pythia factory provider to take advantage of the Latin phonology analyzer in Chiron. You should ensure that the corresponding plugin subfolder (`Pythia.Cli.Plugin.Chiron`) is present under the pythia CLI `plugins` folder.

▶️ (4) **build words index** in database. This builds [words and lemmata indexes](words.md) on top of text spans, also calculating their distributions in documents grouped according to their attributes:

```bash
./pythia index-w -d pythia-demo -c date-value=3 -c date_value=3 -x date
```

>The `-c` option specifies which document attributes are to be treated as numeric, while `-x` removes the `date` attribute from the distributions. This is because this attribute is just a human-friendly textual version of the date value so it would just add useless data to the index, as in most cases it will be a different string value for each document.

▶️ (5) **adjust the profile** for production, by replacing the text retriever ID and text renderer script in the database profile:

```bash
./pythia add-profiles c:/users/dfusi/desktop/pythia/example-prod.json -i example
```

>The `-i example` option assigns the ID `example` to the profile loaded from file `example-prod.json`. This has the effect of overwriting the profile with the new one, rather than automatically assigning an ID based on the source file name (which would result in adding a new profile with ID `example-prod`). The adjusted profile uses a database-based text retriever, so that texts are loaded from database rather than from the file system; and embeds the text rendition XSLT script in the text renderer options, rather than loading it from the file system. Both these changes make the database portable.

▶️ (6) _optionally_, if you want to **bulk export** your database tables in a format ready to be automatically picked up and restored by the Pythia API, run the `bulk-write` command:

```bash
./pythia bulk-write c:/users/dfusi/desktop/pythia/bulk
```

>💡 You can copy all the generated files in the `bulk` directory, and place them in some folder in your Docker host machine to have the API seed data on first startup.

## Inspecting Index

If you now inspect the index database, you can look at the results of the indexing process.

- `profile` contains our profile.

- there are 2 documents in `document`, one for each XML TEI file. As their source was the file system, their source property is just the file path. Metadata (author, title, date value, sort key) were got or calculated from document headers. Also, the full XML document is available in `content` as we requested this (option `-o`).

- category, date, and date value for each document can be found in `document_attribute`. These are attributes attached to documents.

- all the text spans are stored in `span`. Their language is not specified as we are handling single-language documents. The type of span tells whether they are tokens (`tok`) or larger structures. Here you will find:
  - `div`: major text division.
  - `l`: verse.
  - `lg`: strophe.
  - `snt`: sentence.

>Note that there are no structures for `persName`, `geogName`, or `quote`, because these were defined as ghost structures, whose only purpose is adding metadata to the _tokens_ they include. So, where are person and geographic metadata? They were defined for ghost structures, and thus set on tokens. If you inspect span metadata, you will find that effectively there are attributes named `pn`, `gn`, and `q`. Each of these, except `q` (whose value is the constant `1`), has as value the text content of the source XML element, filtered as required: for instance, `pn`=`arrius` and `gn`=`syriam`.

- additional metadata for each span are stored in `span_attribute`. For instance, the occurrence of `chommoda` has `len`=8 (8 characters).

⏭️ [simple example - Catullus](example-dump-1.md)
