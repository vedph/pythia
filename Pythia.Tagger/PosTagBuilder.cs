using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pythia.Tagger;

/// <summary>
/// Part of speech full tag builder.
/// </summary>
public class PosTagBuilder : PosTag
{
    /// <summary>
    /// The profile to use for building full part of speech tags. For each
    /// POS tag, represented by a key in this dictionary, the value is an
    /// array of feature keys in the desired order to build the full tag.
    /// </summary>
    public Dictionary<string, string[]> Profile { get; } = [];

    /// <summary>
    /// Separator between part of speech and features.
    /// </summary>
    public string PosSeparator { get; set; } = ":";

    /// <summary>
    /// Separator between features.
    /// </summary>
    public string FeatSeparator { get; set; } = "|";

    /// <summary>
    /// True to prepend the part of speech key to each feature value.
    /// </summary>
    public bool PrependKey { get; set; } = true;

    /// <summary>
    /// Load the profile from a text reader. The profile is expected to be a
    /// CSV-like format where each line contains a part of speech tag followed
    /// by a comma and a list of feature keys, separated by commas.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <exception cref="ArgumentNullException">reader</exception>
    public void LoadProfile(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        Profile.Clear();

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] parts = line.Split(',');
            if (parts.Length < 2) continue;

            string pos = parts[0];
            string[] features = parts[1..];
            Profile[pos] = features;
        }
    }

    /// <summary>
    /// Build the full part of speech tag from the specified part of speech
    /// and features.
    /// </summary>
    /// <returns>Tag or null if no POS.</returns>
    /// <summary>
    /// Build the full part of speech tag from the specified part of speech
    /// and features.
    /// </summary>
    /// <returns>Tag or null if no POS.</returns>
    public string? Build()
    {
        if (string.IsNullOrEmpty(Pos)) return null;

        StringBuilder sb = new(Pos);
        bool firstFeature = true;

        if (Features.Count > 0)
        {
            if (Profile.TryGetValue(Pos, out string[]? keys))
            {
                if (PrependKey)
                {
                    // keyed mode with profile - only append features that exist
                    foreach (string key in keys)
                    {
                        if (Features.TryGetValue(key, out string? value))
                        {
                            sb.Append(firstFeature ? PosSeparator : FeatSeparator);
                            sb.Append(key).Append('=').Append(value);
                            firstFeature = false;
                        }
                    }
                }
                else
                {
                    // positional mode with profile - respect positions,
                    // can have multiple separators
                    sb.Append(PosSeparator);
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (i > 0) sb.Append(FeatSeparator);
                        if (Features.TryGetValue(keys[i], out string? value))
                        {
                            sb.Append(value);
                        }
                    }
                }
            }
            else
            {
                // no profile - use alphabetical order of keys
                foreach (KeyValuePair<string, string> kvp in
                    Features.OrderBy(f => f.Key))
                {
                    // only include non-empty feature values
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        sb.Append(firstFeature ? PosSeparator : FeatSeparator);
                        if (PrependKey) sb.Append(kvp.Key).Append('=');
                        sb.Append(kvp.Value);
                        firstFeature = false;
                    }
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parse a full part of speech tag from the specified built text.
    /// </summary>
    /// <param name="text">Text with the same format as the one built by
    /// <see cref="Build"/>.</param>
    /// <returns>Parsed tag or null.</returns>
    public PosTag? Parse(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        int posEnd = text.IndexOf(PosSeparator);
        if (posEnd < 0) return new PosTag { Pos = text };

        string pos = text[..posEnd];
        string[] features = text[(posEnd + 1)..].Split(
            FeatSeparator, StringSplitOptions.RemoveEmptyEntries);

        // create a new PosTag with the parsed pos
        PosTag posTag = new() { Pos = pos };

        // if there are features to parse
        if (features.Length > 0)
        {
            // if PrependKey is true, look for key=value format
            if (PrependKey)
            {
                foreach (string feature in features)
                {
                    int equalPos = feature.IndexOf('=');
                    if (equalPos > 0)
                    {
                        string key = feature[..equalPos];
                        string value = feature[(equalPos + 1)..];
                        posTag.Features[key] = value;
                    }
                    else
                    {
                        // = is not present despite PrependKey being true:
                        // in this case, treat the whole feature as a value
                        posTag.Features[feature] = feature;
                    }
                }
            }
            // else if Profile is not empty, use it to deduce keys from positions
            else if (Profile.TryGetValue(pos, out string[]? keys))
            {
                for (int i = 0; i < features.Length && i < keys.Length; i++)
                    posTag.Features[keys[i]] = features[i];
            }
        }

        return posTag;
    }
}
