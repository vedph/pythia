using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Corpus.Core.Reading;

/// <summary>
/// XML element numberer, used before rendering an XML document wherever
/// numbering is required. Each element getting a number receives an
/// attribute named with the prefix <c>_r-</c> followed by the element name.
/// For instance, an <c>l</c> element gets an attribute named <c>_r-l</c>,
/// whose value is the number in the desired format.
/// </summary>
public sealed class XmlElementNumberer
{
    private readonly Dictionary<XName, Tuple<char, int>> _namesAndFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlElementNumberer"/> class.
    /// </summary>
    /// <param name="namesAndFormats">The elements names to be numbered,
    /// with their numbering format and step, represented by a character
    /// following an equal sign, optionally followed by a semicolon
    /// and the step value. If not specified, the default format is
    /// <c>1</c> and the default value is 1. For instance, two strings
    /// with value <c>lg=a</c> and <c>l=1:5</c> mean that every <c>lg</c>
    /// element must be numbered with a lowercase letter (a, b, c...
    /// up to z; then restart from a) and each <c>l</c> element must be
    /// numbered with a number, with step 5 (1, 5, 10...).
    /// <para>To add a namespace URI to a name, prefix the name with the
    /// URI between braces, e.g. <c>{http://www.tei-c.org/ns/1.0}lg</c></para>.
    /// </param>
    /// <exception cref="ArgumentNullException">null elements</exception>
    public XmlElementNumberer(IEnumerable<string> namesAndFormats)
    {
        ArgumentNullException.ThrowIfNull(namesAndFormats);

        _namesAndFormats = [];
        Regex r = new(@"^(?<n>[^=]+)=(?<f>[1Aa])(?::(?<s>\d+))?$");

        foreach (string s in namesAndFormats)
        {
            Match m = r.Match(s);
            if (m.Success)
            {
                _namesAndFormats[m.Groups["n"].Value] =
                    Tuple.Create(
                        m.Groups["f"].Value[0],
                        m.Groups["s"].Value.Length > 0
                            ? int.Parse(m.Groups["s"].Value, CultureInfo.InvariantCulture)
                            : 1);
            }
        }
    }

    private void ApplyNumbering(XElement root, XName name)
    {
        // for each head x in a series of at least 2 x siblings
        foreach (XElement headElem in root.Descendants(name)
            .Where(xe => !xe.ElementsBeforeSelf(name).Any() &&
                         xe.ElementsAfterSelf(name).Any()))
        {
            RenditionNumbering numbering = new(
                _namesAndFormats.ContainsKey(name)
                    ? _namesAndFormats[name].Item1
                    : '1',
                _namesAndFormats.ContainsKey(name)
                    ? _namesAndFormats[name].Item2
                    : 1);

            string s = numbering.Increment();
            if (s.Length > 0)
                headElem.SetAttributeValue($"_r-{name.LocalName}", s);

            foreach (XElement nextElem in headElem.ElementsAfterSelf(name))
            {
                s = numbering.Increment();
                if (s.Length > 0)
                    nextElem.SetAttributeValue($"_r-{name.LocalName}", s);
            }
        }
    }

    /// <summary>
    /// Applies numbering from the specified XML element representing the
    /// item's root.
    /// </summary>
    /// <param name="root">The item's root XML element to start from.</param>
    /// <exception cref="ArgumentNullException">null root element</exception>
    public void Number(XElement root)
    {
        ArgumentNullException.ThrowIfNull(root);

        foreach (XName key in _namesAndFormats.Keys)
            ApplyNumbering(root, key);
    }
}
