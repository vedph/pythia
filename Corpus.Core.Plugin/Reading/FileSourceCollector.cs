using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corpus.Core.Reading;
using Fusi.Tools.Configuration;

namespace Corpus.Core.Plugin.Reading;

/// <summary>
/// File system based source collector. This collector just enumerates
/// the files matching a specified mask in a specified directory.
/// Tag: <c>source-collector.file</c>.
/// </summary>
/// <seealso cref="ISourceCollector" />
[Tag("source-collector.file")]
public sealed class FileSourceCollector : ISourceCollector,
    IConfigurable<FileSourceCollectorOptions>
{
    private FileSourceCollectorOptions? _options;

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(FileSourceCollectorOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Collects all the text sources available in the specified source.
    /// </summary>
    /// <param name="source">The source, which here is a full file path,
    /// usually with a file mask, e.g. <c>C:\Corpus\*.xml</c>.</param>
    /// <returns>text sources</returns>
    /// <exception cref="ArgumentNullException">null source</exception>
    public IEnumerable<string> Collect(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return InnerCollect();

        IEnumerable<string> InnerCollect()
        {
            string dir = Path.GetDirectoryName(source) ?? "";
            string mask = Path.GetFileName(source) ?? "*.*";

            foreach (string filePath in Directory.GetFiles(dir, mask)
                .OrderBy(s => s))
            {
                yield return filePath;
            }

            if (_options?.IsRecursive == true)
            {
                foreach (string subdir in Directory.GetDirectories(dir)
                    .OrderBy(s => s))
                {
                    foreach (string file in Collect(subdir))
                    {
                        yield return file;
                    }
                }
            }
        }
    }
}

/// <summary>
/// Options for <see cref="FileSourceCollector"/>'s.
/// </summary>
public sealed class FileSourceCollectorOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether this instance is recursive.
    /// </summary>
    public bool IsRecursive { get; set; }
}
