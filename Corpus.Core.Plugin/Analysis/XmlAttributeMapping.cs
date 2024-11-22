using System;
using System.Text.RegularExpressions;

namespace Corpus.Core.Plugin.Analysis;

/// <summary>
/// A mapping between an XML search expression and a target attribute,
/// used by <see cref="XmlAttributeMappingSet"/>.
/// </summary>
public sealed class XmlAttributeMapping
{
    /// <summary>
    /// Gets or sets the target attribute name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this target attribute
    /// allows for multiple values. When not set, the search stops at the
    /// first valid match.
    /// </summary>
    public bool IsMultiple { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression to look for the attribute value(s)
    /// in the source XML document. This is a full XPath expression, and
    /// can use namespace prefixes.
    /// </summary>
    public string? XPath { get; set; }

    /// <summary>
    /// Gets or sets the optional regular expression pattern to match
    /// in the node located by <see cref="XPath"/>. The full match will
    /// be used as the source of the attribute's value, unless this
    /// expression has a capturing group. In this case, the value used
    /// will be that of the first capturing group (which should also be
    /// the unique capturing group in the expression).
    /// </summary>
    public Regex? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the target attribute data type.
    /// </summary>
    public AttributeType Type { get; set; }

    /// <summary>
    /// Parses the specified text representing a mappings. This contains
    /// in this order:
    /// <list type="number">
    /// <item>
    /// <term>attribute name</term>
    /// <description>the attribute target name, eventually suffixed with
    /// <c>+</c> when more than 1 values for it are allowed.</description>
    /// </item>
    /// <item>
    /// <term>equals</term>
    /// <description>the equals sign introduces the search expression.
    /// </description>
    /// </item>
    /// <item>
    /// <term>XPath</term>
    /// <description>a full XPath 1.0 expression to locate the attribute's
    /// value(s).</description>
    /// </item>
    /// <item>
    /// <term>attribute type (optional)</term>
    /// <description>The type of the attribute, divided from the previous
    /// token by a space; it is either <c>[T]</c>=text or <c>[N]</c>=numeric.
    /// </description>
    /// </item>
    /// <item>
    /// <term>regular expression</term>
    /// <description>A regular expression pattern, divided from the previous
    /// token by a space, used to further filter the attribute's value
    /// and/or parse it.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>For instance, the date expressed by a year value could be
    /// mapped as
    /// <c>date-value=/tei:TEI/tei:teiHeader/tei:fileDesc/tei:titleStmt/tei:date/@when [N] [12]\d{3}</c>.
    /// </remarks>
    /// <param name="text">The text.</param>
    /// <returns>mapping or null if invalid</returns>
    /// <exception cref="ArgumentNullException">null text</exception>
    public static XmlAttributeMapping? Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Match m = Regex.Match(text,
            @"^\s*(?<n>[^=+\s]+)(?<m>\+)?\s*=\s*(?<p>[^\s]+)" +
            @"(?:\s+\[(?<t>[NnTt])\])?(?:\s+(?<r>[^\s]+.+))?");
        if (m.Success)
        {
            return new XmlAttributeMapping
            {
                Name = m.Groups["n"].Value,
                IsMultiple = m.Groups["m"].Length > 0,
                XPath = m.Groups["p"].Value,
                Pattern = m.Groups["r"].Length > 0 ?
                    new Regex(m.Groups["r"].Value) : null,
                Type = string.Equals(m.Groups["t"].Value, "n",
                    StringComparison.InvariantCultureIgnoreCase)
                       ? AttributeType.Number
                       : AttributeType.Text
            };
        }

        return null;
    }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Name}={XPath}" +
            (IsMultiple? "+" : "") +
            $" [{(Type == AttributeType.Number ? 'N' : 'T')}]" +
            $"{(Pattern != null ? " " + Pattern : "")}";
    }
}
