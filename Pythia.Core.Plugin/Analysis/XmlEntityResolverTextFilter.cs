using Corpus.Core.Analysis;
using Fusi.Tools;
using Fusi.Tools.Configuration;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// XML and HTML standard entities resolver filter.
/// Whenever this filter finds a standard XML or HTML entity (either by code
/// or by name) it replaces it with the corresponding character, plus if
/// requested the padding characters needed to reach the length of the original
/// entity code.
/// <para>Tag: <c>text-filter.xml-entity-resolver</c>.</para>
/// </summary>
[Tag("text-filter.xml-entity-resolver")]
public sealed partial class XmlEntityResolverTextFilter : ITextFilter,
    IConfigurable<XmlEntityResolverTextFilterOptions>
{
    [GeneratedRegex(@"&(?:#[xX][0-9a-fA-F]+|#[0-9]+|[a-zA-Z][a-zA-Z0-9]*);")]
    private static partial Regex EntityRegex();

    private XmlEntityResolverTextFilterOptions _options = new();

    /// <summary>
    /// Configure this filter.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(XmlEntityResolverTextFilterOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Applies the filter to the specified reader asynchronously.
    /// </summary>
    /// <param name="reader">The input reader.</param>
    /// <param name="context">The optional context.</param>
    /// <returns>
    /// The output reader.
    /// </returns>
    public Task<TextReader> ApplyAsync(TextReader reader,
        IHasDataDictionary? context = null)
    {
        string text = reader.ReadToEnd();

        bool padding = _options.IsPaddingEnabled;

        string result = EntityRegex().Replace(text, m =>
        {
            string decoded = WebUtility.HtmlDecode(m.Value);
            // HtmlDecode returns the input unchanged when the entity is unknown
            if (decoded == m.Value) return m.Value;

            if (padding)
            {
                int spaces = m.Length - decoded.Length;
                return spaces > 0 ? decoded + new string(' ', spaces) : decoded;
            }
            return decoded;
        });

        return Task.FromResult((TextReader)new StringReader(result));
    }
}

/// <summary>
/// Options for <see cref="XmlEntityResolverTextFilter"/>.
/// </summary>
public class XmlEntityResolverTextFilterOptions
{
    /// <summary>
    /// True to enable padding. When true, whenever an entity is resolved
    /// a number of spaces after it equal to the entity name length - the length
    /// of the resolved value is added, so that the total length of the text
    /// remains unchanged. Default is false.
    /// </summary>
    public bool IsPaddingEnabled { get; set; }
}
