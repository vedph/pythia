# Atti Chiari

The Atti Chiari corpus is a Pythia index built from a set of pseudonymized TEI documents from Italian legal acts. In the source folder, each act has two files:

- a TEI document.
- a metadata file in the same directory, with the same name plus `.meta` added at its end. This is a CSV file.

So, for instance file `civ-fi-app-cit342-000000_01.xml` has metadata `civ-fi-app-cit342-000000_01.xml.meta`.

## Pipeline Configuration

The full configuration document for the configuration of the Atti Chiari corpus follows, preceded by a descriptive summary:

- **source collector**: a file-based source collector collects source document for indexing, by simply scanning an input folder.
- **text filters**: these filters preprocess the texts being read before indexing them:
  - the XML local tag list is used to extracts a list of entries, one for each of the tags found in the text and listed in the filter's options (here `abbr` and `num`). Each entry has the tag name and position in the document. Entries are stored in context data under, while the text is not changed at all. This is used to store these positions for later use, as `abbr` and `num` TEI elements represent abbreviations and numbers and we want to ensure these are tagged as such in the index.
  - the XML tag filler is used to blank-fill with spaces all the matching tags with their content. As TEI documents use `choice` elements including `abbr` and `expan`, we want to blank-fill all the `expan`'s to avoid indexing them, as these are just expansions of the abbreviations found in the text. So, this text filter replaces `expan` elements and all their content with spaces, thus effectively removing them from indexed text, while keeping offsets and document's length unchanged.
  - the TEI filter is used to blank-fill with spaces the whole TEI header and each tag in the document. This effectively converts the document into plain text, while still preserving the positions and offsets of each character. Note that before applying this filter we have used tohe XML local tag list to extract information about TEI `abbr` and `num` elements.
  - the replacer text filter is used for a specific correction which replaces uppercase E followed by single quote with accented E and space.

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
        "Model": "italian-isdt-ud-2.12-230717",
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
