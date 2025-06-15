using Antlr4.Runtime.Tree;
using Pythia.Core;
using Pythia.Core.Analysis;
using Pythia.Core.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using static Pythia.Core.Query.pythiaParser;

namespace Pythia.Sql;

/// <summary>
/// First pass SQL listener. This collects terminal pairs and their SQL;
/// builds SQL for corpus and documents; and assigns a key to each txtExpr
/// based on the ordinal(s) of the pairs it contains.
/// </summary>
/// <seealso cref="pythiaBaseListener" />
public class SqlPythiaPairListener(SqlPythiaListenerState state)
    : pythiaBaseListener
{
    private static readonly char[] _wildcards = ['*', '?'];

    private readonly SqlPythiaListenerState _state =
        state ?? throw new ArgumentNullException(nameof(state));

    // the SQL code for the document set
    private readonly ListenerSetState _docSetState = new();
    // the SQL code for the current pair in the text set
    private readonly ListenerSetState _txtSetState = new();

    private string? _prevStructName;
    // the corpora IDs collected from the corpus set if any
    private readonly HashSet<string> _corporaIds = [];
    // SQL code built for the corpus filter
    private string? _corpusSql;
    // SQL code built for the document filter
    private string? _docSql;

    private QuerySet _currentSetType = QuerySet.Text;

    /// <summary>
    /// Gets the optional literal filters to apply to literal values of
    /// query pairs.
    /// </summary>
    public IList<ILiteralFilter> LiteralFilters { get; } = [];

    private void Reset()
    {
        _state.Reset();
        _docSetState.Reset();
        _txtSetState.Reset();
        _prevStructName = null;
        _corporaIds.Clear();
        _corpusSql = null;
        _docSql = null;
        _currentSetType = QuerySet.Text;
    }

    #region Helpers
    private string EK(string keyword) =>
        _state.SqlHelper.EscapeKeyword(keyword);

    private string EKP(string keyword1, string keyword2, string? suffix = null) =>
        _state.SqlHelper.EscapeKeyword(keyword1) +
        "." +
        _state.SqlHelper.EscapeKeyword(keyword2) +
        (suffix ?? "");

    private static string LW(string text) => $"LOWER({text})";

    private string SQE(string text, bool hasWildcards = false,
        bool wrapInQuotes = false, bool unicode = true) =>
        _state.SqlHelper.SqlEncode(text, hasWildcards, wrapInQuotes, unicode);
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

        // else build the filtering clause template for corpora documents
        _corpusSql = "INNER JOIN document_corpus\n"
            + "ON span.document_id=document_corpus.document_id\n"
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

        // if the pair refers to a document's privileged attribute,
        // build the corresponding SQL with reference to document
        if (TextSpan.IsPrivilegedDocAttr(pair.Name!.ToLowerInvariant()))
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
                    BuildPairSql(pair.Name, pair.Operator, pair.Value ?? "",
                                 id, "document"));
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
                .Append(LW(SQE(pair.Name, true, true)));
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
            Number = doc ?
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

    private void AppendPairComment(QuerySetPair pair, bool lf, StringBuilder sb)
    {
        sb.Append("-- ").Append(pair.Id).Append(": ")
          .Append(pair.IsStructure ? "$" : "").Append(pair.Name);
        if (pair.Operator > 0)
        {
            sb.Append(' ')
              .Append(_state.Vocabulary.GetSymbolicName(pair.Operator))
              .Append(" \"")
              .Append(pair.Value)
              .Append('"');
        }
        if (lf) sb.Append('\n');
    }

    private void AppendPairJoins()
    {
        // document JOINs if filtering by documents (a1/b1)
        if (_docSql != null)
        {
            _txtSetState.Sql.Append("INNER JOIN document ON " +
                "span.document_id=document.id\n");

            if (_state.HasNonPrivilegedDocAttrs)
            {
                _txtSetState.Sql.Append("INNER JOIN document_attribute ON " +
                    "span.document_id=document_attribute.document_id\n");
            }
        }
    }

    private bool AppendCorWhereDocSql(StringBuilder sb)
    {
        bool any = false;
        if (_corpusSql != null)
        {
            sb.Append("-- crp begin\n")
              .Append(_corpusSql)
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

    private void ThrowInvalidOperatorForNumeric(ITerminalNode node, string name)
    {
        throw new PythiaQueryException(LocalizedStrings.Format(
            Properties.Resources.InvalidOperatorForNumericField,
            node.Symbol.Line,
            node.Symbol.Column,
            _state.Vocabulary.GetSymbolicName(node.Symbol.Type),
            name))
        {
            Line = node.Symbol.Line,
            Column = node.Symbol.Column,
            Index = node.Symbol.StartIndex,
            Length = node.Symbol.StopIndex - node.Symbol.StartIndex
        };
    }

    private string BuildNumericPairSql(string name, string op, string value)
    {
        string escName = EK(name);
        return _state.SqlHelper.BuildTextAsNumber(escName) + " " + op + " " + value;
    }

    /// <summary>
    /// Builds the SQL code corresponding to the specified name/value pair
    /// (name op value).
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="op">The operator.</param>
    /// <param name="value">The value.</param>
    /// <param name="node">The node corresponding to the pair's name.</param>
    /// <returns>The SQL code.</returns>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="numericValue">True if <paramref name="value"/> is numeric
    /// in the scheme.</param>
    /// <exception cref="ArgumentNullException">name or value</exception>
    /// <exception cref="PythiaQueryException"></exception>
    public string BuildPairSql(string name, int op, string value,
        ITerminalNode node, string? tableName = null, bool numericValue = false)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (value == null)
        {
            throw new PythiaQueryException(LocalizedStrings.Format(
               Properties.Resources.NoPairValue,
               node.Symbol.Line,
               node.Symbol.Column,
               _state.Vocabulary.GetSymbolicName(node.Symbol.Type)))
            {
                Line = node.Symbol.Line,
                Column = node.Symbol.Column,
                Index = node.Symbol.StartIndex,
                Length = node.Symbol.StopIndex - node.Symbol.StartIndex
            };
        }

        StringBuilder sb = new();
        string fullName = tableName == null ? name : EKP(tableName, name);

        switch (op)
        {
            // literals
            case pythiaLexer.EQ:
                if (numericValue)
                {
                    // name=value
                    sb.Append(fullName)
                      .Append('=')
                      .Append(value);
                }
                else
                {
                    // LOWER(name)=LOWER('value')
                    sb.Append(LW(fullName))
                      .Append('=')
                      .Append(LW(SQE(ApplyLiteralFilters(value), true, true)));
                }
                break;

            case pythiaLexer.NEQ:
                if (numericValue)
                {
                    // name<>value
                    sb.Append(fullName)
                      .Append("<>")
                      .Append(value);
                }
                else
                {
                    // LOWER(name)<>LOWER('value')
                    sb.Append(LW(fullName))
                      .Append("<>")
                      .Append(LW(SQE(value, true, true)));
                }
                break;

            case pythiaLexer.CONTAINS:
                if (numericValue) ThrowInvalidOperatorForNumeric(node, name);

                // LOWER(name) LIKE ('%' || LOWER('value') || '%')
                sb.Append(LW(fullName))
                  .Append(" LIKE ('%' || ")
                  .Append(LW(SQE(ApplyLiteralFilters(value), false, true)))
                  .Append(" || '%')");
                break;

            case pythiaLexer.STARTSWITH:
                if (numericValue) ThrowInvalidOperatorForNumeric(node, name);

                // LOWER(name) LIKE (LOWER('value') || '%')
                sb.Append(LW(fullName))
                  .Append(" LIKE (")
                  .Append(LW(SQE(ApplyLiteralFilters(value), false, true)))
                  .Append("|| '%')");
                break;

            case pythiaLexer.ENDSWITH:
                if (numericValue) ThrowInvalidOperatorForNumeric(node, name);

                // LOWER(name) LIKE ('%' || LOWER('value'))
                sb.Append(LW(fullName))
                  .Append(" LIKE ('%' || ")
                  .Append(LW(SQE(ApplyLiteralFilters(value), false, true)))
                  .Append(')');
                break;

            // special
            case pythiaLexer.WILDCARDS:
                if (numericValue) ThrowInvalidOperatorForNumeric(node, name);

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
                if (numericValue) ThrowInvalidOperatorForNumeric(node, name);

                sb.Append(_state.SqlHelper.BuildRegexMatch(fullName, value));
                break;

            case pythiaLexer.SIMILAR:
                if (numericValue) ThrowInvalidOperatorForNumeric(node, name);

                sb.Append(_state.SqlHelper.BuildFuzzyMatch(fullName, value));
                break;

            // numeric
            case pythiaLexer.EQN:
                if (numericValue)
                {
                    // name=value
                    sb.Append(fullName)
                      .Append('=')
                      .Append(value);
                }
                else
                {
                    sb.Append(BuildNumericPairSql(fullName, "=", value));
                }
                break;
            case pythiaLexer.NEQN:
                if (numericValue)
                {
                    // name<>value
                    sb.Append(fullName)
                      .Append("<>")
                      .Append(value);
                }
                else
                {
                    sb.Append(BuildNumericPairSql(fullName, "<>", value));
                }
                break;
            case pythiaLexer.LT:
                if (numericValue)
                {
                    // name<value
                    sb.Append(fullName)
                      .Append('<')
                      .Append(value);
                }
                else
                {
                    sb.Append(BuildNumericPairSql(fullName, "<", value));
                }
                break;
            case pythiaLexer.LTEQ:
                if (numericValue)
                {
                    // name<=value
                    sb.Append(fullName)
                      .Append("<=")
                      .Append(value);
                }
                else
                {
                    sb.Append(BuildNumericPairSql(fullName, "<=", value));
                }
                break;
            case pythiaLexer.GT:
                if (numericValue)
                {
                    // name>value
                    sb.Append(fullName)
                      .Append('>')
                      .Append(value);
                }
                else
                {
                    sb.Append(BuildNumericPairSql(fullName, ">", value));
                }
                break;
            case pythiaLexer.GTEQ:
                if (numericValue)
                {
                    // name>=value
                    sb.Append(fullName)
                      .Append(">=")
                      .Append(value);
                }
                else
                {
                    sb.Append(BuildNumericPairSql(fullName, ">=", value));
                }
                break;
        }

        return sb.ToString();
    }

    private void AppendTxtPairFilter(QuerySetPair pair, ITerminalNode id,
        string? indent = null)
    {
        // privileged
        if (TextSpan.IsPrivilegedSpanAttr(pair.Name!.ToLowerInvariant()))
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
            _txtSetState.Sql.Append(indent ?? "")
                .Append(BuildPairSql(
                    pair.Name, pair.Operator, pair.Value ?? "", id, "span",
                    TextSpan.IsNumericPrivilegedSpanAttr(pair.Name)))
                .Append('\n');
        }
        else
        {
            // non-privileged (ID or ID+OP+VAL)
            _txtSetState.Sql
                .Append(indent ?? "")
                .Append("EXISTS\n").Append("(\n")
                .Append("  SELECT * FROM ")
                .Append(EK("span_attribute")).Append(" sa\n")
                .Append("  WHERE sa.span_id=span.id\n")
                .Append("  AND LOWER(sa.name)=")
                .Append(LW(SQE(pair.Name, true, true)));

            if (pair.Operator > 0)
            {
                _txtSetState.Sql.Append('\n').Append("  AND ")
                    .Append(BuildPairSql(
                        "value",
                        pair.Operator,
                        pair.Value ?? "",
                        id));
            }
            _txtSetState.Sql.Append('\n').Append(")\n");
        }
    }

    /// <summary>
    /// Handles the received text set pair. This appends a comment to the text
    /// state SQL, followed by the SQL for the pair (SELECT...FROM sN WHERE...).
    /// </summary>
    /// <param name="pair">The pair as extracted from the pair branch.</param>
    /// <param name="node">The terminal node being the pair's head (=the attribute
    /// name).</param>
    /// <exception cref="PythiaQueryException">syntax error</exception>
    private void HandleTxtSetPair(QuerySetPair pair, ITerminalNode node)
    {
        // comment
        AppendPairComment(pair, true, _txtSetState.Sql);

        _txtSetState.Sql.Append("SELECT DISTINCT\n")
            .Append("  span.id, span.document_id, span.type,\n")
            .Append("  span.p1, span.p2, span.index, span.length,\n")
            .Append("  span.value\n")
            .Append("FROM span\n");

        AppendPairJoins();

        // WHERE + corpus + document
        if (AppendCorWhereDocSql(_txtSetState.Sql))
            _txtSetState.Sql.Append("AND\n");

        // type filter
        if (pair.IsStructure)
        {
            // when value is null, use name as this corresponds to shortcuts
            // like "[$l]" in "[value$="ter"] INSIDE(me=0) [$l]"
            _txtSetState.Sql.AppendFormat("span.type='{0}'\n",
                pair.Value ?? pair.Name);

            // set the previous structure pair name for _ attributes
            _prevStructName = pair.Name;
        }
        else
        {
            // when we refer to a structure's attribute, we prefix the attribute's
            // name with _, e.g. [$fp-lat] AND [_value="pro tempore"] where
            // _ is the prefix for the structure's attribute
            if (pair.Name!.StartsWith('_') && pair.Name.Length > 1)
            {
                if (_prevStructName == null)
                {
                    throw new PythiaQueryException(LocalizedStrings.Format(
                        Properties.Resources.NoPrevStructurePair,
                        node.Symbol.Line,
                        node.Symbol.Column,
                       _state.Vocabulary.GetSymbolicName(node.Symbol.Type)))
                    {
                        Line = node.Symbol.Line,
                        Column = node.Symbol.Column,
                        Index = node.Symbol.StartIndex,
                        Length = node.Symbol.StopIndex - node.Symbol.StartIndex
                    };
                }

                _txtSetState.Sql.AppendFormat("span.type='{0}' AND\n",
                    _prevStructName);
                pair.Name = pair.Name[1..];
            }
            else
            {
                _txtSetState.Sql.AppendFormat("span.type='{0}' AND\n",
                    TextSpan.TYPE_TOKEN);
            }
            AppendTxtPairFilter(pair, node);
        }
    }

    /// <summary>
    /// Handles the specified terminal node (pair or operator) in a text set.
    /// If the terminal node is a logical operator or a bracket, the
    /// corresponding SQL code (INTERSECT, UNION, EXCEPT, or bracket) is added
    /// to the CTE result.
    /// If instead the terminal node is a pair head (=attribute name), the pair
    /// is read (it might be a name-operator-value pair, or just a name pair),
    /// and then handled by <see cref="HandleTxtSetPair"/>, which appends a
    /// comment to the text state SQL, followed by the SQL for the pair
    /// (SELECT...FROM sN WHERE...).
    /// </summary>
    /// <param name="node">The node.</param>
    private void HandleTxtSetTerminal(ITerminalNode node)
    {
        // https://stackoverflow.com/questions/47911252/how-to-get-the-current-rulecontext-class-when-visiting-a-terminalnode

        switch (node.Symbol.Type)
        {
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

    /// <summary>
    /// Enter a parse tree produced by <see cref="M:pythiaParser.query" />:
    /// this resets the state of the listener.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void EnterQuery([NotNull] QueryContext context)
    {
        Reset();
    }

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

    /// <summary>
    /// Exit a parse tree produced by <see cref="pythiaParser.pair"/>. This
    /// node has 3 children: <c>[</c>, a tpair or spair, and <c>]</c>. Its
    /// parent is a txtExpr.
    /// </summary>
    /// <param name="context">The parse tree.</param>
    public override void ExitPair([NotNull] PairContext context)
    {
        // only for type=text
        if (_currentSetType != QuerySet.Text) return;

        // add the pair SQL to the WITH CTEs list
        _state.PairCteQueries[$"s{_txtSetState.PairNumber}"] =
            _txtSetState.Sql.ToString();

        // the pair SQL was just consumed: clear it
        _txtSetState.Sql.Clear();
    }
}

/// <summary>
/// Pythia query set type.
/// </summary>
public enum QuerySet
{
    /// <summary>The text set type.</summary>
    Text = 0,
    /// <summary>The corpora set type.</summary>
    Corpora,
    /// <summary>The document set type.</summary>
    Document
}
