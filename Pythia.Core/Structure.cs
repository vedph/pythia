using System.Collections.Generic;
using System.Linq;
using System.Text;
using Corpus.Core;
using Attribute = Corpus.Core.Attribute;

namespace Pythia.Core
{
    /// <summary>
    /// Document text structure (e.g. section, paragraph, stanza, verse, etc.).
    /// </summary>
    /// <seealso cref="IHasAttributes" />
    public class Structure : IHasAttributes
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the document identifier.
        /// </summary>
        public int DocumentId { get; set; }

        /// <summary>
        /// Gets or sets the start token position for this structure.
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// Gets or sets the end token position for this structure (inclusive).
        /// </summary>
        public int EndPosition { get; set; }

        /// <summary>
        /// Gets or sets the structure name (e.g. <c>line</c>, <c>stanza</c>,
        /// <c>chapter</c>, and the like).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the structure's attributes.
        /// </summary>
        public IList<Attribute> Attributes { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Structure"/> class.
        /// </summary>
        public Structure()
        {
            Attributes = new List<Attribute>();
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Name).Append(" #").Append(DocumentId)
                .Append(':').Append(StartPosition).Append('-').Append(EndPosition)
                .Append(' ');
            if (Attributes != null)
            {
                sb.Append(' ');
                sb.AppendJoin(", ",
                    from a in Attributes
                    orderby a.Name
                    select $"{a.Name}={a.Value}");
            }
            return sb.ToString();
        }
    }
}
