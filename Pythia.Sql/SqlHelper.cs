using System;
using System.Globalization;
using System.Text;

namespace Pythia.Sql
{
    /// <summary>
    /// Base helper class for SQL query builders.
    /// </summary>
    public abstract class SqlHelper
    {
        /// <summary>
        /// Gets or sets the default fuzzy matching minimum treshold.
        /// Default is 0.9.
        /// </summary>
        public double DefaultFuzzyTreshold { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlHelper"/> class.
        /// </summary>
        protected SqlHelper()
        {
            DefaultFuzzyTreshold = 0.9D;
        }

        /// <summary>
        /// Parses the value of a fuzzy clause, which is either a simple text
        /// to match, or this text followed by the minimum treshold for matching
        /// introduced by a colon. If no treshold is specified,
        /// <see cref="DefaultFuzzyTreshold"/> is used.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Tuple where 1=text and 2=treshold.</returns>
        protected Tuple<string, double> ParseFuzzyValue(string value)
        {
            // extract treshold from value (N:T) or assume default if not specified
            string text = value;
            double treshold = DefaultFuzzyTreshold;

            int i = value.LastIndexOf(':');
            if (i > -1)
            {
                text = value.Substring(0, i);
                if (!double.TryParse(value.Substring(i + 1), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out treshold))
                {
                    treshold = DefaultFuzzyTreshold;
                }
            }
            return Tuple.Create(text, treshold);
        }

        /// <summary>
        /// Encodes the specified date (or date and time) value for SQL.
        /// </summary>
        /// <param name="dt">The value.</param>
        /// <param name="time">if set to <c>true</c>, include the time.</param>
        /// <returns>SQL-encoded value</returns>
        public string SqlEncode(DateTime dt, bool time)
        {
            return time ?
                $"'{dt.Year:0000}-{dt.Month:00}-{dt.Day:00}T{dt.Hour:00}:" +
                    $"{dt.Minute:00}:{dt.Second:00}.000Z'" :
                $"'{dt.Year:0000}{dt.Month:00}{dt.Day:00}'";
        }

        /// <summary>
        /// Encodes the specified literal character for SQL.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <param name="hasWildcards">if set to <c>true</c>, the text value
        /// has wildcards.</param>
        /// <param name="sb">The target string builder.</param>
        protected static void EncodeSqlAnsiChar(char c, bool hasWildcards,
            StringBuilder sb)
        {
            if (sb is null) throw new ArgumentNullException(nameof(sb));

            switch (c)
            {
                case '\'':
                    sb.Append("''");
                    break;
                case '_':
                    if (hasWildcards) sb.Append(c);
                    else sb.Append("\\_");
                    break;
                case '%':
                    if (hasWildcards) sb.Append(c);
                    else sb.Append("\\%");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
    }
}
