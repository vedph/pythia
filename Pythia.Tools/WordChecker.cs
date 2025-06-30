using Pythia.Tagger;
using Pythia.Tagger.Lookup;
using System;
using System.Collections.Generic;

namespace Pythia.Tools;

/// <summary>
/// Word form checker.
/// </summary>
public sealed class WordChecker
{
    private readonly ILookupIndex _index;
    private readonly IVariantBuilder _variantBuilder;
    private readonly PosTagBuilder _tagBuilder;

    public WordChecker(ILookupIndex index, IVariantBuilder variantBuilder,
        PosTagBuilder tagBuilder)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
        _variantBuilder = variantBuilder ??
            throw new ArgumentNullException(nameof(variantBuilder));
        _tagBuilder = tagBuilder
            ?? throw new ArgumentNullException(nameof(tagBuilder));
    }

    private WordCheckResult HandleFound(WordToCheck word,
        IList<LookupEntry> entries)
    {
        // ensure that at least one has a compatible POS
        // i.e. a POS with the same POS tag and any subset of features
        if (!string.IsNullOrEmpty(word.Pos))
        {
            PosTag wordTag = _tagBuilder.Parse(word.Pos)!;
            foreach (LookupEntry entry in entries)
            {
                PosTag? entryTag = _tagBuilder.Parse(entry.Pos);
                if (entryTag != null && wordTag.IsSubsetOf(entryTag))
                {
                    return new WordCheckResult(word,
                        WordCheckResultType.Info);
                }
            }
            // no entry with a compatible POS found
            return new WordCheckResult(word, WordCheckResultType.Error)
            {
                Message = $"No entry with POS '{word.Pos}' found for " +
                    $"'{word.Value}'"
            };
        }
        else
        {
            // no POS specified, so any entry is fine
            return new WordCheckResult(word, WordCheckResultType.Info);
        }
    }

    public IList<WordCheckResult> Check(WordToCheck word)
    {
        ArgumentNullException.ThrowIfNull(word);

        // find the word (without POS) in the index
        IList<LookupEntry> entries = _index.Lookup(word.Value);

        // if any found, ensure that at least one has a compatible POS
        // i.e. a POS with the same POS tag and any subset of features
        if (entries.Count > 0)
            return [HandleFound(word, entries)];

        List<WordCheckResult> results = [];

        // look for variants
        foreach (VariantForm v in _variantBuilder.Build(word.Value, word.Pos,
            _index))
        {
            entries = _index.Lookup(v.Value!, v.Pos);
            if (entries.Count > 0)
            {
                results.Add(new WordCheckResult(word,
                    WordCheckResultType.ErrorWithHint)
                {
                    Message = $"Variant '{v.Value}' found for " +
                        $"'{word.Value}' with POS '{v.Pos}'",
                    Action = "use-variant",
                    Data = new Dictionary<string, string>
                    {
                        { "variant", v.Value! },
                        { "pos", v.Pos! }
                    }
                });
            }
        }

        if (results.Count == 0)
        {
            // no variants found, so return an error
            results.Add(new WordCheckResult(word, WordCheckResultType.Error)
            {
                Message = $"No entry found for '{word.Value}'" +
                    (string.IsNullOrEmpty(word.Pos) ? "" :
                        $" with POS '{word.Pos}'")
            });
        }

        return results;
    }
}
