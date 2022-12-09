﻿using Corpus.Core.Analysis;
using Fusi.Tools;
using Fusi.Tools.Config;
using Fusi.UDPipe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pythia.Udp.Plugin;

/// <summary>
/// UDPipe-based text filter. This filter analyzes the whole text with UDPipe,
/// storing the results in the filter's context. Later, this will be
/// available to token filters, which will apply attributes from it. Thus,
/// the received text is only used to extract from it POS data, and is not
/// altered in any way.
/// <para>Tag: <c>text-filter.udp</c>.</para>
/// </summary>
/// <remarks>As API-based UDPipe processors are constrained by the length of
/// text which can be submitted via a GET request, this filter cannot process
/// the whole document's text at once, unless this is short enough. So, internally
/// it uses a <see cref="UdpChunkBuilder"/> to define chunks of text to be
/// analyzed, each with its range with reference to the original text. The
/// filter context will store a list of these chunks, having each one or more
/// sentences.</remarks>
/// <seealso cref="ITextFilter" />
/// <seealso cref="IConfigurable&lt;UdpTextFilterOptions&gt;" />
[Tag("text-filter.udp")]
public sealed class UdpTextFilter : ITextFilter, IConfigurable<UdpTextFilterOptions>
{
    private readonly UdpChunkBuilder _builder;
    private bool _dirty;

    /// <summary>
    /// The key used for UDPipe results stored in the filter's context.
    /// Value: <c>udp</c>.
    /// </summary>
    public const string UDP_KEY = "udp";

    private readonly IUDPipeProcessor _processor;

    /// <summary>
    /// Initializes a new instance of the <see cref="UdpTextFilter"/> class.
    /// </summary>
    public UdpTextFilter()
    {
        _builder = new UdpChunkBuilder();
        _processor = new ApiUDPipeProcessor();
        _dirty = true;
    }

    private void InitProcessor(string model)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));
        if (string.IsNullOrEmpty(model)) return;

        UDPipeOptions options = new()
        {
            Model = model,
            Input = UDPipeOptions.FORMAT_API_TOKENIZE,
            Tagger = "",
            Parser = "",
            Tokenizer = string.Join(";",
            new string[]
            {
                UDPipeOptions.TOKENIZER_RANGES,
                UDPipeOptions.TOKENIZER_PRESEGMENTED
            })
        };
        _processor.Configure(options);
        _dirty = false;
    }

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(UdpTextFilterOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        _builder.MaxLength = options.MaxChunkLength > 0
            ? options.MaxChunkLength : 5000;
        InitProcessor(options.Model);
    }

    /// <summary>
    /// Applies the filter to the specified reader asynchronously. Sentences
    /// extracted from document are stored in <paramref name="context"/>
    /// under key <see cref="UDP_KEY"/>.
    /// </summary>
    /// <param name="reader">The input reader.</param>
    /// <param name="context">The context. This will receive the sentences
    /// extracted from the text under key <see cref="UDP_KEY"/>.
    /// If null, the filter will do nothing.</param>
    /// <returns>The output reader.</returns>
    /// <exception cref="ArgumentNullException">reader</exception>
    public async Task<TextReader> ApplyAsync(TextReader reader,
        IHasDataDictionary? context = null)
    {
        if (reader is null) throw new ArgumentNullException(nameof(reader));

        if (context == null || _dirty) return reader;
        string text = reader.ReadToEnd();

        IList<UdpChunk> chunks = _builder.Build(text);
        foreach (UdpChunk chunk in chunks.Where(c => !c.IsOversized))
        {
            chunk.Sentences.AddRange(await _processor.ParseAsync(
                text,
                CancellationToken.None));
        }

        context.Data[UDP_KEY] = chunks;

        return new StringReader(text);
    }
}

/// <summary>
/// Options for <see cref="UdpTextFilter"/>.
/// </summary>
public class UdpTextFilterOptions
{
    /// <summary>
    /// Gets or sets the model's name.
    /// See demo at https://lindat.mff.cuni.cz/services/udpipe/.
    /// Models list: https://ufal.mff.cuni.cz/udpipe/2/models.
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets the maximum length of the chunk of text to submit to UDP
    /// processor for analysis. This is required when dealing with API-based
    /// UDPipe processors, which are constrained in the limit set for their GET
    /// request.
    /// </summary>
    public int MaxChunkLength { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UdpTextFilterOptions"/> class.
    /// </summary>
    public UdpTextFilterOptions()
    {
        Model = "latin-perseus-ud-2.10-220711";
    }
}
