using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusi.Tools.Data;

namespace Pythia.Core
{
    /// <summary>
    /// Index terms browser filter.
    /// </summary>
    public class TermFilter : PagingOptions
    {
        /// <summary>
        /// Gets or sets the corpus identifier; if specified, documents must
        /// have a corpus ID equal to this value.
        /// </summary>
        public string CorpusId { get; set; }

        /// <summary>
        /// Gets or sets the author. If specified, documents must have
        /// an author containing this value.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the title. If specified, documents must have
        /// a title containing this value.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the source. If specified, documents must have
        /// a source containing this value.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the profile identifier. If specified, documents must
        /// have a profile equal to this value.
        /// </summary>
        public string ProfileId { get; set; }

        /// <summary>
        /// Gets or sets the minimum date value.
        /// </summary>
        public double MinDateValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum date value.
        /// </summary>
        public double MaxDateValue { get; set; }

        /// <summary>
        /// Gets or sets the minimum time modified.
        /// </summary>
        public DateTime? MinTimeModified { get; set; }

        /// <summary>
        /// Gets or sets the maximum time modified.
        /// </summary>
        public DateTime? MaxTimeModified { get; set; }

        /// <summary>
        /// Gets or sets the document's attributes to match. Each attribute
        /// filter is a tuple where 1=name and 2=value. The value must be
        /// contained in the attribute's value.
        /// </summary>
        public List<Tuple<string, string>> DocumentAttributes { get; set; }

        /// <summary>
        /// Gets or sets the token's attributes to match. Each attribute filter
        /// is a tuple where 1=name and 2=value. The value must be contained
        /// in the attribute's value.
        /// </summary>
        public List<Tuple<string, string>> TokenAttributes { get; set; }

        /// <summary>
        /// Gets or sets the value pattern. This can include wildcards <c>?</c>
        /// and <c>*</c>.
        /// </summary>
        public string ValuePattern { get; set; }

        /// <summary>
        /// Gets or sets the token's minimum frequency; 0=not set.
        /// </summary>
        public int MinCount { get; set; }

        /// <summary>
        /// Gets or sets the token's maximum frequency; 0=not set.
        /// </summary>
        public int MaxCount { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        public TermSortOrder SortOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether sort is descending 
        /// rather than ascending.
        /// </summary>
        public bool IsSortDescending { get; set; }

        /// <summary>
        /// True if this filter includes document-related filters.
        /// </summary>
        /// <returns>true if document-related filters are set</returns>
        public bool HasDocumentFilters() =>
            !string.IsNullOrEmpty(CorpusId)
            || !string.IsNullOrEmpty(Author)
            || !string.IsNullOrEmpty(Title)
            || !string.IsNullOrEmpty(Source)
            || !string.IsNullOrEmpty(ProfileId)
            || MinDateValue != 0
            || MaxDateValue != 0
            || MinTimeModified.HasValue
            || MaxTimeModified.HasValue
            || (DocumentAttributes?.Any() == true);

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new();

            const string SEP = " | ";
            if (!string.IsNullOrEmpty(CorpusId))
                sb.Append("CorpusId=").Append(CorpusId).Append(SEP);
            if (!string.IsNullOrEmpty(Author))
                sb.Append("Author=").Append(Author).Append(SEP);
            if (!string.IsNullOrEmpty(Title))
                sb.Append("Title=").Append(Title).Append(SEP);
            if (!string.IsNullOrEmpty(Source))
                sb.Append("Source=").Append(Source).Append(SEP);
            if (!string.IsNullOrEmpty(ProfileId))
                sb.Append("ProfileId=").Append(ProfileId).Append(SEP);

            if (MinDateValue != 0)
                sb.Append("MinDateValue=").Append(MinDateValue).Append(SEP);
            if (MaxDateValue != 0)
                sb.Append("MaxDateValue=").Append(MaxDateValue).Append(SEP);

            if (MinTimeModified.HasValue)
                sb.Append("MinTimeModified=").Append(MinTimeModified).Append(SEP);
            if (MaxTimeModified.HasValue)
                sb.Append("MaxTimeModified=").Append(MaxTimeModified).Append(SEP);

            if (DocumentAttributes?.Count > 0)
                sb.AppendJoin(", ", DocumentAttributes).Append(SEP);

            return sb.ToString();
        }
    }

    /// <summary>
    /// Terms list sort order.
    /// </summary>
    public enum TermSortOrder
    {
        /// <summary>The default sort order (=by value).</summary>
        Default = 0,

        /// <summary>Sort by term's value.</summary>
        ByValue,

        /// <summary>Sort by reversed term's value.</summary>
        ByReversedValue,

        /// <summary>Sort by term's count.</summary>
        ByCount
    }
}
