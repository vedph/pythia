using Pythia.Core;
using System;

namespace Pythia.Sql;

/// <summary>
/// SQL-based terms query builder.
/// </summary>
public interface ISqlLemmaQueryBuilder
{
    /// <summary>
    /// Builds the SQL queries corresponding to the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>SQL queries for data and their total count.</returns>
    Tuple<string, string> Build(LemmaFilter filter);
}
