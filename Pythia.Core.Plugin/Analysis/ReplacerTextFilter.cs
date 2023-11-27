using Corpus.Core.Analysis;
using Fusi.Tools;
using Fusi.Tools.Configuration;
using Fusi.Tools.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// Generic replacer text filter. This applies the replacements specified
/// in the options, which may be either literals or patterns. Typically this
/// is used to apply small changes to texts before indexing them; any further
/// text preparation should be outside the index pipeline. Also, you should
/// ensure that the replacements do not change the text length if you want it
/// to be mapped back to the original one.
/// <para>For instance, you might want to replace things like <c>E’</c> to
/// <c>È </c> (where the redundant space at the end preserves the original
/// length).</para>
/// <para>Tag: <c>text-filter.replacer</c>.</para>
/// </summary>
[Tag("text-filter.replacer")]
public sealed class ReplacerTextFilter : ITextFilter,
    IConfigurable<ReplacerTextFilterOptions>
{
    private readonly TextReplacer _replacer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplacerTextFilter"/> class.
    /// </summary>
    public ReplacerTextFilter()
    {
        _replacer = new TextReplacer(false);
    }

    /// <summary>
    /// Configures the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(ReplacerTextFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _replacer.Clear();
        if (options.Replacements?.Count > 0)
        {
            foreach (ReplacerOptionsEntry o in options.Replacements)
            {
                if (o.IsPattern)
                    _replacer.AddExpression(o.Source!, o.Target!, o.Repetitions);
                else
                    _replacer.AddLiteral(o.Source!, o.Target!, o.Repetitions);
            }
        }
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
        if (_replacer.IsEmpty) return Task.FromResult(reader);

        // uncomment this for diagnostic purposes
        //foreach (var r in _replacer.GetReplacements())
        //{
        //    r.OnReplacing = (rep, text) =>
        //    {
        //        System.Diagnostics.Debug.WriteLine(rep);
        //        return true;
        //    };
        //}
        string text = reader.ReadToEnd();
        text = _replacer.Replace(text)!;
        return Task.FromResult((TextReader)new StringReader(text));
    }
}

/// <summary>
/// Options for <see cref="ReplacerTextFilter"/>.
/// </summary>
public class ReplacerTextFilterOptions
{
    /// <summary>
    /// Gets or sets the replacements.
    /// </summary>
    public IList<ReplacerOptionsEntry>? Replacements { get; set; }
}
