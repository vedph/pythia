using System;
using System.Text;
using System.Text.RegularExpressions;
using Corpus.Core;
using Corpus.Core.Reading;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// The definition of an XML-based structure for an
    /// <see cref="XmlStructureParser"/>.
    /// </summary>
    public sealed class XmlStructureDefinition
    {
        /// <summary>
        /// Gets or sets the XPath-like path to the target element of the
        /// structure, with its value path(s) used to retrieve the targeted
        /// structure's value.
        /// </summary>
        public XmlPath Path { get; set; }

        /// <summary>
        /// Gets or sets the structure name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the structure value type.
        /// </summary>
        public AttributeType Type { get; set; }

        /// <summary>
        /// Gets or sets the name of the token target. When this is not null,
        /// it means that the structure definition targets a token rather than
        /// a structure. 
        /// This happens when you want to add attributes to those tokens which
        /// appear inside specific structures, while you do not want these
        /// structures to be stored as such, as their only usage is marking
        /// the included tokens. 
        /// For instance, a structure like "foreign" in TEI marks a token as
        /// a foreign word, but should not be stored among structures.
        /// </summary>
        public string TokenTargetName { get; set; }

        /// <summary>
        /// Gets or sets the value of the token target. If this value is null and
        /// <see cref="TokenTargetName"/> is not null, this means that the value
        /// for the token's attribute should be equal to the structure value.
        /// </summary>
        public string TokenTargetValue { get; set; }

        /// <summary>
        /// Parses the specified text, with format <c>name=path</c> or
        /// <c>name#=path</c>, where a name ending with <c>#</c> defines a
        /// numeric value type (the default being a string value type). The
        /// path is expressed with the format required by
        /// <see cref="XmlPath.Parse"/>. Further, the <c>name</c> can follow
        /// the pattern <c>name:tokenname:tokenvalue</c> or just
        /// <c>name:tokenname</c> when you want to define a structure targeting
        /// a token: in this case, the second pattern is used when you want
        /// to use the structure's value rather than a constant value expressed
        /// in the definition.
        /// For instance, <c>sound:x:snd</c> means that the structure
        /// corresponding to the element named <c>sound</c> should be used
        /// to add an attribute named <c>x</c> with value <c>snd</c> to each
        /// token inside that structure.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>definition</returns>
        /// <exception cref="System.ArgumentNullException">null text</exception>
        public static XmlStructureDefinition Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            Match m = Regex.Match(text,
                @"^(?<n>[^=\#]+)(?<nv>\#?)\s*=\s*(?<p>.+)");
            if (!m.Success) throw new ArgumentException(nameof(text));

            XmlStructureDefinition def = new XmlStructureDefinition
            {
                Name = m.Groups["n"].Value,
                Path = XmlPath.Parse(m.Groups["p"].Value),
                Type = m.Groups["nv"].Value?.Length > 0
                    ? AttributeType.Number
                    : AttributeType.Text
            };

            // further analyze the name for pattern name:tokenname:tokenvalue
            m = Regex.Match(def.Name, "(?<n>[^:]+):(?<tn>[^:]+)(?::(?<tv>.+))?");
            if (m.Success)
            {
                def.Name = m.Groups["n"].Value;
                def.TokenTargetName = m.Groups["tn"].Value;
                def.TokenTargetValue = m.Groups["tv"].Value.Length == 0?
                    null : m.Groups["tv"].Value;
            }

            return def;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(Name);

            if (TokenTargetName != null)
            {
                sb.Append(':').Append(TokenTargetName);
                if (TokenTargetValue != null)
                    sb.Append(':').Append(TokenTargetValue);
            }
            sb.Append(Type == AttributeType.Number ? "#" : "")
                .Append('=').Append(Path);

            return sb.ToString();
        }
    }
}
