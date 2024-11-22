using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Corpus.Core.Reading;

/// <summary>
/// Document reader.
/// </summary>
/// <remarks>A document reader uses instances of <see cref="ITextRetriever"/>,
/// <see cref="ITextMapper"/>, <see cref="ITextPicker"/> to get
/// the source of a document, parse its text, map it, and retrieve the
/// portion of text defined either by a node of this map or by a range of
/// character offsets.
/// </remarks>
public sealed class DocumentReader
{
    private readonly ITextRetriever _retriever;
    private readonly ITextMapper _mapper;
    private readonly ITextPicker _picker;
    private readonly IMemoryCache? _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentReader" /> class.
    /// </summary>
    /// <param name="retriever">The text retriever.</param>
    /// <param name="mapper">The text mapper.</param>
    /// <param name="picker">The text picker.</param>
    /// <param name="cache">The optional cache to use.</param>
    /// <exception cref="ArgumentNullException">null retriever or mapper
    /// or picker</exception>
    public DocumentReader(ITextRetriever retriever, ITextMapper mapper,
        ITextPicker picker, IMemoryCache? cache = null)
    {
        _retriever = retriever ??
            throw new ArgumentNullException(nameof(retriever));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _picker = picker ?? throw new ArgumentNullException(nameof(picker));
        _cache = cache;
    }

    /// <summary>
    /// Read the text corresponding to the content of the specified document
    /// map node.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="nodePath">The path to the map node to read. See
    /// <see cref="TextMapNode.GetPath"/>
    /// for the path syntax.</param>
    /// <returns>text or null if not found</returns>
    /// <exception cref="ArgumentNullException">null metadata or node</exception>
    public async Task<TextPiece?> ReadAsync(IDocument document,
        string nodePath)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(nodePath);

        string? text = null;
        TextMapNode? map = null;
        bool cacheHit = false;

        // use cached text and map if any
        Tuple<string, TextMapNode>? cached =
            (Tuple<string, TextMapNode>?)_cache?.Get(document.Id);
        if (cached != null)
        {
            text = cached.Item1;
            map = cached.Item2;
            if (map == null) return null;
            cacheHit = true;

            // clear any existing selection 
            map.Visit(n =>
            {
                n.IsSelected = false;
                return true;
            });
        }

        // if no cache available, retrieve the text and map it
        if (!cacheHit)
        {
            text = await _retriever.GetAsync(document);
            // remove FEFF header which might have been inserted when storing text
            if (text?.Length > 0 && text[0] == 0xFEFF) text = text[1..];
            map = _mapper.Map(text!, document.Attributes!.ToImmutableDictionary(
                a => a.Name!, a => a.Value!));
            if (map == null) return null;

            // supply root label if required
            if (map.Label == "-" && !string.IsNullOrEmpty(document.Title))
                map.Label = document.Title;

            _cache?.Set(document.Id, Tuple.Create(text, map));
        }

        // locate the requested node (path "0"=root is a corner case)
        TextMapNode? node;
        if (nodePath.Length < 2)
        {
            node = map;
            while (node?.Parent != null) node = node.Parent;
        }
        else
        {
            node = map?.GetDescendant(nodePath[2..]);
        }

        if (node == null) return null;

        // pick the corresponding text
        return _picker.PickNode(text!, map!, node);
    }

    /// <summary>
    /// Read the context wrapping the specified range of character offsets.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="startIndex">The start character index.</param>
    /// <param name="endIndex">The end character index.</param>
    /// <returns>text or null if not found</returns>
    /// <exception cref="ArgumentNullException">null metadata</exception>
    public async Task<TextPiece?> ReadAsync(IDocument document,
        int startIndex, int endIndex)
    {
        ArgumentNullException.ThrowIfNull(document);

        string? text = null;
        TextMapNode? map = null;
        bool cacheHit = false;

        Tuple<string, TextMapNode>? cached = (Tuple<string, TextMapNode>?)
            _cache?.Get(document.Id);
        if (cached != null)
        {
            text = cached.Item1;
            map = cached.Item2;
            if (map == null) return null;
            cacheHit = true;

            // clear any existing selection 
            map.Visit(n =>
            {
                n.IsSelected = false;
                return true;
            });
        }

        if (!cacheHit)
        {
            text = await _retriever.GetAsync(document);
            map = _mapper.Map(text!,
                document.Attributes!.ToImmutableDictionary(
                    a => a.Name!, a => a.Value!));
            if (map == null) return null;

            // supply root label if required
            if (map.Label == "-" && !string.IsNullOrEmpty(document.Title))
                map.Label = document.Title;

            _cache?.Set(document.Id, Tuple.Create(text, map));
        }

        return _picker.PickContext(text!, map!, startIndex, endIndex);
    }
}
