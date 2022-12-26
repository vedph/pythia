using Fusi.Tools.Data;
using System.Collections.Generic;

namespace Pythia.Core;

/// <summary>
/// A Pythia query request.
/// </summary>
public class SearchRequest : PagingOptions
{
    /// <summary>
    /// The query.
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// The optional sort fields. If not specified, the query will sort
    /// by document's sort key. Otherwise, it will sort by all the fields
    /// specified here, in their order.
    /// </summary>
    public IList<string>? SortFields { get; set; }
}
