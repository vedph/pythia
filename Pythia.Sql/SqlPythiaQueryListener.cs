using Antlr4.Runtime.Tree;
using Pythia.Core.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Pythia.Core.Query.pythiaParser;

namespace Pythia.Sql;

/// <summary>
/// Second pass SQL listener. This listener builds the SQL code for a query
/// by looking at txtExpr nodes.
/// </summary>
/// <seealso cref="pythiaBaseListener" />
public class SqlPythiaQueryListener(SqlPythiaListenerState state)
    : pythiaBaseListener
{
    private sealed class SqlPart
    {
        public string TableName { get; set; } = "";
        public string SqlCode { get; set; } = "";
        public bool IsSubquery { get; set; }

        public override string ToString()
        {
            return $"{TableName}{(IsSubquery? "^" : "")}: {SqlCode}";
        }
    }

    static private readonly Regex _fromRegex = new(@"\bFROM\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly SqlPythiaListenerState _state =
        state ?? throw new ArgumentNullException(nameof(state));
    private readonly Stack<string> _sqlParts = [];
    private readonly LocationHelper _location =
        new(state.Vocabulary, state.SqlHelper);

    private int _pairNumber;
    private int _subqueryCounter;
    private readonly StringBuilder _cteList = new();
    private readonly StringBuilder _cteResult = new();
    private string? _dataSql;
    private string? _countSql;

    #region Properties
    /// <summary>
    /// Page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets the optional sort fields. If not specified, the query will sort
    /// by document's sort key. Otherwise, it will sort by all the fields
    /// specified here, in their order.
    /// </summary>
    public IList<string> SortFields { get; } = [];
    #endregion

    /// <summary>
    /// Gets the SQL built by this listener.
    /// </summary>
    /// <param name="count">if set to <c>true</c>, get the total count
    /// query; else get the results page query.</param>
    /// <returns>SQL string</returns>
    public string? GetSql(bool count) => count ? _countSql : _dataSql;

    //private void Reset()
    //{
    //    _state.Reset();
    //    _cteList.Clear();
    //    _cteResult.Clear();
    //    _dataSql = null;
    //    _countSql = null;
    //    _location.Reset();
    //    _sqlParts.Clear();
    //    _pairNumber = 0;
    //    _subqueryCounter = 0;
    //}

    #region Final
    /// <summary>
    /// Builds the SQL corresponding to the list of fields to sort by.
    /// The default is <c>sort_key, p1</c>.
    /// </summary>
    /// <returns>code</returns>
    private string BuildSortSql()
    {
        // if no sort fields are specified, use the default
        if (SortFields == null || SortFields.Count == 0)
        {
            return "sort_key, p1";
        }

        // else append each field in its order optionally with DESC
        StringBuilder sb = new();
        foreach (string field in SortFields.Where(f => f.Length > 0))
        {
            string f = field.ToLowerInvariant();
            bool desc = false;
            if (f[0] == '+')
            {
                f = f[1..];
            }
            else if (f[0] == '-')
            {
                f = f[1..];
                desc = true;
            }

            if (sb.Length > 0) sb.Append(", ");
            switch (f)
            {
                case "id":
                case "author":
                case "title":
                case "source":
                case "profile_id":
                case "user_id":
                case "date_value":
                case "sort_key":
                case "last_modified":
                    sb.Append("document.").Append(f);
                    break;
                default:
                    sb.Append(
                        "(SELECT da.value FROM document_attribute da " +
                        "WHERE da.document_id=d.id AND da.name=')")
                        .Append(_state.SqlHelper.SqlEncode(f))
                        .Append("' ORDER BY da.value LIMIT 1)");
                    break;
            }
            if (desc) sb.Append(" DESC");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Gets the final SELECT query. This uses <see cref="BuildSortSql"/>
    /// to build the ORDER BY clause.
    /// </summary>
    /// <param name="count">if set to <c>true</c>, build the query for the
    /// total count; else build the query for the requested page of data,
    /// adding an INNER JOIN to document to add some document metadata.</param>
    /// <returns>SQL.</returns>
    private string GetFinalSelect(bool count)
    {
        if (count)
        {
            return "SELECT COUNT(*) FROM r\n";
        }

        int skipCount = (PageNumber - 1) * PageSize;

        // custom sort
        string sort = BuildSortSql();

        return "SELECT DISTINCT r.id, r.document_id, r.p1, r.p2, r.type,\n"
            + "r.index, r.length, r.value,\n"
            + "document.author, document.title, document.sort_key\n"
            + "FROM r\n"
            + "INNER JOIN document ON r.document_id=document.id\n"
            + "ORDER BY " + sort + "\n" +
            _state.SqlHelper.BuildPaging(skipCount, PageSize);
    }
    #endregion

    #region Query
    /// <summary>
    /// Enter a parse tree produced by <see cref="M:pythiaParser.query" />:
    /// this resets the state of the listener, adds simple pairs to the CTE list,
    /// and opens the CTE result.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void EnterQuery([NotNull] QueryContext context)
    {
        // build CTE list for simple pairs
        _cteList.Append("-- CTE list\n");
        foreach (var p in _state.PairCteQueries.OrderBy(p => p.Key))
        {
            // "WITH s1 AS" or just ", sN AS"
            if (p.Key == "s1") _cteList.Append("WITH s1 AS\n(\n");
            else _cteList.Append(", ").Append(p.Key).Append(" AS\n(\n");

            // append the subquery
            _cteList.Append(p.Value).Append(") -- ").AppendLine(p.Key);
        }

        // open CTE result
        _cteResult.Append("-- result\n, r AS\n(\n");
    }

    /// <summary>
    /// Exit a parse tree produced by <see cref="M:pythiaParser.query" />;
    /// this closes the result CTE and composes the final SQL.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void ExitQuery([NotNull] QueryContext context)
    {
        // output CTE result
        string sql = string.Join(" ", _sqlParts.Reverse());
        _cteResult.Append(sql);

        // close CTE result
        if (_cteResult.Length > 0 && _cteResult[^1] != '\n')
            _cteResult.Append('\n');
        _cteResult.Append(") -- r\n");

        // compose the queries
        string body = _cteList.ToString() + _cteResult;
        _dataSql = body + "-- merger\n" + GetFinalSelect(false);
        _countSql = body + "-- merger\n" + GetFinalSelect(true);
    }
    #endregion

    /// <summary>
    /// Enter a parse tree produced by the <c>tePair</c>
    /// labeled alternative in <see cref="M:Pythia.Core.Query.pythiaParser.txtExpr" />.
    /// This adds a <c>SELECT * FROM sN</c>> subquery to the SQL parts stack.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void EnterTePair(TePairContext context)
    {
        _sqlParts.Push($"SELECT * FROM s{++_pairNumber}");
    }

    /// <summary>
    /// Enter a parse tree produced by <see cref="M:pythiaParser.locop" />.
    /// <para>When a locop is entered, locop args are cleared, the operator
    /// is read and stored into argument ARG_OP, and its eventual negation
    /// under ARG_NOT.</para>
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void EnterLocop([NotNull] LocopContext context)
    {
        _location.LocopArgs.Clear();
        ITerminalNode op = (context.GetChild(0) as ITerminalNode)!;

        _location.SetLocop(op.Symbol.Type,
            LocationState.IsNotFn(op.Symbol.Type));
    }

    /// <summary>
    /// Exit a parse tree produced by <see cref="M:Pythia.Core.Query.pythiaParser.locop" />.
    /// This validates the LOCOP arguments and supplies the missing ones.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void ExitLocop([NotNull] LocopContext context)
    {
        // validate and supply args
        _location.ValidateArgs(context, _state.Vocabulary);
    }

    /// <summary>
    /// Enter a parse tree produced by <see cref="M:pythiaParser.locnArg" />,
    /// i.e. a numeric argument for a locop.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void EnterLocnArg([NotNull] LocnArgContext context)
    {
        // type n=1
        string name = context.GetChild(0).GetText();
        string value = context.GetChild(2).GetText();
        _location.LocopArgs[name] =
            int.Parse(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Enter a parse tree produced by <see cref="M:pythiaParser.locsArg" />,
    /// i.e. a string argument for a locop.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void EnterLocsArg([NotNull] LocsArgContext context)
    {
        // type s=l
        string name = context.GetChild(0).GetText();
        _location.LocopArgs[name] = context.GetChild(2).GetText();
    }

    private SqlPart GetSqlPart(string sql)
    {
        Match match = _fromRegex.Match(sql);

        if (match.Success && !sql.Contains("UNION") &&
            !sql.Contains("INTERSECT") && !sql.Contains("EXCEPT"))
        {
            return new SqlPart
            {
                TableName = match.Groups[1].Value,
                SqlCode = sql,
                IsSubquery = false
            };
        }
        else
        {
            string subqueryName = $"q{++_subqueryCounter}";
            return new SqlPart
            {
                TableName = subqueryName,
                SqlCode = $"({sql})",
                IsSubquery = true
            };
        }
    }

    /// <summary>
    /// Exit a parse tree produced by the <c>teLocation</c>
    /// labeled alternative in <see cref="M:Pythia.Core.Query.pythiaParser.txtExpr" />.
    /// This pops the right side of the LOCOP, the left side of the LOCOP,
    /// then appends the LOCOP function call to the SQL parts stack.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void ExitTeLocation(TeLocationContext context)
    {
        // get the location terms
        string rightSql = _sqlParts.Pop();
        string leftSql = _sqlParts.Pop();

        SqlPart leftPart = GetSqlPart(leftSql);
        SqlPart rightPart = GetSqlPart(rightSql);

        LocopContext locop = context.locop();

        // append fn comment
        StringBuilder sql = new();
        _location.AppendLocopFnComment(sql);

        // append the SQL code
        bool negated =
            locop.NOTINSIDE() != null || locop.NOTBEFORE() != null ||
            locop.NOTAFTER() != null || locop.NOTNEAR() != null ||
            locop.NOTOVERLAPS() != null;

        if (negated)
        {
            sql.AppendLine($"SELECT * FROM {(leftPart.IsSubquery ?
                leftPart.SqlCode : leftPart.TableName)}");
            if (leftPart.IsSubquery) sql.Append($" AS {leftPart.TableName}");

            sql.AppendLine(" WHERE NOT EXISTS (");
            sql.Append($"SELECT 1 FROM {(rightPart.IsSubquery ?
                rightPart.SqlCode : rightPart.TableName)}");
            if (rightPart.IsSubquery) sql.Append($" AS {rightPart.TableName}");
            sql.AppendLine();
            sql.AppendLine($"WHERE {leftPart.TableName}.document_id = " +
                $"{rightPart.TableName}.document_id AND");
            _location.AppendLocopFn(leftPart.TableName, rightPart.TableName, sql);
            sql.AppendLine(")");
        }
        else
        {
            sql.Append($"SELECT {leftPart.TableName}.* FROM " +
                $"{(leftPart.IsSubquery ? leftPart.SqlCode : leftPart.TableName)}");
            if (leftPart.IsSubquery) sql.Append($" AS {leftPart.TableName}");
            sql.AppendLine();
            sql.Append($"INNER JOIN {(rightPart.IsSubquery ?
                rightPart.SqlCode : rightPart.TableName)}");
            if (rightPart.IsSubquery) sql.Append($" AS {rightPart.TableName}");
            sql.AppendLine();
            sql.AppendLine($"ON {leftPart.TableName}.document_id = " +
                $"{rightPart.TableName}.document_id AND");
            _location.AppendLocopFn(leftPart.TableName, rightPart.TableName, sql);
        }

        _sqlParts.Push(sql.ToString());
    }

    /// <summary>
    /// Exit a parse tree produced by the <c>teLogical</c>
    /// labeled alternative in <see cref="M:Pythia.Core.Query.pythiaParser.txtExpr" />.
    /// This pops the right side of the logical operator, the left side of the
    /// logical operator, and appends both joined by the corresponding SQL
    /// set operator to the SQL parts stack.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void ExitTeLogical(TeLogicalContext context)
    {
        string right = _sqlParts.Pop();
        string left = _sqlParts.Pop();
        string op = context.AND() != null ? "INTERSECT" :
                    context.ANDNOT() != null ? "EXCEPT" :
                    "UNION";

        _sqlParts.Push($"({left} {op} {right})");
    }
}
