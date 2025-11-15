# Atti Chiari

The Atti Chiari corpus is a Pythia index built from a set of pseudonymized TEI documents from Italian legal acts. In the source folder, each act has two files:

- a TEI document.
- a metadata file in the same directory, with the same name plus `.meta` added at its end. This is a CSV file.

So, for instance file `civ-fi-app-cit342-000000_01.xml` has metadata `civ-fi-app-cit342-000000_01.xml.meta`.

## Pipeline Configuration

The full configuration document for the configuration of the Atti Chiari corpus follows, preceded by a descriptive summary:

- **source collector**: a file-based source collector collects source document for indexing, by simply scanning an input folder.
- **text filters**: these filters preprocess the texts being read before indexing them:
  - the _XML local tag list_ is used to extracts a list of entries, one for each of the tags found in the text and listed in the filter's options (here `abbr` and `num`). Each entry has the tag name and position in the document. Entries are stored in context data under, while the text is not changed at all. This is used to store these positions for later use, as `abbr` and `num` TEI elements represent abbreviations and numbers and we want to ensure these are tagged as such in the index.
  - the _XML tag filler_ is used to blank-fill with spaces all the matching tags with their content. As TEI documents use `choice` elements including `abbr` and `expan`, we want to blank-fill all the `expan`'s to avoid indexing them, as these are just expansions of the abbreviations found in the text. So, this text filter replaces `expan` elements and all their content with spaces, thus effectively removing them from indexed text, while keeping offsets and document's length unchanged.
  - the _TEI filter_ is used to blank-fill with spaces the whole TEI header and each tag in the document. This effectively converts the document into plain text, while still preserving the positions and offsets of each character. Note that before applying this filter we have used tohe XML local tag list to extract information about TEI `abbr` and `num` elements.
  - the _replacer text filter_ is used for a specific correction which replaces uppercase E followed by single quote with accented E and space.
  - the _quotation mark text filter_ replaces U+2019 (right single quotation mark) with an apostrophe (U+0027) when it is included between two letters.
  - the _UDP filter_ is used to apply POS tagging. This specifies the UDPipe model to use (here an Italian model) and options for chunking the document (black tags specified as `abbr` and `num` are used to avoid the chunker split chunks inside these elements, which often include dots which do not represent sentence end, while the chunker strives to preserve sentence integrity to avoid POS issues). This filter will collect all the POS tags for the document, to be consumed later in the pipeline.
- **attribute parsers**: parsers which extract metadata (in the form of Pythia attributes, i.e. name=value pairs) from the TEI text and its CSV counterpart:
  - the XML attribute parser is used to extract the title from the TEI header.
  - the CSV attribute parser is used to extract further metadata from the CSV companion file of each TEI file. For security reasons, TEI documents do not have any relevant metadata; they are rather contained in these external CSV files.
- **document sort key builder**:
  - a standard document sort key builder is used to build a sort key for each document based on its title.
- **document date value calculator**:
  - a UNIX-date date value calculator is used to extract the date of each document from its `data` attribute.
- **tokenizer**: the tokenizer used to split text into tokens, with its filters to purge it or supplement it with additional metadata. The token filters are:
  - _punctuation token filter_: this adds metadata about the position of punctuation with reference to the token. Punctuation is stripped away from the token's value but metadata are added to preserve information about it (e.g. a token ending with a dot).
  - _UDP token filter_: this consumes the POS tags collected earlier by the UDP filter to assign POS tag and features to each detected token.
  - _Italian token filter_: this filters the token's value assuming Italian as its language.
  - _length supplier token filter_: this supplies the length of the token's value (its letters count) as an additional attribute.
- **structure parsers**: these parse textual structures based on 1 or more tokens from the text (e.g. sentences, Latin phrases, etc.):
  - the _XML structure parser_ refers to the original (unfiltered) TEI text and leverages XML markup to detect structures. Some of these structures are "ghost" structures, i.e. they are not used to store structures in the index, but only to add attributes to all the tokens building up them. In this case the `TokenTargetName` option specifies the name of the attribute to add, and `ValueTemplate` is the template of the attribute's value. The template contains named placeholders in braces, which are defined in `ValueTemplateArgs`. The configuration extracts these ghost structures and real structures (also using a standard structure value filter for its value):
    - _ghost structures_:
      - `abbr`=`1` for all tokens in TEI `abbr` element.
      - `address`=`1` for all tokens in TEI `address` element.
      - `date`=trimmed value of the TEI `date` element.
      - `email`=`1` for all tokens in TEI `email` element.
      - `foreign`=LANGUAGE for all tokens in TEI `foreign`, drawing LANGUAGE from its attribute `xml:lang`.
      - `b`=`1` for all tokens in TEI `hi` element with its `rend` attribute containing `b`.
      - `i`=`1` for all tokens in TEI `hi` element with its `rend` attribute containing `i`.
      - `u`=`1` for all tokens in TEI `hi` element with its `rend` attribute containing `u`.
      - `n`=`1` for all tokens in TEI `num` element.
      - `org-m`=trimmed value of the TEI `orgName` element with its `type` attribute equal to `m`.
      - `org-f`=trimmed value of the TEI `orgName` element with its `type` attribute equal to `f`.
      - `pn-m`=trimmed value of the TEI `persName` element with its `type` attribute equal to `mn`.
      - `pn-f`=trimmed value of the TEI `persName` element with its `type` attribute equal to `fn`.
      - `pn-s`=trimmed value of the TEI `persName` element with its `type` attribute equal to `s`.
      - `tn`=trimmed value of the TEI `placeName` element.
    - _real structures_:
      - `p`: paragraph. Its value is the length in characters.
      - `fp-lat`: Latin phrase. Its value is the trimmed text.
  - the _XML sentence structure parser_ detects sentence structures (span type = `snt`) in the XML document.
- **reading**: components used to read document's text:
  - _text retriever_: a file-based text retriever is used in indexing, because we need to load it from a file. The indexing process is configured to store the document's text in the index itself, so the "production" version of this configuration, designed to be used when searching the index, will rather use a PgSql-based retriever (`text-retriever.sql.pg`).
  - _text mapper_: the component used to build a navigatable map of the document. This is based on the XML structure, and uses paragraphs inside the TEI body as the map's entries.
  - _text picker_: the component used to pick a meaningful portion of the text from the document, usually to display the context of a search result.
  - _text renderer_: the component used to render the text for the end user. This uses an XSLT script to transform TEI into HTML.

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
        "Model": "italian-isdt-ud-2.15-241121",
        "MaxChunkLength": 5000,
        "ChunkTailPattern": "(?<![0-9])[.?!](?=\\s|$)",
        "BlackTags": [
          "abbr",
          "num"
        ]
      }
    }
  ],
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
  "DocSortKeyBuilder": {
    "Id": "doc-sortkey-builder.standard"
  },
  "DocDateValueCalculator": {
    "Id": "doc-datevalue-calculator.unix",
    "Options": {
      "Attribute": "data",
      "YmdPattern": "(?<y>\\d{4})(?<m>\\d{2})(?<d>\\d{2})",
      "YmdAsInt": true
    }
  },
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
            "Props": 43,
            "Language": ""
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
  "StructureParsers": [
    {
      "Id": "structure-parser.xml",
      "Options": {
        "Definitions": [
          {
            "Name": "p",
            "XPath": "/tei:TEI/tei:text/tei:body/tei:p",
            "ValueTemplate": "{l}",
            "ValueTemplateArgs": [
              {
                "Name": "l",
                "Value": "string-length(normalize-space(.))"
              }
            ]
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
            "ValueTemplateArgs": [
              {
                "Name": "t",
                "Value": "."
              }
            ],
            "ValueTrimming": true,
            "TokenTargetName": "date"
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
            "ValueTemplateArgs": [
              {
                "Name": "l",
                "Value": "./@xml:lang"
              }
            ],
            "TokenTargetName": "foreign"
          },
          {
            "Name": "fp-lat",
            "XPath": "//tei:foreign[@xml:lang='lat']",
            "ValueTemplate": "{txt}",
            "ValueTemplateArgs": [
              {
                "Name": "txt",
                "Value": "./text()"
              }
            ],
            "ValueTrimming": true
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
            "ValueTemplateArgs": [
              {
                "Name": "t",
                "Value": "."
              }
            ],
            "ValueTrimming": true,
            "TokenTargetName": "org-m"
          },
          {
            "Name": "org-name",
            "XPath": "//tei:orgName[@type='f']",
            "ValueTemplate": "{t}",
            "ValueTemplateArgs": [
              {
                "Name": "t",
                "Value": "."
              }
            ],
            "ValueTrimming": true,
            "TokenTargetName": "org-f"
          },
          {
            "Name": "pers-name",
            "XPath": "//tei:persName[@type='mn']",
            "ValueTemplate": "{t}",
            "ValueTemplateArgs": [
              {
                "Name": "t",
                "Value": "."
              }
            ],
            "ValueTrimming": true,
            "TokenTargetName": "pn-m"
          },
          {
            "Name": "pers-name",
            "XPath": "//tei:persName[@type='fn']",
            "ValueTemplate": "{t}",
            "ValueTemplateArgs": [
              {
                "Name": "t",
                "Value": "."
              }
            ],
            "ValueTrimming": true,
            "TokenTargetName": "pn-f"
          },
          {
            "Name": "pers-name",
            "XPath": "//tei:persName[@type='s']",
            "ValueTemplate": "{t}",
            "ValueTemplateArgs": [
              {
                "Name": "t",
                "Value": "."
              }
            ],
            "ValueTrimming": true,
            "TokenTargetName": "pn-s"
          },
          {
            "Name": "place-name",
            "XPath": "//tei:placeName",
            "ValueTemplate": "{t}",
            "ValueTemplateArgs": [
              {
                "Name": "t",
                "Value": "."
              }
            ],
            "ValueTrimming": true,
            "TokenTargetName": "tn"
          }
        ],
        "Namespaces": [
          "tei=http://www.tei-c.org/ns/1.0",
          "xml=http://www.w3.org/XML/1998/namespace"
        ],
        "PrivilegedMappings": {
          "foreign": "language"
        }  
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
        "RootXPath": "/tei:TEI/tei:text/tei:body",
        "StopTags": [
          "head"
        ],
        "NoSentenceMarkerTags": [
          "abbr",
          "num"
        ],
        "Namespaces": [
          "tei=http://www.tei-c.org/ns/1.0"
        ]
      }
    }
  ],
  "TextRetriever": {
    "Id": "text-retriever.file"
  },
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
            {
              "Name": "t",
              "Value": "."
            }
          ],
          "ValueMaxLength": 60,
          "ValueTrimming": true,
          "DiscardEmptyValue": true
        }
      ],
      "Namespaces": [
        "tei=http://www.tei-c.org/ns/1.0"
      ],
      "DefaultNsPrefix": "tei"
    }
  },
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
  "TextRenderer": {
    "Id": "text-renderer.xslt",
    "Options": {
      "Script": "c:\\users\\dfusi\\desktop\\ac\\read.xsl",
      "ScriptRootElement": "{http://www.tei-c.org/ns/1.0}body"
    }
  }
}
```
