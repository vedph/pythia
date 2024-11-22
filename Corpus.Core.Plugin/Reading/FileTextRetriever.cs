using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Corpus.Core.Reading;
using Fusi.Tools.Configuration;

namespace Corpus.Core.Plugin.Reading;

/// <summary>
/// File-system based UTF-8 text retriever. This is the simplest text
/// retriever, which just opens a text file from the file system and
/// reads it.
/// Tag: <c>text-retriever.file</c>.
/// </summary>
[Tag("text-retriever.file")]
public sealed class FileTextRetriever : ITextRetriever,
    IConfigurable<FileTextRetrieverOptions>
{
    private Regex? _findRegex;
    private string? _replace;

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(FileTextRetrieverOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrEmpty(options.FindPattern))
        {
            try
            {
                _findRegex = new Regex(options.FindPattern);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            _replace = options.ReplacePattern ?? "";
        }
    }

    /// <summary>
    /// Retrieve the text from the specified document.
    /// </summary>
    /// <param name="document">The document to retrieve text for.</param>
    /// <param name="context">Not used.</param>
    /// <returns>Text, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">null id</exception>
    public async Task<string?> GetAsync(IDocument document, object? context = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        string filePath = _findRegex != null
            ? _findRegex.Replace(document.Source!, _replace ?? "")
            : document.Source!;

        using StreamReader reader = new(File.OpenRead(filePath!), Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}

/// <summary>
/// Options for <see cref="FileTextRetriever"/>.
/// </summary>
public class FileTextRetrieverOptions
{
    /// <summary>
    /// Gets or sets the optional find pattern. This is an expression used
    /// to find a part of the file path and replace it with the value in
    /// <see cref="ReplacePattern"/>.
    /// For instance, <c>^E:\\Temp\\Archive\\</c> could be a find pattern,
    /// and <c>D:\Jobs\Crusca\Prin2012\Archive\</c> a replace pattern.
    /// </summary>
    public string? FindPattern { get; set; }

    /// <summary>
    /// Gets or sets the optional replace pattern. This replaces the
    /// expression specified by <see cref="FindPattern"/>.
    /// For instance, <c>^E:\\Temp\\Archive\\</c> could be a find pattern, and
    /// <c>D:\Jobs\Crusca\Prin2012\Archive\</c> a replace pattern.
    /// </summary>
    public string? ReplacePattern { get; set; }
}
