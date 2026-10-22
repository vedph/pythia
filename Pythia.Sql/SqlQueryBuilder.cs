using Antlr4.Runtime;
using Pythia.Core;
using Pythia.Core.Analysis;
using Pythia.Core.Query;
using System;
using System.Collections.Generic;

namespace Pythia.Sql;

/// <summary>
/// SQL query builder.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SqlQueryBuilder"/> class.
/// </remarks>
/// <param name="sqlHelper">The SQL helper.</param>
/// <exception cref="ArgumentNullException">sqlHelper</exception>
public sealed class SqlQueryBuilder(ISqlHelper sqlHelper)
{
    private readonly ISqlHelper _sqlHelper = sqlHelper
        ?? throw new ArgumentNullException(nameof(sqlHelper));

    /// <summary>
    /// Gets or sets the optional literal filters.
    /// </summary>
    public IList<ILiteralFilter>? LiteralFilters { get; set; }

    /// <summary>
    /// Gets the type of the specified privileged attribute.
    /// </summary>
    /// <param name="name">The attribute name. If this is not a privileged
    /// attribute name, 0 will be returned.</param>
    /// <returns>The attribute type.</returns>
    public static int GetPrivilegedAttrType(string name)
    {
        return name switch
        {
            "date_value" or "p1" or "p2" or "index" or "length" => 1,
            _ => 0,
        };
    }

    /// <summary>
    /// Parses the specified Pythia query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns>The query parse tree.</returns>
    /// <exception cref="ArgumentNullException">query</exception>
    /// <exception cref="PythiaQueryException">syntax error</exception>
    public static pythiaParser.QueryContext Parse(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        AntlrInputStream input = new(query);
        pythiaLexer lexer = new(input);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(new ThrowingLexerErrorListener());

        CommonTokenStream tokens = new(lexer);
        pythiaParser parser = new(tokens);

        // throw at any parser error
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new ThrowingErrorListener());

        return parser.query();
    }

    /// <summary>
    /// Builds an SQL query from the specified Pythia query.
    /// </summary>
    /// <param name="request">The Pythia query request.</param>
    /// <returns>A tuple with 1=results page SQL query, and 2=total count
    /// SQL query.</returns>
    /// <exception cref="ArgumentNullException">request</exception>
    /// <exception cref="PythiaQueryException">query error</exception>
    public Tuple<string, string> Build(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        pythiaParser.QueryContext tree = Parse(request.Query ?? "");
        SqlPythiaTranslator translator = new(_sqlHelper, LiteralFilters);
        return translator.Translate(tree, request.PageNumber, request.PageSize,
            request.SortFields);
    }
}
