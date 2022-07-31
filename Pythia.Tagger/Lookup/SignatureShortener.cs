using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Chiron.Morphology.Core.Templates;

namespace Pythia.Tagger.Lookup
{
    /// <summary>
    /// Morphological signature shortener. This is a helper class used to shorten
    /// a morphological signature according to a set of specified parameters.
    /// The parameters are specified using XML: the <c>abbreviations</c> element
    /// includes any number of <c>axis</c> elements, each with its <c>id</c> attribute,
    /// and the corresponding abbreviation in an <c>a</c> attribute. In turn, each
    /// axis includes any number of points (<c>p</c> elements), each with its <c>v</c>
    /// (value) attribute and its corresponding abbreviation (<c>a</c> attribute).
    /// </summary>
    public class SignatureShortener
    {
        private readonly Dictionary<string, string> _abbreviations;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignatureShortener" />
        /// class.
        /// </summary>
        public SignatureShortener()
        {
            _abbreviations = new Dictionary<string, string>();
        }

        /// <summary>
        /// Loads the parameters from the specified XML element.
        /// </summary>
        /// <param name="abbreviations">The <c>abbreviations</c> element.</param>
        /// <exception cref="ArgumentNullException">null element</exception>
        public void LoadParameters(XElement abbreviations)
        {
            if (abbreviations == null)
                throw new ArgumentNullException(nameof(abbreviations));

            _abbreviations.Clear();
            foreach (XElement xeAxis in abbreviations.Elements("axis"))
            {
                // axis id and abbreviation
                string head = xeAxis.Attribute("id").Value;
                char headAbbr = xeAxis.Attribute("a").Value[0];

                // for each point in axis, store the combination axis-point
                foreach (XElement xeP in xeAxis.Elements("p"))
                {
                    // pt abbreviation
                    char c = xeP.Attribute("a") != null
                        ? xeP.Attribute("a").Value[0]
                        : xeP.Attribute("v").Value[0];

                    // e.g. Gm : gen=m
                    _abbreviations[new string(new[] { headAbbr, c })] =
                        String.Format(CultureInfo.InvariantCulture, "{0}={1}",
                            head, xeP.Attribute("v").Value);
                }
            }
        }

        /// <summary>
        /// Abbreviates the specified axis=point pair.
        /// </summary>
        /// <param name="axis">The axis name.</param>
        /// <param name="point">The point value.</param>
        /// <returns>abbreviated pair or null</returns>
        /// <exception cref="System.ArgumentNullException">null pair</exception>
        /// <remarks>
        /// An abbreviated pair has the form Cc where the 1st character represents
        /// the axis and the 2nd its point. An expanded pair has the form axis=point.
        /// </remarks>
        public string AbbreviateAxisPoint(string axis, string point)
        {
            if (axis == null) throw new ArgumentNullException(nameof(axis));
            if (point == null) throw new ArgumentNullException(nameof(point));

            string pair = axis + "=" + point;
            if (_abbreviations.All(p => p.Value != pair)) return null;
            var kvp = _abbreviations.First(p => p.Value == pair);
            return kvp.Key;
        }

        /// <summary>
        /// Abbreviates the specified axis=point pair.
        /// </summary>
        /// <param name="pair">The axis=point pair.</param>
        /// <returns>abbreviated pair or null</returns>
        /// <exception cref="System.ArgumentNullException">null pair</exception>
        /// <remarks>
        /// An abbreviated pair has the form Cc where the 1st character represents
        /// the axis and the 2nd its point. An expanded pair has the form axis=point.
        /// </remarks>
        public string AbbreviateAxisPoint(string pair)
        {
            if (pair == null) throw new ArgumentNullException(nameof(pair));

            if (_abbreviations.All(p => p.Value != pair)) return null;
            var kvp = _abbreviations.First(p => p.Value == pair);
            return kvp.Key;
        }

        /// <summary>
        /// Expand the specified abbreviated axis=point pair.
        /// </summary>
        /// <remarks>An abbreviated pair has the form Cc where the 1st character
        /// represents the axis and the 2nd its point. An expanded pair has
        /// the form axis=point.</remarks>
        /// <param name="pair">The pair.</param>
        /// <returns>expanded pair or null</returns>
        /// <exception cref="System.ArgumentNullException">null pair</exception>
        public string ExpandAxisPoint(string pair)
        {
            if (pair == null) throw new ArgumentNullException(nameof(pair));

            return _abbreviations.ContainsKey(pair) ? _abbreviations[pair] : null;
        }

        /// <summary>
        /// Abbreviates the specified signature.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <returns>abbreviated signature where each axis=point pair is represented
        /// by two characters</returns>
        public string Abbreviate(string signature)
        {
            if (string.IsNullOrEmpty(signature)) return "";

            Signature sig = new(signature);

            string coords = string.Concat(from kvp in sig.Coords
                select AbbreviateAxisPoint(kvp.Key, kvp.Value));

            return sig.Path + (coords.Length > 0 ? "@" + coords : "");
        }

        /// <summary>
        /// Expands the specified abbreviated signature.
        /// </summary>
        /// <param name="signature">The signature.</param>
        /// <returns>expanded, "standard" signature</returns>
        public string Expand(string signature)
        {
            if (string.IsNullOrEmpty(signature)) return "";

            int x = signature.IndexOf('@');
            if (x == -1) return signature;

            StringBuilder sb = new(signature.Substring(0, x));
            for (int i = x + 1; i < signature.Length; i += 2)
            {
                string pair = ExpandAxisPoint(signature.Substring(i, 2));
                if (pair != null)
                {
                    sb.Append(i == x + 1 ? '@' : ',');
                    sb.Append(pair);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Describes the path.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="signature">The signature.</param>
        /// <returns>description</returns>
        protected virtual string DescribePath(Template template, Signature signature)
        {
            return "";
        }

        /// <summary>
        /// Describes the specified signature.
        /// </summary>
        /// <param name="template">The morphological template used.</param>
        /// <param name="signature">The signature.</param>
        /// <returns>
        /// description
        /// </returns>
        /// <exception cref="System.ArgumentNullException">null template</exception>
        public virtual string Describe(Template template, string signature)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (string.IsNullOrEmpty(signature)) return "";

            StringBuilder sb = new();
            Signature sig = new(signature);

            // class
            sb.Append(template.GetClassLabel(sig.Class));

            // [, path]
            string path = DescribePath(template, sig);
            if (!string.IsNullOrEmpty(path))
                sb.AppendFormat(CultureInfo.CurrentUICulture, ", {0}", path);

            if (sig.Coords.Any())
            {
                sb.Append(": ");

                // coords: try following the TGR axes order if any
                TgRule rule = template.GetMatchingTgr(signature);
                List<string> axesDone = new();

                if (rule != null)
                {
                    foreach (TgrAxis axis in rule.Axes)
                    {
                        if (sig.Coords.HasPoint(axis.Id))
                        {
                            axesDone.Add(axis.Id);
                            if (axesDone.Count > 1) sb.Append(", ");

                            TgrAxisDefinition def = template.AxisDefinitions.First(
                                a => a.Id == axis.Id);
                            sb.AppendFormat(CultureInfo.CurrentUICulture,
                                "{0}: {1}",
                                def.Label,
                                def.Points.First(p => p.Value == sig.Coords[axis.Id]).Label);
                        }
                    }
                }

                // just dump all the other coords as they occur
                int n = axesDone.Count;
                foreach (var kvp in sig.Coords.Where(c => !axesDone.Contains(c.Key)))
                {
                    if (++n > 1) sb.Append(", ");
                    TgrAxisDefinition def = template.AxisDefinitions.First(
                        a => a.Id == kvp.Key);
                    sb.AppendFormat(CultureInfo.CurrentUICulture, "{0}: {1}",
                        def.Label,
                        def.Points.First(p => p.Value == kvp.Value).Label);
                }
            }

            return sb.ToString();
        }
    }
}
