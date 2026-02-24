namespace Pythia.Tagger;

/// <summary>
/// Defines a contract for calculating a similarity score between two strings.
/// </summary>
/// <remarks>Implementations of this interface provide a method to evaluate how
/// similar two strings are, which can be useful in scenarios such as search
/// algorithms, data deduplication, and natural language processing. The specific
/// scoring algorithm and interpretation of the score may vary between
/// implementations.</remarks>
public interface IStringSimilarityScorer
{
    /// <summary>
    /// Computes the similarity score between two strings.
    /// </summary>
    /// <param name="a">First string.</param>
    /// <param name="b">Second string.</param>
    /// <returns>Similarity score between 0 and 1, where 1 means identical.</returns>
    double Score(string a, string b);
}
