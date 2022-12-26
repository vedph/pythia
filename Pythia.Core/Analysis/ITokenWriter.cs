using System;

namespace Pythia.Core.Analysis;

/// <summary>
/// Generic writer for <see cref="Token"/>'s. This is typically used
/// when dumping tokens via <see cref="IndexBuilder"/>.
/// </summary>
public interface ITokenWriter : IDisposable
{
    /// <summary>
    /// Writes the specified token.
    /// </summary>
    /// <param name="token">The token.</param>
    void Write(Token token);
}
