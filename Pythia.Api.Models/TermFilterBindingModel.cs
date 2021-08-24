using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Pythia.Core;

namespace Pythia.Api.Models
{
    /// <summary>
    /// Terms filter model.
    /// </summary>
    public sealed class TermFilterBindingModel
    {
        /// <summary>
        /// The page number (1-N).
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; }

        /// <summary>
        /// Page size (1-N).
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; }

        /// <summary>
        /// The corpus ID.
        /// </summary>
        public string CorpusId { get; set; }

        /// <summary>
        /// Text to be found inside the authors field.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Text to be found inside the title field.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Text to be found inside the source field.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The profile ID.
        /// </summary>
        public string ProfileId { get; set; }

        /// <summary>
        /// The minimum date value.
        /// </summary>
        public double? MinDateValue { get; set; }

        /// <summary>
        /// The maximum date value.
        /// </summary>
        public double? MaxDateValue { get; set; }

        /// <summary>
        /// The minimum modified date and time value.
        /// </summary>
        public DateTime? MinTimeModified { get; set; }

        /// <summary>
        /// The maximum modified date and time value.
        /// </summary>
        public DateTime? MaxTimeModified { get; set; }

        /// <summary>
        /// The document attributes, with format name=value (or just name=),
        /// each separated by comma.
        /// </summary>
        public string DocAttributes { get; set; }

        /// <summary>
        /// The token attributes, with format name=value (or just name=),
        /// each separated by comma.
        /// </summary>
        public string TokAttributes { get; set; }

        /// <summary>
        /// The term's value pattern. This can include wildcards <c>?</c> and
        /// <c>*</c>.
        /// </summary>
        public string ValuePattern { get; set; }

        /// <summary>
        /// The token's minimum frequency; 0=not set.
        /// </summary>
        public int MinCount { get; set; }

        /// <summary>
        /// The token's maximum frequency; 0=not set.
        /// </summary>
        public int MaxCount { get; set; }

        /// <summary>
        /// Gets or sets the sort order: 0=default, 1=by value, 2=by reversed
        /// value, 3=by count.
        /// </summary>
        public TermSortOrder SortOrder { get; set; }

        /// <summary>
        /// A value indicating whether sort is descending rather than ascending.
        /// </summary>
        public bool Descending { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TermFilterBindingModel"/> class.
        /// </summary>
        public TermFilterBindingModel()
        {
            PageNumber = 1;
            PageSize = 50;
        }

        private static List<Tuple<string, string>> ParseAttributes(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            List<Tuple<string, string>> a = new List<Tuple<string, string>>();
            Regex r = new Regex("(^[^=]+)=(.*)$");
            foreach (string s in text.Split(new[] { ',' },
                StringSplitOptions.RemoveEmptyEntries))
            {
                Match m = r.Match(s);
                if (m.Success)
                    a.Add(Tuple.Create(m.Groups[1].Value, m.Groups[2].Value));
            }

            return a;
        }

        /// <summary>
        /// Converts this model to the corresponding Pythia filter.
        /// </summary>
        /// <returns>Filter.</returns>
        public TermFilter ToFilter()
        {
            return new TermFilter
            {
                PageNumber = PageNumber,
                PageSize = PageSize,
                CorpusId = CorpusId,
                Author = Author,
                Title = Title,
                Source = Source,
                ProfileId = ProfileId,
                MinDateValue = MinDateValue ?? 0,
                MaxDateValue = MaxDateValue ?? 0,
                MinTimeModified = MinTimeModified,
                MaxTimeModified = MaxTimeModified,
                DocumentAttributes = ParseAttributes(DocAttributes),
                TokenAttributes = ParseAttributes(TokAttributes),
                ValuePattern = ValuePattern,
                MinCount = MinCount,
                MaxCount = MaxCount,
                SortOrder = SortOrder,
                IsSortDescending = Descending
            };
        }
    }
}
