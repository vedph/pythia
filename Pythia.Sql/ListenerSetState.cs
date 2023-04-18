using System.Text;

namespace Pythia.Sql;

/// <summary>
/// State for the a document/text set in <see cref="SqlPythiaListener"/>.
/// This state keeps track of the pair numbering, and contain the SQL code
/// being built for the current pair.
/// </summary>
public class ListenerSetState
{
    /// <summary>
    /// Gets or sets the pair number. This is increased whenever
    /// a pair is entered.
    /// </summary>
    public int PairNumber { get; set; }

    /// <summary>
    /// Gets the SQL being built for the current pair.
    /// </summary>
    public StringBuilder Sql { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenerSetState"/> class.
    /// </summary>
    public ListenerSetState()
    {
        Sql = new StringBuilder();
    }

    /// <summary>
    /// Resets this state.
    /// </summary>
    public virtual void Reset()
    {
        PairNumber = 0;
        Sql.Clear();
    }
}
