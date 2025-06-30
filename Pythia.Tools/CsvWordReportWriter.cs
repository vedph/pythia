using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Pythia.Tools;

/// <summary>
/// CSV writer for word check results.
/// </summary>
public sealed class CsvWordReportWriter : IWordReportWriter, IDisposable
{
    private StreamWriter? _writer;
    private CsvWriter? _csv;
    private bool _headerWritten;
    private bool _disposed;

    /// <summary>
    /// Opens the writer to write to the specified target file.
    /// </summary>
    /// <param name="target">The path of the output file.</param>
    public void Open(string target)
    {
        ArgumentNullException.ThrowIfNull(target);

        _writer = new StreamWriter(target, false, Encoding.UTF8);
        _csv = new CsvWriter(_writer, CultureInfo.InvariantCulture);
        _headerWritten = false;
    }

    private void WriteHeader()
    {
        if (_csv == null) return;

        // WordToCheck properties
        _csv.WriteField(nameof(WordToCheck.Id));
        _csv.WriteField(nameof(WordToCheck.Language));
        _csv.WriteField(nameof(WordToCheck.Value));
        _csv.WriteField(nameof(WordToCheck.Pos));
        _csv.WriteField(nameof(WordToCheck.LemmaId));
        _csv.WriteField(nameof(WordToCheck.Lemma));

        // WordCheckResult properties
        _csv.WriteField(nameof(WordCheckResult.Type));
        _csv.WriteField(nameof(WordCheckResult.Message));
        _csv.WriteField(nameof(WordCheckResult.Action));

        // data dictionary entries with col_ prefix
        _csv.WriteField(nameof(WordCheckResult.Data));

        _csv.NextRecord();
    }

    /// <summary>
    /// Writes a word check result to the CSV file.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <exception cref="InvalidOperationException">not opened.</exception>
    public void Write(WordCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (_csv == null)
        {
            throw new InvalidOperationException(
                "Writer not opened. Call Open() first.");
        }

        // write header only once
        if (!_headerWritten)
        {
            WriteHeader();
            _headerWritten = true;
        }

        WriteRecord(result);
    }

    private void WriteRecord(WordCheckResult result)
    {
        if (_csv == null) return;

        // WordToCheck properties
        _csv.WriteField(result.Source.Id);
        _csv.WriteField(result.Source.Language);
        _csv.WriteField(result.Source.Value);
        _csv.WriteField(result.Source.Pos);
        _csv.WriteField(result.Source.LemmaId);
        _csv.WriteField(result.Source.Lemma);

        // WordCheckResult properties
        _csv.WriteField(result.Type);
        _csv.WriteField(result.Message);
        _csv.WriteField(result.Action);

        // data dictionary entries with col_ prefix
        string dataValue = "";
        if (result.Data != null)
        {
            IEnumerable<string> colEntries = result.Data
                .Where(kvp => kvp.Key.StartsWith("col_",
                    StringComparison.OrdinalIgnoreCase))
                .Select(kvp => $"{kvp.Key}={kvp.Value}");
            dataValue = string.Join(";", colEntries);
        }
        _csv.WriteField(dataValue);

        _csv.NextRecord();
    }

    /// <summary>
    /// Close this writer and release resources.
    /// </summary>
    public void Close()
    {
        if (_csv != null)
        {
            _csv.Flush();
            _csv.Dispose();
            _csv = null;
        }

        if (_writer != null)
        {
            _writer.Dispose();
            _writer = null;
        }
    }

    /// <summary>
    /// Disposes the writer and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Close();
            _disposed = true;
        }
    }
}
