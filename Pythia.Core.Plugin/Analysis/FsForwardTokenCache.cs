﻿using Pythia.Core.Analysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Pythia.Core.Plugin.Analysis;

/// <summary>
/// A file-system based, forward-only tokens cache. This is optimized for
/// forward writes or reads, and is typically used for deferred POS
/// tagging.
/// </summary>
/// <seealso cref="ITokenCache" />
public sealed class FsForwardTokenCache : ITokenCache
{
    private readonly Regex _tokHeadRegex;
    private string _rootDir;
    private TextWriter? _writer;
    private int _writeDocId;
    private int _writeFileNr;
    private int _writtenTokenCount;

    private int _readDocId;
    private int _readFileNr;
    private readonly List<TextSpan> _readTokens;

    /// <summary>
    /// Gets the list of TextSpan attributes allowed to be stored in the cache.
    /// When empty, any attribute is allowed; otherwise, only the attributes
    /// included in this list are allowed.
    /// </summary>
    public HashSet<string> AllowedAttributes { get; }

    /// <summary>
    /// Gets or sets the number of tokens per file. The default value is
    /// 1000. Whenever the tokens count reaches this limit, a new file
    /// is created.
    /// </summary>
    public int TokensPerFile { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FsForwardTokenCache"/>
    /// class.
    /// </summary>
    public FsForwardTokenCache()
    {
        _tokHeadRegex = new Regex(@"^\#(?<p>\d+)\s+(?<i>\d+)x(?<l>\d+)");
        _rootDir = "";
        AllowedAttributes = [];
        TokensPerFile = 1000;
        _readTokens = [];
    }

    /// <summary>
    /// Opens or creates the cache at the specified source.
    /// </summary>
    /// <param name="source">The source. The meaning of this parameter
    /// varies according to the cache implemetation. For instance, in a
    /// file system it might just be a directory name.</param>
    /// <exception cref="ArgumentNullException">source</exception>
    public void Open(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Close();
        if (!Directory.Exists(source)) Directory.CreateDirectory(source);
        _rootDir = source;
    }

    private void CloseReadDocument()
    {
        _readDocId = 0;
        _readFileNr = 0;
        _readTokens.Clear();
    }

    private void CloseWriteDocument()
    {
        _writer?.Flush();
        _writer?.Close();
        _writer = null;
        _writeDocId = 0;
        _writtenTokenCount = 0;
    }

    /// <summary>
    /// Closes this cache.
    /// </summary>
    public void Close()
    {
        _rootDir = "";

        CloseReadDocument();
        CloseWriteDocument();
    }

    /// <summary>
    /// Deletes the cache at the specified source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <exception cref="ArgumentNullException">source</exception>
    public void Delete(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (Directory.Exists(source)) Directory.Delete(source, true);
    }

    /// <summary>
    /// Checks if the cache at the specified source exists.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>True if exists.</returns>
    /// <exception cref="ArgumentNullException">source</exception>
    public bool Exists(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Directory.Exists(source);
    }

    private static string GetFileMask(int documentId)
        => $"{documentId:00000}.*.txt";

    private string GetFilePath(int documentId, int block) =>
        Path.Combine(_rootDir ?? "", $"{documentId:00000}.{block:000}.txt");

    private static void TryDelete(string file)
    {
        Exception? error = null;
        int ms = 500;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                File.Delete(file);
                return;
            }
            catch (Exception ex)
            {
                error = ex;
                Thread.Sleep(ms);
                ms += 500;
            }
        }
        if (error != null) throw error;
    }

    /// <summary>
    /// Deletes the document with the specified ID from the cache.
    /// May throw any <see cref="File.Delete"/> exception.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    public void DeleteDocument(int id)
    {
        if (_writeDocId == id) CloseWriteDocument();
        if (_readDocId == id) CloseReadDocument();

        string mask = GetFileMask(id);
        foreach (string file in Directory.EnumerateFiles(_rootDir, mask))
        {
            TryDelete(file);
        }
    }

    private void WriteToken(TextSpan TextSpan, TextWriter writer)
    {
        // #pos NxN
        writer.WriteLine($"#{TextSpan.P1} {TextSpan.Index}x{TextSpan.Length}");
        // value
        writer.WriteLine(TextSpan.Value);
        // attributes
        if (TextSpan.Attributes?.Count > 0)
        {
            foreach (var attribute in TextSpan.Attributes)
            {
                if (AllowedAttributes.Count == 0
                    || AllowedAttributes.Contains(attribute.Name!))
                {
                    writer.WriteLine($"{attribute.Name}={attribute.Value}");
                }
            }
        }
        // empty
        writer.WriteLine();
    }

    private TextSpan? ReadToken(TextReader reader, int documentId)
    {
        TextSpan TextSpan = new()
        {
            DocumentId = documentId
        };

        // #pos NxN
        string? head = reader.ReadLine();
        if (head == null) return null;

        Match m = _tokHeadRegex.Match(head);
        Debug.Assert(m.Success);
        TextSpan.SetPositions(int.Parse(m.Groups["p"].Value,
            CultureInfo.InvariantCulture));
        TextSpan.Index = int.Parse(m.Groups["i"].Value,
            CultureInfo.InvariantCulture);
        TextSpan.Length = short.Parse(m.Groups["l"].Value,
            CultureInfo.InvariantCulture);

        // value
        TextSpan.Value = reader.ReadLine()!;

        // attributes
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0) break;
            int i = line.IndexOf('=');
            Debug.Assert(i > -1);
            TextSpan.AddAttribute(new Corpus.Core.Attribute
            {
                TargetId = TextSpan.P1,
                Name = line[..i],
                Value = line[(i + 1)..]
            });
        }

        return TextSpan;
    }

    private void CreateFile(int documentId, bool reset)
    {
        _writeDocId = documentId;
        _writeFileNr = reset ? 1 : _writeFileNr + 1;
        _writtenTokenCount = 0;
        _writer = new StreamWriter(
            new FileStream(GetFilePath(documentId, _writeFileNr),
            FileMode.Create, FileAccess.Write, FileShare.Read),
            Encoding.UTF8);
    }

    private bool LoadFileTokens(int documentId, int block)
    {
        string file = GetFilePath(documentId, block);
        if (!File.Exists(file))
        {
            CloseReadDocument();
            return false;
        }

        using (StreamReader reader = new(
            GetFilePath(documentId, block), Encoding.UTF8))
        {
            TextSpan? TextSpan;
            while ((TextSpan = ReadToken(reader, documentId)) != null)
            {
                _readTokens.Add(TextSpan);
            }
            _readDocId = documentId;
            _readFileNr = block;
        }
        return true;
    }

    /// <summary>
    /// Adds the specified tokens to the cache. The tokens must all
    /// belong to the same document. As this is a forward-only cache,
    /// tokens are appended to the existing document if any.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="tokens">The tokens.</param>
    /// <param name="content">The document's content. Pass this when
    /// you want to add a TextSpan attribute named <c>text</c> with value
    /// equal to the original TextSpan. This can be required in some scenarios,
    /// e.g. for deferred POS tagging.</param>
    /// <exception cref="ArgumentNullException">tokens</exception>
    public void AddSpans(int documentId, IList<TextSpan> tokens,
        string? content = null)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        if (_writeDocId != documentId)
        {
            CloseWriteDocument();
            CreateFile(documentId, _writeDocId == 0);
        }
        foreach (TextSpan TextSpan in tokens)
        {
            if (TokensPerFile > 0 && ++_writtenTokenCount > TokensPerFile)
            {
                CloseWriteDocument();
                CreateFile(documentId, false);
            }

            if (content != null)
            {
                TextSpan.AddAttribute(new Corpus.Core.Attribute
                {
                    TargetId = TextSpan.DocumentId,
                    Name = "text",
                    Value = content.Substring(TextSpan.Index, TextSpan.Length),
                });
                TextSpan.AddAttribute(new Corpus.Core.Attribute
                {
                    TargetId = TextSpan.DocumentId,
                    Name = "text-pos",
                    Type = Corpus.Core.AttributeType.Number,
                    Value = TextSpan.P1.ToString(CultureInfo.InvariantCulture)
                });
            }

            WriteToken(TextSpan, _writer!);
        }
    }

    /// <summary>
    /// Gets the specified TextSpan from the cache. As this is a forward-only
    /// cache, it is assumed that tokens are read sequentially from each
    /// document. Thus, this will return null if you try getting a TextSpan
    /// whose position is before that of the last TextSpan got.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="position">The TextSpan's position.</param>
    /// <returns>TextSpan, or null if not found.</returns>
    public TextSpan? GetSpan(int documentId, int position)
    {
        // if another doc is requested or no tokens were read, load 1st file
        if (_readDocId != documentId || _readTokens.Count == 0)
        {
            CloseReadDocument();
            if (!LoadFileTokens(documentId, 1)) return null;
        }

        // if file is open but no tokens are present or requested position
        // is before the current file, not found (forward-only)
        if (_readTokens.Count == 0 || _readTokens[0].P1 > position)
        {
            return null;
        }

        // find the file including the requested TextSpan position
        while (_readTokens[^1].P1 < position)
        {
            // load next file: if no more files, not found
            if (!LoadFileTokens(documentId, ++_readFileNr))
            {
                CloseReadDocument();
                return null;
            }
        }

        // return TextSpan from loaded file, if any
        return _readTokens.Find(t => t.P1 == position);
    }
}
