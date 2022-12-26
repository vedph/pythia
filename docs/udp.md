# UDP Integration

A couple of plugins are provided for a quick integration of [UDPipe](https://lindat.mff.cuni.cz/services/udpipe/) POS taggers into the analysis system.

The main issue in integrating POS taggers into the complex Pythia [analysis flow](analysis.md) is that Pythia remains agnostic with respect to the input text format, and most times it analyzes marked text (e.g. XML) rather than plain text. Also, its flow is fully customizable, so your own tokenization algorithm might well be different from that adopted by the chosen UDPipe model.

As a recap, the figure below shows the Pythia's analysis components in their flow:

![components](img/components.png)

To solve these issues while still being compliant with its open and modular architecture, Pythia provides a couple of plugins designed to work together: the [UDP text filter](components.md#udp-text-filter) and the [UDP token filter](components.md#udp-token-filter).

The [UDP text filter](components.md#udp-text-filter) belongs to the family of text filters components, i.e. it's a filter applied once to the whole document, at the beginning of the analysis process. The purpose of this filter is not changing the document's text, but only submitting it to the UDPipe service, in order to get back POS tags for its content.

This submission may either happen at once, when text documents have a reasonable size; or may be configured to happen in chunks, so that POST requests to the UDPipe service have a smaller body. In this case, the text filter is designed in such a way to avoid splitting a sentence into different chunks (unless it happens to be longer than the maximum allowed chunk size, as configured in the profile).

Whatever the submission details, in the end this filter collects all the POS tags for the document's content. So, it's just a middleware component inserted at the beginning of the analysis flow, after some filters like the [XML filler filter](components.md#xml-tag-filler-text-filter) have been applied to 'neutralize' eventual markup (which of course must be excluded from UDPipe processing).

Later on, after the text has been tokenized, the [UDP token filter](components.md#udp-token-filter) comes into play. Its task is matching the token being filtered with the token (if any) defined by UDPipe, extract all the POS data from it, and store into the target index the subset of them specified by the analysis configuration.

Token matching happens in a rather mechanical way: as the filter has the character-based offset of the token being processed and its length, it just scans the POS data got by the UDP text filter and matches the first UDPipe token overlapping it. This is made possible by the fact that the text filter requested the POS data together with the offsets and extent of each token (passed via the CONLLU `Misc` field). So, whatever the original format of the document and the differences in tokenization, in most cases this produces the expected result and thus provides a quick way of incorporating UDPipe data in the index.

## Example

As a configuration example, consider this real-world profile.

This defines an analysis flow which applies _document filtering_ as follows:

1. draws TEI documents from the file system (`source-collector.file`);
2. collects the spans of elements with tag `abbr` or `num` (`text-filter.xml-local-tag-list`), as they will be consumed later to help the UDP chunker not being fooled by stops not corresponding to sentence ends;
3. blank fills all the XML `tei:expan` elements (which thus get removed from text, as they are expansions of abbreviations, i.e. inserted text; `text-filter.xml-tag-filler`);
4. blank-fills XML markup and the whole TEI header (`text-filter.tei`; this removes only tags, not their content);
5. normalizes quotation marks;
6. applies an UDPipe text filter to the resulting document. This filter uses a model for the Italian language, and while submitting the text to the service chunks it. Chunking happens by locating a regular expression corresponding to sentence end, but not inside elements `abbr` or `num`. Note that the text received by this filter has no tags at all (by virtue of (4)), but the data context of the filter carries the original elements positions collected at (2).

After this document filtering, some _attribute parsers_ are used to extract document metadata from some specific locations in the TEI header, and from an additional CSV file. This is because for security reasons more sensitive metadata are not kept in the TEI header, but rather isolated in an external file.

Then, a standard _tokenizer_ is used, which essentially splits text at whitespaces. This tokenizer has 3 filters, among which the UDPipe token filter, which extracts token metadata from the POS data previously collected by the UDPipe text filter.

Finally, a number of _structure filters_ are used to extract text structures corresponding to paragraphs (as they are relevant for the study of this kind of prose), sentences, and a number of "ghost" structures defined for the purpose of adding further metadata to the tokens they include (e.g. `foreign` to mark each token inside it as a foreign-language word, the main language of the document being Italian).

At the bottom of the configuration you find the components related to text _reading_: text retriever, mapper, passages picker, and renderer (used to convert the original TEI document into HTML via a XSLT transformation).

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
      "Id": "text-filter.quotation-mark"
    },
    {
      "Id": "text-filter.udp",
      "Options": {
        "Model": "italian-isdt-ud-2.10-220711",
        "MaxChunkLength": 5000,
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
        "SourceFind": "\\.xml$",
        "SourceReplace": ".xml.meta",
        "NameColumnIndex": 0,
        "ValueColumnIndex": 1,
        "ValueTrimming": true
      }
    }
  ],
  "DocSortKeyBuilder": {
    "Id": "doc-sortkey-builder.standard"
  },
  "DocDateValueCalculator": {
    "Id": "doc-datevalue-calculator.unix",
    "Options": {
      "Attribute": "act-date",
      "YmdPattern": "(?<y>\\d{4})(?<m>\\d{2})(?<d>\\d{2})",
      "YmdAsInt": true
    }
  },
  "Tokenizer": {
    "Id": "tokenizer.standard",
    "Options": {
      "TokenFilters": [
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
