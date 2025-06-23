namespace Pythia.Tagger;

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
