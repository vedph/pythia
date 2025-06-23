using System;

namespace Pythia.Tagger;

/// <summary>
/// Implements the Damerau-Levenshtein distance algorithm to compute string
/// similarity, optimized for single word comparisons.
/// </summary>
/// <remarks>
/// The Damerau-Levenshtein algorithm is particularly well-suited for single
/// word comparisons as it accounts for the most common typing errors:
/// insertions, deletions, substitutions, and transpositions of adjacent
/// characters.
/// </remarks>
public sealed class DamerauLevenshteinSimilarityScorer : IStringSimilarityScorer
{
    /// <summary>
    /// Calculates the Damerau-Levenshtein distance between two strings.
    /// </summary>
    /// <param name="a">First string.</param>
    /// <param name="b">Second string.</param>
    /// <returns>The edit distance between the strings.</returns>
    private static int DamerauLevenshteinDistance(string a, string b)
    {
        // create a matrix with a row and column for each string character
        // plus one
        int[,] matrix = new int[a.Length + 1, b.Length + 1];

        // initialize the matrix with increasing values along the rows
        // and columns
        for (int i = 0; i <= a.Length; i++) matrix[i, 0] = i;

        for (int j = 0; j <= b.Length; j++) matrix[0, j] = j;

        // fill the matrix by considering each combination of characters
        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;

                // calculate the cost of different operations
                int deletion = matrix[i - 1, j] + 1;
                int insertion = matrix[i, j - 1] + 1;
                int substitution = matrix[i - 1, j - 1] + cost;

                // take the minimum cost operation
                int minCost = Math.Min(deletion,
                    Math.Min(insertion, substitution));

                // check for transposition
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] &&
                    a[i - 2] == b[j - 1])
                {
                    minCost = Math.Min(minCost, matrix[i - 2, j - 2] + cost);
                }

                matrix[i, j] = minCost;
            }
        }

        // the bottom-right cell contains the final distance
        return matrix[a.Length, b.Length];
    }

    /// <summary>
    /// Computes the similarity score between two strings.
    /// </summary>
    /// <param name="a">First string.</param>
    /// <param name="b">Second string.</param>
    /// <returns>Similarity score between 0 and 1, where 1 means identical.</returns>
    public double Score(string a, string b)
    {
        // handle null or empty strings
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
            return 1.0;

        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0.0;

        // quick check for equality
        if (a == b)
            return 1.0;

        // calculate Damerau-Levenshtein distance
        int distance = DamerauLevenshteinDistance(a, b);

        // convert the distance to a similarity score between 0 and 1
        // using the formula: 1 - (distance / max(len(a), len(b)))
        int maxLength = Math.Max(a.Length, b.Length);
        return 1.0 - ((double)distance / maxLength);
    }
}
