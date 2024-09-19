using Antlr4.Runtime;
using System;
using System.Collections.Generic;

namespace Pythia.Sql;

/// <summary>
/// State for the SQL Pythia listener.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SqlPythiaListenerState"/> class.
/// </remarks>
/// <param name="vocabulary">The vocabulary.</param>
/// <param name="sqlHelper">The SQL helper.</param>
/// <exception cref="ArgumentNullException">vocabulary or sqlHelper</exception>
public class SqlPythiaListenerState(IVocabulary vocabulary, ISqlHelper sqlHelper)
{
    /// <summary>
    /// Gets the grammar vocabulary.
    /// </summary>
    public IVocabulary Vocabulary { get; } = vocabulary ??
            throw new ArgumentNullException(nameof(vocabulary));

    /// <summary>
    /// Gets the SQL helper.
    /// </summary>
    public ISqlHelper SqlHelper { get; } = sqlHelper ??
            throw new ArgumentNullException(nameof(sqlHelper));

    /// <summary>
    /// Gets or sets a value indicating whether the query has non-privileged
    /// document attributes.
    /// </summary>
    public bool HasNonPrivilegedDocAttrs { get; set; }

    /// <summary>
    /// Gets the pair subqueries, a dictionary where the key is the pair subquery
    /// name like <c>s1</c>, <c>s2</c>, etc., and the value is the subquery SQL code.
    /// </summary>
    public Dictionary<string, string> PairCteQueries { get; } = [];

    /// <summary>
    /// Resets this state.
    /// </summary>
    public void Reset()
    {
        PairCteQueries.Clear();
    }
}
