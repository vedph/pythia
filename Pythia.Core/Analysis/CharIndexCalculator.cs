using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pythia.Core.Analysis;

/// <summary>
/// Character index from line/column pair calculator.
/// </summary>
public sealed class CharIndexCalculator
{
    private readonly List<int> _lineChars;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharIndexCalculator"/>
    /// class.
    /// </summary>
    /// <param name="reader">The text reader of the source text.</param>
    /// <exception cref="ArgumentNullException">null reader</exception>
    public CharIndexCalculator(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        _lineChars = new List<int>();
        Scan(reader);
    }

    private void Scan(TextReader reader)
    {
        int n, count = 0;
        bool prevCr = false;

        while ((n = reader.Read()) != -1)
        {
            switch (n)
            {
                case 13:    // CR
                    prevCr = true;
                    _lineChars.Add(count + 1);
                    count = 0;
                    break;
                case 10:    // LF
                    // if LF only, end the line
                    if (!prevCr)
                    {
                        _lineChars.Add(count + 1);
                        count = 0;
                    }
                    // else the line was already ended by CR,
                    // just add 1 for LF
                    else
                    {
                        _lineChars[^1]++;
                    }
                    prevCr = false;
                    break;
                default:
                    prevCr = false;
                    count++;
                    break;
            }
        }
        if (count > 0) _lineChars.Add(count);
    }

    /// <summary>
    /// Gets the character index corresponding to the specified line and
    /// column.
    /// </summary>
    /// <param name="line">The line (1-N).</param>
    /// <param name="column">The column (1-N).</param>
    /// <returns>index</returns>
    /// <exception cref="ArgumentOutOfRangeException">line or column less
    /// than 1</exception>
    public int GetIndex(int line, int column)
    {
        if (line < 1) throw new ArgumentOutOfRangeException(nameof(line));
        if (column < 1) throw new ArgumentOutOfRangeException(nameof(column));

        return _lineChars.Take(line - 1).Sum() + column - 1;
    }
}
