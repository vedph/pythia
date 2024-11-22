using Fusi.Tools;
using System.Globalization;

namespace Corpus.Core.Reading;

/// <summary>
/// Rendition numberer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RenditionNumbering" />
/// class.
/// </remarks>
/// <param name="format">The format: numbering format: <c>A</c>=alphabetic,
/// uppercase; <c>a</c>=alphabetic, lowercase; <c>1</c>=Arabic numbers,
/// <c>I</c>=Roman, uppercase; <c>i</c>=Roman, lowercase.</param>
/// <param name="step">The numbering step.</param>
internal sealed class RenditionNumbering(char format, int step = 1)
{
    private readonly char _format = format;
    private readonly int _step = step;
    private int _value;

    /// <summary>
    /// Resets the numbering to 0.
    /// </summary>
    public void Reset()
    {
        _value = 0;
    }

    /// <summary>
    /// Increments the current value and returns its label.
    /// </summary>
    /// <returns>label</returns>
    public string Increment()
    {
        _value++;
        switch (_format)
        {
            case 'A':
            case 'a':
                if (_value > 26) _value = 1;
                if (_step > 1 && _value > 1 && _value % _step != 0) return "";
                return new string((char) (_format + _value - 1), 1);

            case 'I':
                return RomanNumber.ToRoman(_value).ToUpperInvariant();
            case 'i':
                return RomanNumber.ToRoman(_value).ToLowerInvariant();

            default:
                if (_step > 1 && _value > 1 && _value % _step != 0) return "";
                return _value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
