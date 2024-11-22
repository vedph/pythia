using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace Corpus.Core.Plugin.Analysis;

/// <summary>
/// Helper for options dealing with a list of XML namespaces.
/// </summary>
public static class XmlNsOptionHelper
{
    /// <summary>
    /// Parses the specified namespaces pairs.
    /// </summary>
    /// <param name="namespaces">The namespaces pairs (prefix=uri).</param>
    /// <returns>Dictionary with namespaces keyed by their prefixes, or
    /// null.</returns>
    public static Dictionary<string, string>? ParseNamespaces(
        IList<string>? namespaces)
    {
        Dictionary<string, string>? nss = null;
        if (namespaces?.Count > 0)
        {
            nss = [];
            foreach (string pair in namespaces)
            {
                Match m = Regex.Match(pair, "^([^=]*)=(.+)");
                if (m.Success) nss[m.Groups[1].Value] = m.Groups[2].Value;
            }
        }
        return nss;
    }

    /// <summary>
    /// Resolves the name of the XML tag when it has a namespace prefix,
    /// or just return it unchanged when it's unprefixed.
    /// </summary>
    /// <param name="name">The tag name.</param>
    /// <param name="namespaces">The namespaces keyed by their prefix.</param>
    /// <returns>Resolved name or null if cannot be resolved.</returns>
    /// <exception cref="ArgumentNullException">name</exception>
    public static string? ResolveTagName(string name,
        IDictionary<string, string> namespaces)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!name.Contains(':')) return name;

        Match m = Regex.Match(name, "^(?<p>[^:]+):(?<n>.+)");
        if (!m.Success) return name;    // defensive

        string prefix = m.Groups["p"].Value;
        string localName = m.Groups["n"].Value;
        if (namespaces?.ContainsKey(prefix) != true) return null;
        return "{" + namespaces[prefix] + "}" + localName;
    }

    private static void AddNamespaces(IDictionary<string, string> namespaces,
        string? defaultNsPrefix, XmlNamespaceManager nsmgr)
    {
        foreach (var ns in namespaces)
        {
            nsmgr.AddNamespace(ns.Key, ns.Value);

            // an empty namespace is mapped also to the DEFAULT prefix.
            // This is because XPath treats the empty prefix as the null
            // namespace, i.e. only prefixes mapped to namespaces can be
            // used in XPath queries. To query against a namespace in XML,
            // even if it is the default namespace, you need to define
            // a prefix for it. See:
            // https://stackoverflow.com/questions/585812/using-xpath-with-default-namespace-in-c-sharp
            if (ns.Key.Length == 0 && defaultNsPrefix != null)
                nsmgr.AddNamespace(defaultNsPrefix, ns.Value);
        }
    }

    /// <summary>
    /// Gets all the document namespaces in root scope into an XML namespaces
    /// manager.
    /// </summary>
    /// <param name="xml">The XML code representing the document.</param>
    /// <param name="defaultNsPrefix">The optional default prefix to map
    /// to the default namespace with an empty prefix. This is required
    /// when you have a document with a default empty namespace, and you
    /// want to query it via XPath.</param>
    /// <param name="namespaces">Additional namespaces to use, keyed by
    /// their prefix.</param>
    /// <returns>Namespaces manager.</returns>
    /// <exception cref="ArgumentNullException">xml</exception>
    public static XmlNamespaceManager GetDocNamespacesManager(string xml,
        string? defaultNsPrefix = null,
        IDictionary<string, string>? namespaces = null)
    {
        ArgumentNullException.ThrowIfNull(xml);

        // get all the namespaces in document
        // https://www.hanselman.com/blog/get-namespaces-from-an-xml-document-with-xpathdocument-and-linq-to-xml
        XmlDocument doc = new();
        doc.LoadXml(xml);
        XPathNavigator nav = doc.CreateNavigator()!;
        nav.MoveToFollowing(XPathNodeType.Element);
        var docNamespaces = nav.GetNamespacesInScope(XmlNamespaceScope.All);
        XmlNamespaceManager nsmgr = new(doc.NameTable);

        AddNamespaces(docNamespaces, defaultNsPrefix, nsmgr);
        if (namespaces?.Count > 0)
            AddNamespaces(namespaces, defaultNsPrefix, nsmgr);

        return nsmgr;
    }
}
