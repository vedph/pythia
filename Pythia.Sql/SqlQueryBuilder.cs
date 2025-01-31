﻿using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Pythia.Core;
using Pythia.Core.Analysis;
using Pythia.Core.Query;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
    static private readonly Regex _docRegex = new(@"\@(\[[^;]+);",
        RegexOptions.Compiled);

    static private readonly Regex _nonPrivDocAttrRegex = new(
        @"(?:\[([a-zA-Z_][-0-9a-zA-Z_]*)[^]]*\])+",
        RegexOptions.Compiled);

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

    private static bool HasNonPrivilegedDocAttrs(string? query)
    {
        if (string.IsNullOrEmpty(query)) return false;

        Match m = _docRegex.Match(query);
        if (!m.Success) return false;

        foreach (Match am in _nonPrivDocAttrRegex.Matches(m.Groups[1].Value))
        {
            if (!TextSpan.IsPrivilegedDocAttr(am.Groups[1].Value))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Builds an SQL query from the specified Pythia query.
    /// </summary>
    /// <param name="request">The Pythia query request.</param>
    /// <returns>A tuple with 1=results page SQL query, and 2=total count
    /// SQL query.</returns>
    /// <exception cref="ArgumentNullException">request</exception>
    public Tuple<string, string> Build(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        AntlrInputStream input = new(request.Query);
        pythiaLexer lexer = new(input);
        CommonTokenStream tokens = new(lexer);
        pythiaParser parser = new(tokens);

        // throw at any parser error
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new ThrowingErrorListener());

        pythiaParser.QueryContext tree = parser.query();
        ParseTreeWalker walker = new();

        SqlPythiaListenerState state = new(lexer.Vocabulary, _sqlHelper)
        {
            HasNonPrivilegedDocAttrs = HasNonPrivilegedDocAttrs(request.Query)
        };

        // first pass
        SqlPythiaPairListener pairListener = new(state);
        if (LiteralFilters?.Count > 0)
        {
            foreach (ILiteralFilter filter in LiteralFilters)
                pairListener.LiteralFilters.Add(filter);
        }
        walker.Walk(pairListener, tree);

        // second pass
        SqlPythiaQueryListener queryListener = new(state)
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };
        if (request.SortFields?.Count > 0)
        {
            foreach (string field in request.SortFields)
                queryListener.SortFields.Add(field);
        }

        walker.Walk(queryListener, tree);
        return Tuple.Create(queryListener.GetSql(false)!, queryListener.GetSql(true)!);
    }
}
