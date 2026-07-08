# Pythia.Udp.Plugin

Integrating UDPipe in Pythia requires two components: a text filter and a token filter. The text filter is responsible for submitting the document’s text to the UDPipe service and collecting the POS tags returned by it. The token filter is responsible for matching the tokens in the document with the tokens returned by UDPipe, and storing the relevant POS data in the index.

## Text Filters

### XML Tag Filler Text Filter

This generic filter is used to blank-fill text corresponding to specified XML elements. For instance a TEI `choice` element typically contains a child `abbr` and a child `expan` and we do NOT want to index the `expan` content because it is an editorial addition outside of the text. So before submitting the text to UDPipe we blank fill the whole `expan` element with its content.

### UDPipe Text Filter

The UDPipe text filter belongs to the family of text filters components, i.e. it's a filter applied once to the whole document, at the beginning of the analysis process. The purpose of this filter is not changing the document’s text, but only submitting it to the UDPipe service, in order to get back POS tags for its content. This submission usually does not happen at once, but rather in chunks, so that POST requests to the UDPipe service are granted a smaller body. In this case, the text filter is designed in such a way to avoid splitting a sentence into different chunks (unless it happens to be longer than the maximum allowed chunk size, as configured in the profile).

In the end this filter collects all the POS tags for the document's content. So, it's just a middleware component at the beginning of the pipeline, after some filters like the XML filler filter have been applied to 'neutralize' markup (which must be excluded from UDPipe processing).

## Token Filter

Later, after the text has been tokenized, the UDP token filter comes into play. Its task is matching the token being filtered with the token (if any) defined by UDPipe, extract all the POS data from it, and store into the target index the subset of them specified by the analysis configuration.

Token matching happens in a rather mechanical way: as the filter has the character-based offset of the token being processed and its length, it scans the POS data got by the UDP text filter and matches the first UDPipe token overlapping it. This is made possible by the fact that the text filter requested the POS data together with the offsets and extent of each token (passed via the CONLLU `Misc` field). So, whatever the original format of the document and the differences in tokenization, in most cases this produces the expected result.

A corner case in this filter is represented by **multiword tokens** like Italian "della" = "di" + "la".
For instance, consider this Italian sentence: "Questo è della casa.". The POS tagger analyzes this as follows:

| id  | form   | lemma  | UPos  | XPos | Feats                                                     | Head | DepRel | Deps | Misc                            |
| --- | ------ | ------ | ----- | ---- | --------------------------------------------------------- | ---- | ------ | ---- | ------------------------------- |
| 1   | Questo | questo | PRON  | PD   | Gender=Masc\|Number=Sing\|PronType=Dem                    | 5    | nsubj  | -    | TokenRange=0:6                  |
| 2   | è      | essere | AUX   | VA   | Mood=Ind\|Number=Sing\|Person=3\|Tense=Pres\|VerbForm=Fin | 5    | cop    | -    | TokenRange=7:8                  |
| 3-4 | della  | -      | -     | -    | -                                                         | -    | -      |      | TokenRange=9:14                 |
| 3   | di     | di     | ADP   | E    | -                                                         | 5    | case   | -    | -                               |
| 4   | la     | il     | DET   | RD   | Definite=Def\|Gender=Fem\|Number=Sing\|PronType=Art       | 5    | det    | -    | -                               |
| 5   | casa   | casa   | NOUN  | S    | Gender=Fem\|Number=Sing                                   | 0    | root   | -    | SpaceAfter=No\|TokenRange=15:19 |
| 6   | .      | .      | PUNCT | FS   | -                                                         | 5    | punct  | -    | SpaceAfter=No\|TokenRange=19:20 |

Here `della` is the multiword token (tokens 3-4) and its children (3 and 4) follow it.

In Pythia we need to get POS data for the single `della` token. Filters applied to this token know only about it; `di` and `la` are 'artifacts' from the POS tagger, while here we need to represent all the data on top of the original text, where only `della` exists.

The filter has been implemented to:

1. detect the multiword token (`della`);
2. collect its "children" tokens (`di` and `la`);
3. lookup the configuration provided in filter options to inject specific POS data into the token being processed depending on its children.

In most cases, we need to POS-tag the form as it appears on the text (like `della`) rather than its analysis (`di` + `la`), because we are here focusing on a text-based search which must reflect the document's text. Compare for instance the Morph-It list of Italian inflected forms, where POS tags are designed to fit this specific language, and forms like `della` are at the same level as words like `di` or `la`. In this case, the POS tag is `ART-F:s`, meaning article, feminine, singular.

In the same way, here for each typology of such composite forms we must decide which subset of POS data from its components should make their way into the POS tag of the surface form `della`:

- `di`: `ADP.E` (preposition)
- `la`: `DET.RD` (determinative, definite article), with additional features: `Def`, `Fem`, `Sing`, `Art`.

Here we will typically let the morphologically and syntactically richer component prevail, so that we treat the whole form `della` just like a `DET.RD`, ignoring its composite origin. Anyway, the decision will vary accoring to the typology of composition in the various forms presenting multi-word tokens. So, we need a generalized and configurable strategy to deal with them.

To provide a generic, reusable configuration to build a new lemma and tag by variously collecting data from the children tokens, you use the `Multiwords` option in the filter configuration object (see unit tests for an example). In the case of `della`, the configuration tells the filter to match tokens `ADP.E` followed by `DET.RO`, and provide the token's value as the lemma (which is the default behavior), plus UPOS, XPOS and features from the second token (`la`). The corresponding JSON configuration would be:

```json
{
  "Id": "token-filter.udp",
  "Options": {
    "Props": 43,
    "Language": "",
    "Multiwords": [
      {
        "MinCount": 2,
        "MaxCount": 2,
        "Tokens": [
          {
            "Upos": "ADP",
            "Xpos": "E"
          },
          {
            "Upos": "DET",
            "Xpos": "RD"
          }
        ],
        "Target": {
          "Upos": "DET",
          "Xpos": "RD",
          "Feats": {
            "*": "2"
          }
        }
      }
    ]
  }
},
```

This matches any 2-tokens multiword token having `ADP.E` for its first token and `DET.RD` as its second one. We might also add `Feats` to the filters, but that's not required here. The resulting tag for `della` is `DET.RD` and its `Feats` are copied from all the features of the second token. The lemma is just equal to the token's value (`della`), as this is the default when `Lemma` is not specified.
