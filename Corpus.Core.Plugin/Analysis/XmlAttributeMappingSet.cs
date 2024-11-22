using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace Corpus.Core.Plugin.Analysis;

/// <summary>
/// A set of <see cref="XmlAttributeMapping"/>'s.
/// </summary>
public sealed class XmlAttributeMappingSet
{
    /// <summary>
    /// Gets the mappings.
    /// </summary>
    public IList<XmlAttributeMapping> Mappings { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlAttributeMappingSet"/>
    /// class.
    /// </summary>
    public XmlAttributeMappingSet()
    {
        Mappings = new List<XmlAttributeMapping>();
    }

    /// <summary>
    /// Extracts the attributes by parsing the specified document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="nsmgr">The namespace manager.</param>
    /// <returns>attributes</returns>
    /// <exception cref="ArgumentNullException">document or nsmgr</exception>
    public IList<Attribute> Extract(XPathDocument document,
        XmlNamespaceManager nsmgr)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(nsmgr);

        List<Attribute> attributes = [];
        XPathNavigator nav = document.CreateNavigator();

        HashSet<string> names = [];

        foreach (XmlAttributeMapping mapping in Mappings)
        {
            // ignore if already located and no multiple values allowed
            if (names.Contains(mapping.Name!) && !mapping.IsMultiple) continue;

            XPathNodeIterator iterator = nav.Select(mapping.XPath!, nsmgr);
            if (iterator.MoveNext())
            {
                string? value = iterator.Current!.Value;
                if (mapping.Pattern != null)
                {
                    Match m = mapping.Pattern.Match(value);
                    value = m.Success ?
                        (m.Groups.Count > 1 ? m.Groups[1].Value : m.Value) : null;
                }

                if (value != null)
                {
                    attributes.Add(new Attribute(mapping.Name!, value)
                    {
                        Type = mapping.Type
                    });
                    names.Add(mapping.Name!);
                }
            }
        }

        return attributes;
    }

    /// <summary>
    /// Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{nameof(XmlAttributeMappingSet)}: {string.Join("\n", Mappings)}";
    }
}
