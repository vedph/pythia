namespace Pythia.Tools;

/// <summary>
/// Word check report writer interface.
/// </summary>
public interface IWordReportWriter
{
    /// <summary>
    /// Open the writer to write to the specified target.
    /// </summary>
    /// <param name="target"></param>
    void Open(string target);

    /// <summary>
    /// Writes the specified word check result to the output destination.
    /// </summary>
    /// <param name="result">The result of a word check operation to be written.
    /// </param>
    void Write(WordCheckResult result);

    /// <summary>
    /// Close the writer.
    /// </summary>
    void Close();
}
