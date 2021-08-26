using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Pythia.Core.Plugin.Analysis
{
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
        public static Dictionary<string, string> ParseNamespaces(string[] namespaces)
        {
            Dictionary<string, string> nss = null;
            if (namespaces?.Length > 0)
            {
                nss = new Dictionary<string, string>();
                foreach (string pair in namespaces)
                {
                    Match m = Regex.Match(pair, "^([^=]+)=(.+)");
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
        public static string ResolveTagName(string name,
            IDictionary<string, string> namespaces)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (name.IndexOf(':') == -1) return name;

            Match m = Regex.Match(name, "^(?<p>[^:]+):(?<n>.+)");
            if (!m.Success) return name;    // defensive

            string prefix = m.Groups["p"].Value;
            string localName = m.Groups["n"].Value;
            if (namespaces?.ContainsKey(prefix) != true) return null;
            return "{" + namespaces[prefix] + "}" + localName;
        }
    }
}
