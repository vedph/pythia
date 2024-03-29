﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime;
using Pythia.Core.Query;
using System.Globalization;
using Pythia.Core.Analysis;
using static Pythia.Core.Query.pythiaParser;

namespace Pythia.Sql;

/// <summary>
/// A Pythia listener which builds SQL code.
/// </summary>
/// <seealso cref="pythiaBaseListener" />
public sealed class SqlPythiaListener : pythiaBaseListener
{
    private readonly IVocabulary _vocabulary;
    private readonly ISqlHelper _sqlHelper;

    // CTE list
    private readonly StringBuilder _cteList;
    // CTE result
    private readonly StringBuilder _cteResult;

    // general constants
    internal static readonly HashSet<string> PrivilegedDocAttrs =
        new(new[]
        {
            "id", "author", "title", "date_value", "sort_key", "source",
            "profile_id"
        });
    internal static readonly HashSet<string> PrivilegedTokAttrs =
        new(new[]
        {
            "value", "language", "position", "length"
        });
    internal static readonly HashSet<string> PrivilegedStrAttrs =
        new(new[]
        {
            "name", "start_position", "end_position"
        });
    private static readonly char[] _wildcards = new[] { '*', '?' };

    // state
    // the type of the current set: document, text, or corpus
    private QuerySet _currentSetType;
    // the state of the current location expression
    private readonly LocationState _locationState;
    // the corpora IDs collected from the corpus set if any
    private readonly HashSet<string> _corporaIds;
    // the SQL code for the document set
    private readonly ListenerSetState _docSetState;
    // the SQL code for the current pair in the text set
    private readonly ListenerSetState _txtSetState;
    private readonly Dictionary<IParseTree, string> _nodeIds;
    private bool _anyCte;
    // the depth of the current CTE according to brackets nesting
    private int _currentCteDepth;

    // SQL code built for the corpus filter
    private string? _corpusSql;
    // SQL code built for the document filter
    private string? _docSql;
    // SQL code for the built queries (one for data, another for total count)
    private string? _dataSql;
    private string? _countSql;

    #region Properties
    /// <summary>
    /// Page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets the optional literal filters to apply to literal values of
    /// query pairs.
    /// </summary>
    public IList<ILiteralFilter> LiteralFilters { get; }

    /// <summary>
    /// Gets the optional sort fields. If not specified, the query will sort
    /// by document's sort key. Otherwise, it will sort by all the fields
    /// specified here, in their order.
    /// </summary>
    public IList<string> SortFields { get; }
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlPythiaListener" />
    /// class.
    /// </summary>
    /// <param name="vocabulary">The vocabulary.</param>
    /// <param name="sqlHelper">The SQL helper.</param>
    /// <exception cref="ArgumentNullException">vocabulary or sqlHelper
    /// </exception>
    public SqlPythiaListener(IVocabulary vocabulary, ISqlHelper sqlHelper)
    {
        _vocabulary = vocabulary ??
            throw new ArgumentNullException(nameof(vocabulary));
        _sqlHelper = sqlHelper ??
            throw new ArgumentNullException(nameof(sqlHelper));

        _cteList = new StringBuilder();
        _cteResult = new StringBuilder();
        _docSetState = new ListenerSetState();
        _txtSetState = new ListenerSetState();
        _locationState = new LocationState(vocabulary, sqlHelper);
        _nodeIds = new Dictionary<IParseTree, string>();
        _corporaIds = new HashSet<string>();

        PageNumber = 1;
        PageSize = 20;
        LiteralFilters = new List<ILiteralFilter>();
        SortFields = new List<string>();
    }

    /// <summary>
    /// Gets the SQL built by this listener.
    /// </summary>
    /// <param name="count">if set to <c>true</c>, get the total count
    /// query; else get the results page query.</param>
    /// <returns>SQL string</returns>
    public string? GetSql(bool count) => count? _countSql : _dataSql;

    private void Reset()
    {
        _cteList.Clear();
        _cteResult.Clear();

        _currentSetType = QuerySet.Text;
        _corporaIds.Clear();
        _docSetState.Reset();
        _txtSetState.Reset();
        _locationState.Reset(true);
        _nodeIds.Clear();
        _anyCte = false;
        _currentCteDepth = 0;

        _corpusSql = null;
        _docSql = null;
        _dataSql = null;
    }

    #region Helpers
    private string EK(string keyword) =>
        _sqlHelper.EscapeKeyword(keyword);

    private string EKP(string keyword1, string keyword2, string? suffix = null) =>
        _sqlHelper.EscapeKeyword(keyword1) +
        "." +
        _sqlHelper.EscapeKeyword(keyword2) +
        (suffix ?? "");

    private static string LW(string text) => $"LOWER({text})";

    private string SQE(string text, bool hasWildcards = false,
        bool wrapInQuotes = false, bool unicode = true) =>
        _sqlHelper.SqlEncode(text, hasWildcards, wrapInQuotes, unicode);

    /// <summary>
    /// Adds the indent of the specified <paramref name="length"/> to
    /// the specified string builder. This method inserts the requested
    /// amount of spaces after each LF, and at the first line if this
    /// does not begin with LF. Should any consecutive LF's be present,
    /// no indents are inserted between them. No indent is added after
    /// a final LF, too.
    /// </summary>
    /// <param name="length">The requested indent length.</param>
    /// <param name="sb">The target string builder.</param>
    private static void AddIndent(int length, StringBuilder sb)
    {
        if (sb.Length == 0) return;

        string indent = new(' ', length);
        int i = sb.Length - 1;

        // ignore final LF's
        while (i > -1 && sb[i] == '\n') i--;

        while (i > -1)
        {
            if (sb[i] == '\n')
            {
                sb.Insert(i + 1, indent);
                while (i > -1 && sb[i] == '\n') i--;
            }
            else
            {
                i--;
            }
        }

        // first line
        if (sb[0] != '\n') sb.Insert(0, indent);
    }
    #endregion

    #region CTE List        
    /// <summary>
    /// Appends the SQL pair query in the current set state to the list
    /// of CTEs; this means adding <c>WITH sN AS (...)</c> the first time,
    /// and then <c>, sN AS (...)</c> all the subsequent times.
    /// </summary>
    private void AppendPairToCteList()
    {
        // WITH sN AS (...), sN AS (...), ...
        string sn = "s" + _txtSetState.PairNumber;

        // prefix: the first time is WITH, the rest is comma
        if (!_anyCte)
            _cteList.Append("-- CTE list\n").Append("WITH ");
        else
            _cteList.Append(", ");

        _cteList.Append(sn).Append(" AS\n");

        // update CTE state
        _anyCte = true;

        AddIndent(2, _txtSetState.Sql);

        // ... ) -- sN
        _cteList.Append("(\n")
                .Append(_txtSetState.Sql)
                .Append(") -- ").Append(sn).Append('\n');
    }
    #endregion

    #region Final    
    /// <summary>
    /// Builds the SQL corresponding to the list of fields to sort by.
    /// </summary>
    /// <returns>code</returns>
    private string BuildSortSql()
    {
        // if no sort fields are specified, use the default
        if (SortFields == null || SortFields.Count == 0)
        {
            return "document.sort_key, occurrence.position";
        }

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
                        .Append(_sqlHelper.SqlEncode(f))
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
    /// total count; else build the query for the requested page of data.</param>
    /// <returns>SQL.</returns>
    private string GetFinalSelect(bool count)
    {
        // we join occurrences on p1 or p2; these are equal for occurrences,
        // and different for structures, where they represent start and end.
        const string R_POS = "AND (occurrence.position=r.p1 OR occurrence.position=r.p2)";

        if (count)
        {
            return "SELECT COUNT(*) FROM\n"
                + "(\n"
                + "SELECT DISTINCT\n"
                + "occurrence.document_id,\n"
                + "occurrence.position,\n"
                + "entity_type,\n"
                + "entity_id\n"
                + "FROM occurrence\n"
                + "INNER JOIN token ON occurrence.token_id=token.id\n"
                + "INNER JOIN r ON\n"
                + "occurrence.document_id=r.document_id\n"
                + $"{R_POS}\n"
                + ") c\n";
        }

        int skipCount = (PageNumber - 1) * PageSize;

        // custom sort
        string sort = BuildSortSql();

        return "SELECT DISTINCT\n"
            + "occurrence.document_id,\n"
            + "occurrence.position,\n"
            + "occurrence.index,\n"
            + "occurrence.length,\n"
            + "entity_type,\n"
            + "entity_id,\n"
            + "token.value,\n"
            + "document.author,\n"
            + "document.title,\n"
            + "document.sort_key\n"
            + "FROM occurrence\n"
            + "INNER JOIN token ON occurrence.token_id=token.id\n"
            + "INNER JOIN document ON occurrence.document_id=document.id\n"
            + "INNER JOIN r ON occurrence.document_id=r.document_id\n"
            + $"{R_POS}\n"
            + "ORDER BY " + sort + "\n" +
            _sqlHelper.BuildPaging(skipCount, PageSize);
    }
    #endregion

    #region Pairs
    /// <summary>
    /// Reads the query set pair whose head (=the pair name) is represented
    /// by the <paramref name="id"/> node. A pair is built of an attribute name
    /// (prefixed by $ for structures), and usually (but optionally) by
    /// an operator and an attribute value.
    /// </summary>
    /// <param name="id">The identifier node, which starts the pair.</param>
    /// <param name="doc">if set to <c>true</c>, the pair is inside a document
    /// set; otherwise, it's inside a text set.</param>
    /// <returns>The pair.</returns>
    private QuerySetPair ReadQuerySetPair(ITerminalNode id, bool doc)
    {
        // read the initial ID/SID creating the pair
        QuerySetPair pair = new()
        {
            Name = id.GetText(),
            Number = doc?
                ++_docSetState.PairNumber :
                ++_txtSetState.PairNumber
        };

        // if operator and value are present, read them in advance:
        //      parent
        //   +----+----+
        //   |    |    |
        // name [op  value]
        //   0    1    2
        if (id.Parent.ChildCount > 1)
        {
            ITerminalNode op = (ITerminalNode)id.Parent.GetChild(1);
            ITerminalNode val = (ITerminalNode)id.Parent.GetChild(2);
            if (op.Symbol.StartIndex > -1) pair.Operator = op.Symbol.Type;
            if (val.Symbol.StartIndex > -1) pair.Value = val.GetText();
        }

        return pair;
    }

    private void AppendPairComment(QuerySetPair pair, bool lf,
        StringBuilder sb)
    {
        sb.Append("-- ").Append(pair.Id).Append(": ")
          .Append(pair.IsStructure ? "$" : "").Append(pair.Name);
        if (pair.Operator > 0)
        {
            sb.Append(' ')
              .Append(_vocabulary.GetSymbolicName(pair.Operator))
              .Append(" \"")
              .Append(pair.Value)
              .Append('"');
        }
        if (lf) sb.Append('\n');
    }

    private string BuildNumericPairSql(string name, string op, string value,
        string? tableName)
    {
        string escName = tableName != null? EKP(tableName, name) : EK(name);
        return _sqlHelper.BuildTextAsNumber(escName) + " " + op + " " + value;
    }

    private string ApplyLiteralFilters(string text)
    {
        if (LiteralFilters.Count == 0) return text;
        StringBuilder sb = new(text);
        foreach (ILiteralFilter filter in LiteralFilters)
        {
            filter.Apply(sb);
            if (sb.Length == 0) break;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Builds the SQL code corresponding to the specified name/value pair
    /// (name op value).
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="op">The operator.</param>
    /// <param name="value">The value.</param>
    /// <param name="node">The node corresponding to the pair's name.</param>
    /// <param name="namePrefix">The optional name prefix.</param>
    /// <returns>The SQL code.</returns>
    /// <exception cref="ArgumentNullException">name or value</exception>
    /// <exception cref="PythiaQueryException"></exception>
    public string BuildPairSql(string name, int op, string value,
        ITerminalNode node, string? namePrefix = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (value == null)
        {
            throw new PythiaQueryException(LocalizedStrings.Format(
               Properties.Resources.NoPairValue,
               node.Symbol.Line,
               node.Symbol.Column,
               _vocabulary.GetSymbolicName(node.Symbol.Type)))
            {
                Line = node.Symbol.Line,
                Column = node.Symbol.Column,
                Index = node.Symbol.StartIndex,
                Length = node.Symbol.StopIndex - node.Symbol.StartIndex
            };
        }

        StringBuilder sb = new();
        string fullName;

        if (namePrefix != null)
            fullName = $"{namePrefix}.{name}";
        else
            fullName = name;

        switch (op)
        {
            // literals
            case pythiaLexer.EQ:
                // LOWER(name)=LOWER('value')
                sb.Append(LW(fullName))
                  .Append('=')
                  .Append(LW(SQE(ApplyLiteralFilters(value), false, true)));
                break;

            case pythiaLexer.NEQ:
                // LOWER(name)<>LOWER('value')
                sb.Append(LW(fullName))
                  .Append("<>")
                  .Append(LW(SQE(value, false, true)));
                break;

            case pythiaLexer.CONTAINS:
                // LOWER(name) LIKE ('%' || LOWER('value') || '%')
                sb.Append(LW(fullName))
                  .Append(" LIKE ('%' || ")
                  .Append(LW(SQE(ApplyLiteralFilters(value), false, true)))
                  .Append(" || '%')");
                break;

            case pythiaLexer.STARTSWITH:
                // LOWER(name) LIKE (LOWER('value') || '%')
                sb.Append(LW(fullName))
                  .Append(" LIKE (")
                  .Append(LW(SQE(ApplyLiteralFilters(value), false, true)))
                  .Append("|| '%')");
                break;

            case pythiaLexer.ENDSWITH:
                // LOWER(name) LIKE ('%' || LOWER('value'))
                sb.Append(LW(fullName))
                  .Append(" LIKE ('%' || ")
                  .Append(LW(SQE(ApplyLiteralFilters(value), false, true)))
                  .Append(')');
                break;

            // special
            case pythiaLexer.WILDCARDS:
                // if value has no wildcards, fallback to equals
                if (value.IndexOfAny(_wildcards) == -1) goto case pythiaLexer.EQ;

                // translate wildcards: * => %, ? => _
                string wild = value.Replace('*', '%').Replace('?', '_');

                // LOWER(name) LIKE LOWER('value') (encoded except for wildcards)
                sb.Append(LW(fullName))
                  .Append(" LIKE ")
                  .Append(LW(SQE(wild, true, true)));
                break;

            case pythiaLexer.REGEXP:
                sb.Append(_sqlHelper.BuildRegexMatch(fullName, value));
                break;

            case pythiaLexer.SIMILAR:
                sb.Append(_sqlHelper.BuildFuzzyMatch(fullName, value));
                break;

            // numeric
            case pythiaLexer.EQN:
                sb.Append(BuildNumericPairSql(name, "=", value, namePrefix));
                break;
            case pythiaLexer.NEQN:
                sb.Append(BuildNumericPairSql(name, "<>", value, namePrefix));
                break;
            case pythiaLexer.LT:
                sb.Append(BuildNumericPairSql(name, "<", value, namePrefix));
                break;
            case pythiaLexer.LTEQ:
                sb.Append(BuildNumericPairSql(name, "<=", value, namePrefix));
                break;
            case pythiaLexer.GT:
                sb.Append(BuildNumericPairSql(name, ">", value, namePrefix));
                break;
            case pythiaLexer.GTEQ:
                sb.Append(BuildNumericPairSql(name, ">=", value, namePrefix));
                break;
        }

        return sb.ToString();
    }
    #endregion

    #region Query
    /// <summary>
    /// Enter a parse tree produced by <see cref="M:pythiaParser.query" />:
    /// this resets the state of the listener and starts the r CTE.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void EnterQuery([NotNull] QueryContext context)
    {
        Reset();

        // open CTE result
        _cteResult.Append("-- result\n, r AS\n(\n");
    }

    /// <summary>
    /// Exit a parse tree produced by <see cref="M:pythiaParser.query" />;
    /// this closes the r CTE and composes the final SQL.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void ExitQuery([NotNull] QueryContext context)
    {
        // close CTE result
        _cteResult.Append(") -- r\n\n");

        // compose the queries
        string body = _cteList.ToString() + _cteResult.ToString();
        _dataSql = body + "--merger\n" + GetFinalSelect(false);
        _countSql = body + "--merger\n"  + GetFinalSelect(true);
    }
    #endregion

    #region Corpora
    /// <summary>
    /// Enter a parse tree produced by <see cref="M:pythiaParser.corSet" />.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void EnterCorSet([NotNull] CorSetContext context)
    {
        // the current set type is the corpora set
        _currentSetType = QuerySet.Corpora;
    }

    /// <summary>
    /// Exit a parse tree produced by <see cref="M:pythiaParser.corSet" />.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void ExitCorSet([NotNull] CorSetContext context)
    {
        // exit the corpora set type
        _currentSetType = QuerySet.Text;

        // nothing to do if no corpus ID was collected from the set
        if (_corporaIds.Count == 0) return;

        // else build the filtering clause template for corpora documents,
        // where the placeholder "{{p}}" will be either occurrence or
        // document_structure (according to whether we will be dealing
        // with a token or with a structure)
        _corpusSql = "INNER JOIN document_corpus\n"
            + "ON {{p}}.document_id=document_corpus.document_id\n"
            + "AND document_corpus.corpus_id IN("
            + string.Join(", ",
                from s in _corporaIds
                select SQE(s, false, true, true))
            + ")\n";
    }
    #endregion

    #region Documents
    /// <summary>
    /// Enter a parse tree produced by <see cref="M:pythiaParser.docSet" />.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void EnterDocSet([NotNull] DocSetContext context)
    {
        // the current set type is document
        _currentSetType = QuerySet.Document;
    }

    /// <summary>
    /// Exit a parse tree produced by <see cref="M:pythiaParser.docSet" />.
    /// <para>The default implementation does nothing.</para>
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void ExitDocSet([NotNull] DocSetContext context)
    {
        // exit the document set
        _currentSetType = QuerySet.Text;

        // if there are any clause(s), wrap them in (...)
        if (_docSetState.Sql.Length > 0)
        {
            _docSetState.Sql.Insert(0, "(\n");
            _docSetState.Sql.Append(")\n");
            _docSql = _docSetState.Sql.ToString();
        }
        // else there will be no clauses at all
        else
        {
            _docSql = null;
        }
    }

    private static bool IsNumericOp(int op)
    {
        return op == pythiaLexer.EQN || op == pythiaLexer.NEQN ||
               op == pythiaLexer.LT || op == pythiaLexer.LTEQ ||
               op == pythiaLexer.GT || op == pythiaLexer.GTEQ;
    }

    private static string? GetSqlForNumericOp(int op)
    {
        return op switch
        {
            pythiaLexer.EQN => "=",
            pythiaLexer.NEQN => "<>",
            pythiaLexer.LT => "<",
            pythiaLexer.LTEQ => "<=",
            pythiaLexer.GT => ">",
            pythiaLexer.GTEQ => ">=",
            _ => null
        };
    }

    /// <summary>
    /// Handles the specified pair of a document set.
    /// </summary>
    /// <param name="id">The node corresponding to the pair's head ID node.
    /// </param>
    private void HandleDocSetPair(ITerminalNode id)
    {
        // read the pair numbering it
        QuerySetPair pair = ReadQuerySetPair(id, true);
        // add the mapping between the pair node and its assigned name (pN)
        _nodeIds[id] = "p" + pair.Number;

        // if the pair refers to a document's privileged attribute,
        // build the corresponding SQL with reference to document
        if (PrivilegedDocAttrs.Contains(pair.Name!.ToLowerInvariant()))
        {
            // document.{name}{=}{value}
            AppendPairComment(pair, true, _docSetState.Sql);
            // ID is a special case as it's numeric
            if (pair.Name == "id")
            {
                if (pair.Operator != pythiaLexer.EQ && !IsNumericOp(pair.Operator))
                    throw new PythiaQueryException("Invalid operator for document ID");

                _docSetState.Sql.Append("document.id")
                    .Append(GetSqlForNumericOp(pair.Operator) ?? "=")
                    .Append(pair.Value);
            }
            else
            {
                _docSetState.Sql.Append(
                    BuildPairSql(pair.Name, pair.Operator, pair.Value ?? "", id,
                        "document"));
            }
        }

        // else, when the pair refers to a document's non-privileged attribute,
        // refer to document_attribute name or name and value
        else
        {
            // document_attribute.name={name}
            // AND document_attribute.value{=}{value} if specified
            AppendPairComment(pair, true, _docSetState.Sql);
            _docSetState.Sql
                .Append(LW("document_attribute.name"))
                .Append('=')
                .Append(LW(SQE(pair.Name, false, true)));
            if (pair.Operator > 0)
            {
                _docSetState.Sql.Append(" AND ")
                    .Append(BuildPairSql("value",
                        pair.Operator,
                        pair.Value ?? "",
                        id,
                        "document_attribute"));
            }
        }

        _docSetState.Sql.Append('\n');
    }

    /// <summary>
    /// Handles the specified document set terminal node, which is either an
    /// operator, a bracket, or a pair. Operators are just translated to SQL,
    /// while pairs are handled by <see cref="HandleDocSetPair(ITerminalNode)"/>.
    /// </summary>
    /// <param name="node">The node.</param>
    private void HandleDocSetTerminal(ITerminalNode node)
    {
        switch (node.Symbol.Type)
        {
            case pythiaLexer.AND:
                _docSetState.Sql.Append("AND\n");
                break;
            case pythiaLexer.OR:
                _docSetState.Sql.Append("OR\n");
                break;
            case pythiaLexer.ANDNOT:
                _docSetState.Sql.Append("AND NOT\n");
                break;
            case pythiaLexer.ORNOT:
                _docSetState.Sql.Append("OR NOT\n");
                break;
            case pythiaLexer.LPAREN:
                _docSetState.Sql.Append("(\n");
                break;
            case pythiaLexer.RPAREN:
                _docSetState.Sql.Append(")\n");
                break;
            case pythiaLexer.ID:
                HandleDocSetPair(node);
                break;
        }
    }
    #endregion

    #region Text Set        
    /// <summary>
    /// Enter a parse tree produced by <see cref="M:pythiaParser.locExpr" />.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void EnterTeLocation([NotNull] TeLocationContext context)
    {
        _locationState.Context = context;
        _locationState.Number++;
    }

    /// <summary>
    /// Exit a parse tree produced by <see cref="M:pythiaParser.locExpr" />.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void ExitTeLocation([NotNull] TeLocationContext context)
    {
        var lr = _locationState.TailDictionary[context];

        // if child of another one teLocation with locop as its operator,
        // push the SQL to be closed later
        RuleContext parent = context.Parent;

        if (parent.RuleIndex == context.RuleIndex &&
            parent.GetChild(1) is RuleContext sibling &&
            sibling.GetChild(0) is ITerminalNode locopChild &&
            LocationState.IsFn(locopChild.Symbol.Type))
        {
            _locationState.PushFnTail(lr.Item1, lr.Item2);
        }
        else
        {
            // append tail
            _cteResult.Append(") AS ").Append(lr.Item2).Append('\n')
                .Append("ON ").Append(lr.Item1).Append(".document_id=")
                .Append(lr.Item2).Append(".document_id AND\n");
            _locationState.AppendLocopFn(lr.Item1, lr.Item2, _cteResult);

            // pop and emit if any
            _locationState.PopFnTail(_cteResult);

            // reset non-global location state
            _locationState.Reset(false);
        }
    }

    /// <summary>
    /// Exit a parse tree produced by <see cref="pythiaParser.pair"/>.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void ExitPair([NotNull] PairContext context)
    {
        // only for type=text
        if (_currentSetType != QuerySet.Text) return;

        // add the pair SQL to the WITH CTEs list
        AppendPairToCteList();

        // add SELECT sN.* FROM sN to r CTE
        if (_currentCteDepth > 0)
        {
            for (int i = 0; i < _currentCteDepth * 2; i++)
                _cteResult.Append(' ');
        }
        _cteResult.Append("SELECT s").Append(_txtSetState.PairNumber)
            .Append(".* FROM s")
            .Append(_txtSetState.PairNumber)
            .Append('\n');

        // the pair SQL was just consumed: clear it
        _txtSetState.Sql.Clear();
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
        _locationState.LocopArgs.Clear();
        ITerminalNode op = (context.GetChild(0) as ITerminalNode)!;

        _locationState.SetLocop(op.Symbol.Type,
            LocationState.IsNotFn(op.Symbol.Type));
    }

    /// <summary>
    /// Exit a parse tree produced by <see cref="M:pythiaParser.locop" />.
    /// <para>When the locop args have been fully read and we're exiting
    /// the locop expression, validate and integrate locop arguments; these
    /// will then be consumed when appending the next modifier token filter.
    /// </para>
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void ExitLocop([NotNull] LocopContext context)
    {
        _locationState.ValidateArgs(context, _vocabulary);
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
        _locationState.LocopArgs[name] =
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
        _locationState.LocopArgs[name] = context.GetChild(2).GetText();
    }

    private bool AppendCorWhereDocSql(QuerySetPair pair, StringBuilder sb)
    {
        bool any = false;
        if (_corpusSql != null)
        {
            string sql = _corpusSql.Replace("{{p}}",
                pair.IsStructure ? "document_structure" : "occurrence");
            sb.Append("-- crp begin\n")
              .Append(sql)
              .Append("-- crp end\n");
        }

        sb.Append("WHERE\n");

        if (_docSql != null)
        {
            sb.Append("-- doc begin\n")
              .Append(_docSql)
              .Append("-- doc end\n");
            any = true;
        }
        return any;
    }

    private void AppendPairJoins(QuerySetPair pair)
    {
        // document JOINs if filtering by documents (a1/b1)
        if (_docSql != null)
        {
            string t = pair.IsStructure ? "structure" : "occurrence";
            _txtSetState.Sql.Append(
                // INNER JOIN document
                "INNER JOIN document ON ")
                .Append(t).Append(".document_id=document.id\n")
                .Append("INNER JOIN document_attribute ON ")
                .Append(t).Append(".document_id=document_attribute.document_id\n");
        }
    }

    private void AppendTokenPairFilter(QuerySetPair pair, ITerminalNode id,
        string? tokenTableAlias = null, string? indent = null)
    {
        // pair and language are in token, all the others in occurrence
        string t = tokenTableAlias ??
            (pair.Name == "value" || pair.Name == "language"
            ? "token"
            : "occurrence");

        // token, privileged
        if (PrivilegedTokAttrs.Contains(pair.Name!.ToLowerInvariant()))
        {
            // short pairs not allowed for privileged attribute
            if (pair.Operator == 0)
            {
                throw new PythiaQueryException(LocalizedStrings.Format(
                   Properties.Resources.InvalidShortPair,
                   id.Symbol.Line,
                   id.Symbol.Column,
                   pair))
                {
                    Line = id.Symbol.Line,
                    Column = id.Symbol.Column,
                    Index = id.Symbol.StartIndex,
                    Length = id.Symbol.StopIndex - id.Symbol.StartIndex
                };
            }
            _txtSetState.Sql
                .Append(indent ?? "")
                .Append(BuildPairSql(pair.Name, pair.Operator, pair.Value ?? "",
                    id, t))
                .Append('\n');
        }
        else
        {
            // token, non-privileged (ID or ID+OP+VAL)
            _txtSetState.Sql
                .Append(indent ?? "")
                .Append("EXISTS\n").Append("(\n")
                .Append("  SELECT * FROM ")
                .Append(EK("occurrence_attribute")).Append(" oa\n")
                .Append("  WHERE oa.occurrence_id=occurrence.id\n")
                .Append("  AND LOWER(oa.name)=")
                .Append(LW(SQE(pair.Name, false, true)));

            if (pair.Operator > 0)
            {
                _txtSetState.Sql
                    .Append('\n').Append("  AND ")
                    .Append(BuildPairSql("value", pair.Operator, pair.Value ?? "",
                        id, "oa"));
            }
            _txtSetState.Sql.Append('\n').Append(")\n");
        }
    }

    private void AppendStructurePairFilter(QuerySetPair pair, ITerminalNode id,
        string? structTableAlias = null, string? indent = null)
    {
        string t = structTableAlias ?? "structure";

        // structure, privileged (value)
        if (PrivilegedStrAttrs.Contains(pair.Name!.ToLowerInvariant()))
        {
            // short pairs not allowed for privileged attribute
            if (pair.Operator == 0)
            {
                throw new PythiaQueryException(LocalizedStrings.Format(
                   Properties.Resources.InvalidShortPair,
                   id.Symbol.Line,
                   id.Symbol.Column,
                   pair))
                {
                    Line = id.Symbol.Line,
                    Column = id.Symbol.Column,
                    Index = id.Symbol.StartIndex,
                    Length = id.Symbol.StopIndex - id.Symbol.StartIndex
                };
            }
            _txtSetState.Sql
                .Append(indent ?? "")
                .Append(BuildPairSql(pair.Name, pair.Operator, pair.Value ?? "",
                    id, t))
                .Append('\n');
        }
        else
        {
            // structure, non-privileged (ID or ID+OP+VAL)
            _txtSetState.Sql
                .Append("EXISTS\n").Append("(\n")
                .Append("  SELECT * FROM structure_attribute sa\n")
                .Append("  WHERE sa.structure_id=")
                .Append(t).Append(".id\n")
                .Append("  AND LOWER(sa.name)=")
                .Append(LW(SQE(pair.Name, false, true)));

            if (pair.Operator > 0)
            {
                _txtSetState.Sql
                    .Append('\n').Append("  AND ")
                    .Append(BuildPairSql("value", pair.Operator, pair.Value ?? "",
                        id, "sa"));
            }
            _txtSetState.Sql.Append('\n').Append(")\n");
        }
    }

    private void HandleTxtSetPair(QuerySetPair pair, ITerminalNode id)
    {
        if (_locationState.IsActive) _locationState.Number++;

        // comment
        AppendPairComment(pair, true, _txtSetState.Sql);

        if (pair.IsStructure)
        {
            _txtSetState.Sql
                .Append("SELECT DISTINCT\nstructure.document_id,\n")
                .Append("structure.start_position AS p1,\n")
                .Append("structure.end_position AS p2,\n")
                .Append("'s' AS entity_type,\n")
                .Append("structure.id AS entity_id\n")
                .Append("FROM structure\n");
        }
        else
        {
            // tokens draw their document positions from occurrence,
            // joined with token to allow filters access value/language
            _txtSetState.Sql
                .Append("SELECT DISTINCT\noccurrence.document_id,\n")
                .Append("occurrence.position AS p1,\n")
                .Append("occurrence.position AS p2,\n")
                .Append("'t' AS entity_type,\n")
                .Append("occurrence.id AS entity_id\n")
                .Append("FROM occurrence\n")
                .Append("INNER JOIN token ON occurrence.token_id=token.id\n");
        }

        AppendPairJoins(pair);

        // WHERE + corpus + document
        if (AppendCorWhereDocSql(pair, _txtSetState.Sql))
            _txtSetState.Sql.Append("AND\n");

        // pair filter
        if (pair.IsStructure)
            AppendStructurePairFilter(pair, id);
        else
            AppendTokenPairFilter(pair, id);
    }

    /// <summary>
    /// Handles the specified terminal node (pair or operator) in a text set.
    /// This adds the corresponding SQL operator for logical operators or
    /// brackets, and adds the SQL query for a pair node.
    /// </summary>
    /// <param name="node">The node.</param>
    private void HandleTxtSetTerminal(ITerminalNode node)
    {
        // https://stackoverflow.com/questions/47911252/how-to-get-the-current-rulecontext-class-when-visiting-a-terminalnode
        switch (node.Symbol.Type)
        {
            // non-locop operators or brackets
            case pythiaLexer.AND:
                _cteResult.Append("INTERSECT\n");
                break;
            case pythiaLexer.OR:
                _cteResult.Append("UNION\n");
                break;
            case pythiaLexer.ANDNOT:
                _cteResult.Append("EXCEPT\n");
                break;
            case pythiaLexer.LPAREN:
                if (node.Parent.RuleContext is not LocopContext)
                {
                    _cteResult.Append("(\n");
                    _currentCteDepth++;
                }
                break;
            case pythiaLexer.RPAREN:
                if (node.Parent.RuleContext is not LocopContext)
                {
                    _cteResult.Append(")\n");
                    _currentCteDepth--;
                }
                break;

            // locop operators
            case pythiaLexer.NEAR:
            case pythiaLexer.NOTNEAR:
            case pythiaLexer.BEFORE:
            case pythiaLexer.NOTBEFORE:
            case pythiaLexer.AFTER:
            case pythiaLexer.NOTAFTER:
            case pythiaLexer.INSIDE:
            case pythiaLexer.NOTINSIDE:
            case pythiaLexer.OVERLAPS:
            case pythiaLexer.NOTOVERLAPS:
            case pythiaLexer.LALIGN:
            case pythiaLexer.RALIGN:
                // append to r sth like (s1=left, s2=right):
                // INNER JOIN (
                int left = _txtSetState.PairNumber;
                int right = _txtSetState.PairNumber + 1;

                _locationState.AppendLocopFnComment(_cteResult);
                _cteResult.AppendLine("INNER JOIN (");

                // save closing tail to dictionary under key=txtExpr
                // of type teLocation; as we're on the locop child head,
                // we must get to locop and then to txtExpr
                IRuleNode txtExpr = node.Parent.Parent!;

                // AS s-left_s-right
                // ON s-left.document_id=s-left_s-right.document_id
                // AND
                // fn using s-left and s-left_s-right
                StringBuilder tail = new();
                string ln = $"s{left}";
                string rn = $"s{left}_s{right}";

                tail.Append(") AS ").Append(rn).Append('\n')
                    .Append("ON ").Append(ln).Append(".document_id=")
                    .Append(rn).Append(".document_id AND\n");

                // store in dictionary for later use; we can't append
                // the tail yet as we don't know its fn arguments
                _locationState.TailDictionary[txtExpr] = Tuple.Create(ln, rn);
                break;

            // pair heads
            case pythiaLexer.ID:
            case pythiaLexer.SID:
                // head of tpair/spair (text/structure pair)
                if (node.Parent.RuleContext is TpairContext ||
                    node.Parent.RuleContext is SpairContext)
                {
                    QuerySetPair pair = ReadQuerySetPair(node, false);
                    HandleTxtSetPair(pair, node);
                }
                break;
        }
    }
    #endregion

    /// <summary>
    /// Visit a terminal node. This will take different actions according
    /// to the node's context set: corpus, document, or text.
    /// </summary>
    /// <param name="node">Node.</param>
    public override void VisitTerminal([NotNull] ITerminalNode node)
    {
        switch (_currentSetType)
        {
            case QuerySet.Corpora:
                // corpus ID terminal, like "alpha" in @@alpha beta:
                // add the corpus ID to the list of corpora IDs
                if (node.Symbol.Type == pythiaLexer.ID)
                {
                    string text = node.GetText().ToLowerInvariant();
                    if (!string.IsNullOrEmpty(text)) _corporaIds.Add(text);
                }
                break;

            case QuerySet.Document:
                // document set: @docExpr
                HandleDocSetTerminal(node);
                break;

            case QuerySet.Text:
                // text set
                HandleTxtSetTerminal(node);
                break;
        }
    }
}

/// <summary>
/// Pythia query set type.
/// </summary>
internal enum QuerySet
{
    Text = 0,
    Corpora,
    Document
}
