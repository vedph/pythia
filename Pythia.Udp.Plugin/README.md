# Pythia.Udp.Plugin

Integrating UDPipe in Pythia requires two components: a text filter and a token filter. The text filter is responsible for submitting the document’s text to the UDPipe service and collecting the POS tags returned by it. The token filter is responsible for matching the tokens in the document with the tokens returned by UDPipe, and storing the relevant POS data in the index.

## Text Filter

The UDPipe text filter belongs to the family of text filters components, i.e. it's a filter applied once to the whole document, at the beginning of the analysis process. The purpose of this filter is not changing the document’s text, but only submitting it to the UDPipe service, in order to get back POS tags for its content. This submission usually does not happen at once, but rather in chunks, so that POST requests to the UDPipe service are granted a smaller body. In this case, the text filter is designed in such a way to avoid splitting a sentence into different chunks (unless it happens to be longer than the maximum allowed chunk size, as configured in the profile).

In the end this filter collects all the POS tags for the document's content. So, it's just a middleware component at the beginning of the pipeline, after some filters like the XML filler filter have been applied to 'neutralize' markup (which must be excluded from UDPipe processing).

## Token Filter

Later, after the text has been tokenized, the UDP token filter comes into play. Its task is matching the token being filtered with the token (if any) defined by UDPipe, extract all the POS data from it, and store into the target index the subset of them specified by the analysis configuration.

Token matching happens in a rather mechanical way: as the filter has the character-based offset of the token being processed and its length, it scans the POS data got by the UDP text filter and matches the first UDPipe token overlapping it. This is made possible by the fact that the text filter requested the POS data together with the offsets and extent of each token (passed via the CONLLU `Misc` field). So, whatever the original format of the document and the differences in tokenization, in most cases this produces the expected result.
