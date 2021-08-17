using System.Text;

namespace Pythia.Core.Analysis
{
    /// <summary>
    /// A query literal value filter. This is used to filter literal values
    /// in queries for those comparison operators involving literals only,
    /// i.e. equals, not equals, starts with, ends with, contains).
    /// For instance, if you enter a query like <c>[value="Città"]</c>, an
    /// Italian literal filter might transform its value into <c>citta</c>.
    /// </summary>
    public interface ILiteralFilter
    {
        /// <summary>
        /// Applies the filter to the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        void Apply(StringBuilder text);
    }
}
