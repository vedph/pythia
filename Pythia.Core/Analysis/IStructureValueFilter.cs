using System.Text;

namespace Pythia.Core.Analysis;

/// <summary>
/// A filter applied by <see cref="IStructureParser"/>'s to the structure's
/// values.
/// </summary>
public interface IStructureValueFilter
{
    /// <summary>
    /// Applies this filter to the specified text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="structure">The structure being parsed.</param>
    void Apply(StringBuilder text, Structure? structure);
}
