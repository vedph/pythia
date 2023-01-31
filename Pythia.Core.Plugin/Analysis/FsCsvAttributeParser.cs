using Corpus.Core;
using Corpus.Core.Analysis;
using Fusi.Tools.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// CSV file-based attributes parser.
    /// Tag: <c>attribute-parser.fs-csv</c>.
    /// </summary>
    /// <seealso cref="IAttributeParser" />
    [Tag("attribute-parser.fs-csv")]
    public sealed class FsCsvAttributeParser : IAttributeParser,
        IConfigurable<FsCsvAttributeParserOptions>
    {
        private readonly Dictionary<string, Tuple<string, AttributeType>> _mappings;
        private FsCsvAttributeParserOptions? _options;
        private Regex? _findRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="FsCsvAttributeParser"/>
        /// class.
        /// </summary>
        public FsCsvAttributeParser()
        {
            _mappings = new Dictionary<string, Tuple<string, AttributeType>>();
        }

        /// <summary>
        /// Configures the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">options</exception>
        public void Configure(FsCsvAttributeParserOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _findRegex = string.IsNullOrEmpty(_options.SourceFind)
                ? null : new Regex(options.SourceFind!, RegexOptions.Compiled);

            // default to 0,1 if cols not specified
            if (_options.NameColumnIndex == 0 && _options.ValueColumnIndex == 0)
                _options.ValueColumnIndex = 1;

            _mappings.Clear();
            if (_options.NameMappings?.Count > 0)
            {
                foreach (string pair in _options.NameMappings)
                {
                    int i = pair.IndexOf('=');
                    if (i == -1) continue;  // defensive
                    string name = pair[..i];
                    AttributeType type = AttributeType.Text;
                    if (name.EndsWith("#"))
                    {
                        name = name[0..^1];
                        type = AttributeType.Number;
                    }
                    _mappings[name] = Tuple.Create(pair[(i + 1)..], type);
                }
            }
        }

        /// <summary>
        /// Parses the text from the specified reader.
        /// </summary>
        /// <param name="reader">The text reader.</param>
        /// <param name="document">The document being parsed.</param>
        /// <returns>
        /// List of attributes extracted from the text.
        /// </returns>
        /// <exception cref="ArgumentNullException">reader or document</exception>
        public IList<Corpus.Core.Attribute> Parse(TextReader reader,
            IDocument document)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            // get the CSV file name
            string filePath;
            if (_findRegex != null)
            {
                filePath = _findRegex.Replace(document.Source ?? "",
                    _options?.SourceReplace ?? "");
                if (filePath.Length == 0)
                    return Array.Empty<Corpus.Core.Attribute>();
            }
            else
            {
                filePath = document.Source!;
            }

            // read file
            List<Corpus.Core.Attribute> attrs = new();
            using StreamReader stream = new(new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.Read));
            using var csv = new CsvReader(stream,
                new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = _options!.Separator ?? ",",
                HasHeaderRecord = _options.HasHeader
            });
            while (csv.Read())
            {
                string? name = csv.GetField(_options.NameColumnIndex);
                string? value = csv.GetField(_options.ValueColumnIndex);

                if (!string.IsNullOrEmpty(name))
                {
                    // trim name, and value if requested
                    name = name.Trim();
                    if (_options.ValueTrimming && value != null)
                        value = value.Trim();

                    // remap names if required
                    AttributeType type = AttributeType.Text;
                    if (_mappings.ContainsKey(name))
                    {
                        type = _mappings[name].Item2;
                        name = _mappings[name].Item1;
                    }
                    if (name.Length == 0) continue;

                    attrs.Add(new Corpus.Core.Attribute
                    {
                        Name = name,
                        Value = value,
                        Type = type
                    });
                }
            }

            return attrs;
        }
    }

    /// <summary>
    /// Options for <see cref="FsCsvAttributeParser"/>.
    /// </summary>
    public class FsCsvAttributeParserOptions
    {
        /// <summary>
        /// Gets or sets the separator (default is comma).
        /// </summary>
        public string? Separator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the first record in the CSV
        /// file corresponds to the header.
        /// </summary>
        public bool HasHeader { get; set; }

        /// <summary>
        /// Gets or sets the regular expression pattern to find in the source
        /// when replacing it with <see cref="SourceReplace"/>. If this is not
        /// set, the document's source itself will be used. The document
        /// extension should be <c>xlsx</c> or <c>xls</c> for the legacy Excel
        /// format.
        /// </summary>
        public string? SourceFind { get; set; }

        /// <summary>
        /// Gets or sets the text to replace when matching <see cref="SourceFind"/>
        /// in the document's source, so that the corresponding Excel file
        /// path can be built from it.
        /// </summary>
        public string? SourceReplace { get; set; }

        /// <summary>
        /// Gets or sets the index of the name column in the Excel file.
        /// </summary>
        public int NameColumnIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the value column in the excel file.
        /// </summary>
        public int ValueColumnIndex { get; set; }

        /// <summary>
        /// Gets or sets the name mappings. This is a list of name=value
        /// pairs, where each name is a metadata attribute name as found in the
        /// Excel file, and each value is its renamed counterpart. You can
        /// use this to rename metadata found in Excel files, or to skip some
        /// of them by mapping them to an empty string.
        /// The name can end with <c>#</c> to indicate that the attribute has
        /// a numeric rather than a string value. You can thus use mappings
        /// even when you don't want to rename attributes, but you want to set
        /// their type.
        /// </summary>
        public IList<string>? NameMappings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether attribute values should
        /// be trimmed when read from the Excel file. Attribute names are
        /// always trimmed.
        /// </summary>
        public bool ValueTrimming { get; set; }
    }
}
