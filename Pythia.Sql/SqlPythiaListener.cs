using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime;
using Pythia.Core.Query;
using System.Globalization;
using static pythiaParser;
using Pythia.Core.Analysis;

namespace Pythia.Sql
{
    /// <summary>
    /// A Pythia listener which builds SQL code.
    /// </summary>
    /// <seealso cref="pythiaBaseListener" />
    public sealed class SqlPythiaListener : pythiaBaseListener
    {
        // keys used for locop arguments in parsing query
        private const string ARG_OP = "op";
        private const string ARG_NOT = "not";
        private const string ARG_N = "n";
        private const string ARG_M = "m";
        private const string ARG_S = "s";
        private const string ARG_NS = "ns";
        private const string ARG_MS = "ms";
        private const string ARG_NE = "ne";
        private const string ARG_ME = "me";

        private readonly IVocabulary _vocabulary;
        private readonly ISqlHelper _sqlHelper;

        // CTE list
        private readonly StringBuilder _cteList;
        // CTE result
        private readonly StringBuilder _cteResult;

        // general constants
        internal static readonly HashSet<string> PrivilegedDocAttrs =
            new HashSet<string>(new[]
            {
                "author", "title", "date_value", "sort_key", "source", "profile_id"
            });
        internal static readonly HashSet<string> PrivilegedTokAttrs =
            new HashSet<string>(new[]
            {
                "value", "language", "position", "length"
            });
        internal static readonly HashSet<string> PrivilegedStrAttrs =
            new HashSet<string>(new[]
            {
                "name", "start_position", "end_position"
            });
        private static readonly char[] _wildcards = new[] { '*', '?' };

        // state
        private QuerySet _currentSetType;
        private readonly LocationState _locationState;
        private readonly HashSet<string> _corporaIds;
        private readonly ListenerSetState _docSetState;
        private readonly ListenerSetState _txtSetState;
        private readonly Dictionary<string, object> _locopArgs;
        private readonly Dictionary<IParseTree, string> _nodeIds;
        private bool _anyCte;
        private int _currentCteDepth;

        // the built corpus filter
        private string _corpusSql;
        // the built document filter
        private string _docSql;
        // the built full queries
        private string _dataSql;
        private string _countSql;

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
            _locationState = new LocationState();
            _locopArgs = new Dictionary<string, object>();
            _nodeIds = new Dictionary<IParseTree, string>();
            _corporaIds = new HashSet<string>();

            PageNumber = 1;
            PageSize = 20;
            LiteralFilters = new List<ILiteralFilter>();
        }

        /// <summary>
        /// Gets the SQL built by this listener.
        /// </summary>
        /// <param name="count">if set to <c>true</c>, get the total count
        /// query; else get the results page query.</param>
        /// <returns>SQL string</returns>
        public string GetSql(bool count) => count? _countSql : _dataSql;

        private void Reset()
        {
            _cteList.Clear();
            _cteResult.Clear();

            _currentSetType = QuerySet.Text;
            _corporaIds.Clear();
            _docSetState.Reset();
            _txtSetState.Reset();
            _locationState.Reset();
            _locopArgs.Clear();
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

        private string EKP(string keyword1, string keyword2, string suffix = null) =>
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
        private void AddIndent(int length, StringBuilder sb)
        {
            if (sb.Length == 0) return;

            string indent = new string(' ', length);
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
                else i--;
            }

            // first line
            if (sb[0] != '\n') sb.Insert(0, indent);
        }
        #endregion

        #region CTE List        
        /// <summary>
        /// Appends the SQL pair query in the current set state to the list
        /// of CTEs.
        /// </summary>
        private void AppendPairCteToList()
        {
            // WITH sN AS (...), sN AS (...), ...
            string sn = "s" + _txtSetState.PairNumber;

            // prefix
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
        private string GetFinalSelect(bool count)
        {
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
                    //+ "WHERE EXISTS\n"
                    //+ "(\n"
                    //+ "  SELECT * FROM r\n"
                    //+ "  WHERE occurrence.document_id=r.document_id\n"
                    //+ "  AND occurrence.position=r.position\n"
                    //+ ")\n"
                    + "INNER JOIN r ON\n"
                    + "occurrence.document_id=r.document_id\n"
                    + "AND occurrence.position=r.position\n"
                    + ") c\n";
            }

            int skipCount = (PageNumber - 1) * PageSize;
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
                //+ "WHERE EXISTS\n"
                //+ "(\n"
                //+ "  SELECT * FROM r\n"
                //+ "  WHERE occurrence.document_id=r.document_id\n"
                //+ "  AND occurrence.position=r.position\n"
                //+ ")\n"
                + "INNER JOIN r ON\n"
                + "occurrence.document_id=r.document_id\n"
                + "AND occurrence.position=r.position\n"
                + "ORDER BY document.sort_key,"
                + "occurrence.position\n" +
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
            QuerySetPair pair = new QuerySetPair
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
            string tableName)
        {
            string escName = tableName != null? EKP(tableName, name) : EK(name);
            return _sqlHelper.BuildTextAsNumber(escName) + " " + op + " " + value;
        }

        private string ApplyLiteralFilters(string text)
        {
            if (LiteralFilters.Count == 0) return text;
            StringBuilder sb = new StringBuilder(text);
            foreach (ILiteralFilter filter in LiteralFilters)
            {
                filter.Apply(sb);
                if (sb.Length == 0) break;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Builds the SQL code corresponding to the specified name/value pair.
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
            ITerminalNode node, string namePrefix = null)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
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

            StringBuilder sb = new StringBuilder();
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
                      .Append(")");
                    break;

                // special
                case pythiaLexer.WILDCARDS:
                    // if value has no wildcards, fallback to equals
                    if (value.IndexOfAny(_wildcards) == -1)
                        goto case pythiaLexer.EQ;

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
        /// Enter a parse tree produced by <see cref="M:pythiaParser.query" />.
        /// </summary>
        /// <param name="context">The parse tree.</param>
        public override void EnterQuery([NotNull] QueryContext context)
        {
            Reset();

            // open CTE result
            _cteResult.Append("-- result\n, r AS\n(\n");
        }

        /// <summary>
        /// Exit a parse tree produced by <see cref="M:pythiaParser.query" />.
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
            else _docSql = null;
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
            if (PrivilegedDocAttrs.Contains(pair.Name.ToLowerInvariant()))
            {
                // document.{name}{=}{value}
                AppendPairComment(pair, true, _docSetState.Sql);
                _docSetState.Sql.Append(
                    BuildPairSql(pair.Name, pair.Operator, pair.Value, id,
                        "document"));
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
                        .Append(BuildPairSql("value", pair.Operator, pair.Value,
                            id, "document_attribute"));
                }
            }

            _docSetState.Sql.Append('\n');
        }

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
        public override void EnterLocExpr([NotNull] LocExprContext context)
        {
            _locationState.Context = context;
        }

        /// <summary>
        /// Exit a parse tree produced by <see cref="M:pythiaParser.locExpr" />.
        /// </summary>
        /// <param name="context">The parse tree.</param>
        public override void ExitLocExpr([NotNull] LocExprContext context)
        {
            _locopArgs.Clear();
            _locationState.Reset();
        }

        /// <summary>
        /// Exit a parse tree produced by <see cref="pythiaParser.pair"/>.
        /// </summary>
        /// <param name="context">The parse tree.</param>
        public override void ExitPair([NotNull] PairContext context)
        {
            if (_currentSetType != QuerySet.Text) return;

            // add the pair SQL to the WITH CTEs list
            AppendPairCteToList();

            // if this is a modifier pair in a location, use it as a filter
            // for the previous sN CTE, e.g. for sic-mater-sic:
            // SELECT * FROM s1 -- the first token in locExpr: "sic"
            // WHERE
            // -- the 1st modifier token in locExpr: "mater"
            // EXISTS
            // (
            //   SELECT * FROM s2
            //   WHERE s1.document_id = s2.document_id AND fn(...s1/s2...)
            // )
            // -- the 3rd modifier token in locExpr: "sic"
            // AND EXISTS
            // (
            //   SELECT * FROM s3
            //   WHERE s2.document_id = s3.document_id AND fn(...s2/s3...)
            // )
            // etc.
            if (_locationState.IsActive && _locationState.CurrentPairNumber > 1)
            {
                string left = "s" + (_locationState.CurrentPairNumber - 1);
                string right = "s" + _locationState.CurrentPairNumber;

                _cteResult.Append("-- ").Append(right)
                          .Append(" -> ").Append(left).Append('\n')
                          .Append(_locationState.CurrentPairNumber == 2 ?
                            "WHERE" : "AND");

                if (_locopArgs.ContainsKey(ARG_NOT) && (bool)_locopArgs[ARG_NOT])
                    _cteResult.Append(" NOT");

                _cteResult.Append(" EXISTS\n(\n");
                // subquery
                StringBuilder sql = new StringBuilder();
                sql.Append("SELECT * FROM ").Append(right).Append('\n')
                   .Append("WHERE ")
                   .Append(left).Append(".document_id=")
                   .Append(right).Append(".document_id\n")
                   .Append("AND ");

                Tuple<char, char> types = _locationState.GetCurrentPairTypes();
                if (types.Item2 == 's' || types.Item2 == 'S')
                    AppendStructureCollocationFilter(left, right, sql);
                else
                    AppendTokenCollocationFilter(left, right, sql);

                AddIndent(2, sql);
                _cteResult.Append(sql);
                _cteResult.Append(") -- ").Append(right).Append('\n');
            }
            else
            {
                // SELECT * FROM sN
                if (_currentCteDepth > 0)
                {
                    for (int i = 0; i < _currentCteDepth * 2; i++)
                        _cteResult.Append(' ');
                }
                _cteResult.Append("SELECT * FROM s")
                    .Append(_txtSetState.PairNumber)
                    .Append('\n');
            }

            // the pair SQL was just consumed: clear it
            _txtSetState.Sql.Clear();
        }

        private static bool IsNotFn(int symbolType)
        {
            return symbolType == pythiaLexer.NOTAFTER ||
                symbolType == pythiaLexer.NOTBEFORE ||
                symbolType == pythiaLexer.NOTINSIDE ||
                symbolType == pythiaLexer.NOTLALIGN ||
                symbolType == pythiaLexer.NOTNEAR ||
                symbolType == pythiaLexer.NOTOVERLAPS ||
                symbolType == pythiaLexer.NOTRALIGN;
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
            _locopArgs.Clear();
            ITerminalNode op = context.GetChild(0) as ITerminalNode;

            // NOT
            if (IsNotFn(op.Symbol.Type)) _locopArgs[ARG_NOT] = true;

            // fn
            _locopArgs[ARG_OP] = op.Symbol.Type;
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
            switch (_locopArgs[ARG_OP])
            {
                case pythiaLexer.NEAR:
                case pythiaLexer.NOTNEAR:
                case pythiaLexer.BEFORE:
                case pythiaLexer.NOTBEFORE:
                case pythiaLexer.AFTER:
                case pythiaLexer.NOTAFTER:
                case pythiaLexer.OVERLAPS:
                case pythiaLexer.NOTOVERLAPS:
                case pythiaLexer.LALIGN:
                case pythiaLexer.NOTLALIGN:
                case pythiaLexer.RALIGN:
                case pythiaLexer.NOTRALIGN:
                    ValidateNearOperatorArgs(context);
                    break;
                case pythiaLexer.INSIDE:
                case pythiaLexer.NOTINSIDE:
                    ValidateInsideOperatorArgs(context);
                    break;
            }
        }

        /// <summary>
        /// Enter a parse tree produced by <see cref="M:pythiaParser.locnArg" />.
        /// </summary>
        /// <param name="context">The parse tree.</param>
        public override void EnterLocnArg([NotNull] LocnArgContext context)
        {
            // type n=1
            string name = context.GetChild(0).GetText();
            string value = context.GetChild(2).GetText();
            _locopArgs[name] = int.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Enter a parse tree produced by <see cref="M:pythiaParser.locsArg" />.
        /// </summary>
        /// <param name="context">The parse tree.</param>
        public override void EnterLocsArg([NotNull] LocsArgContext context)
        {
            // type s=l
            string name = context.GetChild(0).GetText();
            _locopArgs[name] = context.GetChild(2).GetText();
        }

        /// <summary>
        /// Finds the first terminal node descending from <paramref name="context"/>
        /// and matching <paramref name="filter"/>.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>Terminal node or null if not found.</returns>
        private static ITerminalNode FindTerminalFrom(IRuleNode context,
            Func<ITerminalNode, bool> filter)
        {
            for (int i = 0; i < context.ChildCount; i++)
            {
                IParseTree child = context.GetChild(i);
                ITerminalNode leaf = child as ITerminalNode;
                if (leaf == null)
                {
                    IRuleNode rule = child as IRuleNode;
                    if (rule != null) return FindTerminalFrom(rule, filter);
                }
                else
                {
                    if (filter(leaf)) return leaf;
                }
            }
            return null;
        }

        /// <summary>
        /// Validates the operator arguments provided in the query when using
        /// location functions with N, M, S, like near-within.
        /// </summary>
        /// <param name="context">The locop context node.</param>
        /// <exception cref="PythiaQueryException">error</exception>
        private void ValidateNearOperatorArgs(IRuleNode context)
        {
            // supply defaults if missing N, M
            if (!_locopArgs.ContainsKey(ARG_N)) _locopArgs[ARG_N] = 0;
            if (!_locopArgs.ContainsKey(ARG_M)) _locopArgs[ARG_M] = int.MaxValue;

            // S is not allowed with NOT (S is the context shared between
            // two items, while NOT negates the second item)
            if (_locopArgs.ContainsKey(ARG_S))
            {
                bool not = _locopArgs.ContainsKey(ARG_NOT) &&
                    (bool)_locopArgs[ARG_NOT];

                if (not)
                {
                    // the S node is the penultimate child (the last being RPAREN)
                    ITerminalNode node =
                        (ITerminalNode)context.GetChild(context.ChildCount - 2);

                    throw new PythiaQueryException(LocalizedStrings.Format(
                       Properties.Resources.ArgopSWithNot,
                       node.Symbol.Line,
                       node.Symbol.Column,
                       _vocabulary.GetSymbolicName(node.Symbol.Type),
                       context.GetText()))
                    {
                        Line = node.Symbol.Line,
                        Column = node.Symbol.Column,
                        Index = node.Symbol.StartIndex,
                        Length = node.Symbol.StopIndex - node.Symbol.StartIndex
                    };
                }
            }

            // n cannot be > m
            int n = (int)_locopArgs[ARG_N];
            int m = (int)_locopArgs[ARG_M];
            if (n > m)
            {
                // the reference node for the error is the n/m child
                ITerminalNode node =
                    FindTerminalFrom(context, leaf => leaf.GetText() == "n") ??
                    FindTerminalFrom(context, leaf => leaf.GetText() == "m");

                throw new PythiaQueryException(LocalizedStrings.Format(
                   Properties.Resources.ArgopNGreaterThanM,
                   node.Symbol.Line,
                   node.Symbol.Column,
                   _vocabulary.GetSymbolicName(node.Symbol.Type),
                   n, m))
                {
                    Line = node.Symbol.Line,
                    Column = node.Symbol.Column,
                    Index = node.Symbol.StartIndex,
                    Length = node.Symbol.StopIndex - node.Symbol.StartIndex
                };
            }
        }

        /// <summary>
        /// Validates the operator arguments provided in the query when using
        /// location functions with NS, MS, NE, ME, S, like inside-within.
        /// </summary>
        /// <param name="context">The locop context node.</param>
        /// <exception cref="PythiaQueryException">error</exception>
        private void ValidateInsideOperatorArgs(IRuleNode context)
        {
            // supply defaults if missing NS, MS, NE, ME
            if (!_locopArgs.ContainsKey(ARG_NS)) _locopArgs[ARG_NS] = 0;
            if (!_locopArgs.ContainsKey(ARG_MS)) _locopArgs[ARG_MS] = int.MaxValue;
            if (!_locopArgs.ContainsKey(ARG_NE)) _locopArgs[ARG_NE] = 0;
            if (!_locopArgs.ContainsKey(ARG_ME)) _locopArgs[ARG_ME] = int.MaxValue;

            // ns cannot be greater than ms
            int ns = (int)_locopArgs[ARG_NS];
            int ms = (int)_locopArgs[ARG_MS];
            if (ns > ms)
            {
                // the reference node for the error is the ns/ms child
                ITerminalNode node =
                    FindTerminalFrom(context, leaf => leaf.GetText() == "ns") ??
                    FindTerminalFrom(context, leaf => leaf.GetText() == "ms");

                throw new PythiaQueryException(LocalizedStrings.Format(
                   Properties.Resources.ArgopNSGreaterThanMS,
                   node.Symbol.Line,
                   node.Symbol.Column,
                   _vocabulary.GetSymbolicName(node.Symbol.Type),
                   ns, ms))
                {
                    Line = node.Symbol.Line,
                    Column = node.Symbol.Column,
                    Index = node.Symbol.StartIndex,
                    Length = node.Symbol.StopIndex - node.Symbol.StartIndex
                };
            }

            // ne cannot be greater than me
            int ne = (int)_locopArgs[ARG_NE];
            int me = (int)_locopArgs[ARG_ME];
            if (ne > me)
            {
                // the reference node for the error is the ns/ms child
                ITerminalNode node =
                    FindTerminalFrom(context, leaf => leaf.GetText() == "ne") ??
                    FindTerminalFrom(context, leaf => leaf.GetText() == "me");

                throw new PythiaQueryException(LocalizedStrings.Format(
                   Properties.Resources.ArgopNEGreaterThanME,
                   node.Symbol.Line,
                   node.Symbol.Column,
                   _vocabulary.GetSymbolicName(node.Symbol.Type),
                   ne, me))
                {
                    Line = node.Symbol.Line,
                    Column = node.Symbol.Column,
                    Index = node.Symbol.StartIndex,
                    Length = node.Symbol.StopIndex - node.Symbol.StartIndex
                };
            }

            // S is not allowed with NOT (S is the context shared between
            // two items, while NOT negates the second item)
            if (_locopArgs.ContainsKey(ARG_S))
            {
                bool not = _locopArgs.ContainsKey(ARG_NOT) &&
                    (bool)_locopArgs[ARG_NOT];

                if (not)
                {
                    // the S node is the penultimate child (the last being RPAREN)
                    ITerminalNode node =
                        (ITerminalNode)context.GetChild(context.ChildCount - 2);

                    throw new PythiaQueryException(LocalizedStrings.Format(
                       Properties.Resources.ArgopSWithNot,
                       node.Symbol.Line,
                       node.Symbol.Column,
                       _vocabulary.GetSymbolicName(node.Symbol.Type),
                       context.GetText()))
                    {
                        Line = node.Symbol.Line,
                        Column = node.Symbol.Column,
                        Index = node.Symbol.StartIndex,
                        Length = node.Symbol.StopIndex - node.Symbol.StartIndex
                    };
                }
            }
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

        private int GetMinArgValue(string name)
        {
            return _locopArgs.ContainsKey(name) ?
                (int)_locopArgs[name] :
                0;
        }

        private int GetMaxArgValue(string name)
        {
            return _locopArgs.ContainsKey(name) ?
                (int)_locopArgs[name] :
                int.MaxValue;
        }

        /// <summary>
        /// Appends the SQL code corresponding to the S-argument of a locop
        /// operator relative to a right (modifier) token node.
        /// </summary>
        /// <param name="left">The left node name (sN).</param>
        /// <param name="right">The right node name (sN).</param>
        /// <param name="sql">The SQL.</param>
        private void AppendSArgWithToken(string left, string right,
            StringBuilder sql)
        {
            if (!_locopArgs.ContainsKey(ARG_S)) return;

            const string ssp = "s.start_position";
            const string sep = "s.end_position";

            // determine the type of the left (target) node
            Tuple<char, char> locTypes = _locationState.GetCurrentPairTypes();
            bool t1 = locTypes.Item1 == 't' || locTypes.Item1 == 'T';

            sql.Append("-- s=").Append(_locopArgs[ARG_S]).Append('\n')
               .Append("AND EXISTS\n").Append("(\n")
               .Append("  SELECT id FROM structure s\n")
               .Append("  WHERE LOWER(s.name)=")
               .Append(LW(SQE((string)_locopArgs[ARG_S], false, true))).Append('\n')
               .Append("  AND s.document_id=").Append(right).Append(".document_id\n");

            // TT
            if (t1)
            {
                sql.Append("  -- t[t] inside s\n")
                   .Append("  AND ").Append(right).Append(".position >= ")
                   .Append(ssp).Append('\n')
                   .Append("  AND ").Append(right).Append(".position <= ")
                   .Append(sep).Append('\n')
                   .Append("  -- [t]t inside s\n")
                   .Append("  AND ")
                   .Append(left).Append(".position >= ").Append(ssp).Append('\n')
                   .Append("  AND ")
                   .Append(left).Append(".position <= ").Append(sep).Append('\n');
            }
            // ST
            else
            {
                sql.Append("  -- s[t] inside s\n")
                   .Append("  AND ").Append(right).Append(".position >= ")
                   .Append(ssp).Append('\n')
                   .Append("  AND ").Append(right).Append(".position <= ")
                   .Append(sep).Append('\n')
                   .Append("  -- [s]t inside s\n")
                   .Append("  AND ")
                   .Append(left).Append(".start_position >= ")
                   .Append(ssp).Append('\n')
                   .Append("  AND ")
                   .Append(left).Append(".end_position <= ").Append(sep).Append('\n');
            }

            sql.Append(")\n");
        }

        private void AppendSArgWithStructure(string left, string right,
            StringBuilder sql)
        {
            if (!_locopArgs.ContainsKey(ARG_S)) return;

            const string ssp = "s.start_position";
            const string sep = "s.end_position";

            // determine the type of the left (target) node
            Tuple<char, char> locTypes = _locationState.GetCurrentPairTypes();
            bool t1 = locTypes.Item1 == 't' || locTypes.Item1 == 'T';

            sql.Append("-- s=").Append(_locopArgs[ARG_S]).Append('\n')
                .Append("AND EXISTS\n").Append("(\n")
                .Append("  SELECT id FROM structure s\n")
                .Append("  WHERE LOWER(s.name)=")
                .Append(LW(SQE((string)_locopArgs[ARG_S], false, true))).Append('\n')
                .Append("  AND s.document_id=").Append(right).Append(".document_id\n");

            // TS
            if (t1)
            {
                sql.Append("  -- [t]s inside s\n")
                   .Append("  AND ").Append(left).Append(".position >= ")
                   .Append(ssp).Append('\n')
                   .Append("  AND ").Append(left).Append(".position <= ")
                   .Append(sep).Append('\n')
                   .Append("  -- t[s] inside s\n")
                   .Append("  AND ")
                   .Append(right).Append(".start_position >= ").Append(ssp).Append('\n')
                   .Append("  AND ")
                   .Append(right).Append(".end_position <= ").Append(sep).Append('\n');
            }
            // SS
            else
            {
                sql.Append("  -- [s]s inside s\n")
                   .Append("  AND ")
                   .Append(left).Append(".start_position >= ").Append(ssp).Append('\n')
                   .Append("  AND ")
                   .Append(left).Append(".end_position <= ").Append(sep).Append('\n')
                   .Append("  -- s[s] inside s\n")
                   .Append("  AND ").Append(right).Append(".start_position >= ")
                   .Append(ssp).Append('\n')
                   .Append("  AND {right}.end_position <= ").Append(sep).Append('\n');
            }

            sql.Append(")\n");
        }

        private void AppendPairJoins(QuerySetPair pair)
        {
            // document JOINs if filtering by documents (a1/b1)
            if (_docSql != null)
            {
                string t = pair.IsStructure ? "document_structure" : "occurrence";
                _txtSetState.Sql.Append(
                    // INNER JOIN document
                    "INNER JOIN document ON ")
                    .Append(t).Append(".document_id=document.id\n")
                    .Append("INNER JOIN document_attribute ON ")
                    .Append(t).Append(".document_id=document_attribute.document_id\n");
            }

            // if pair is structure, add structure JOINs (b1)
            if (pair.IsStructure)
            {
                _txtSetState.Sql.Append("INNER JOIN structure ON " +
                    "document_structure.structure_id=structure.id\n");
            }
            else
            {
                // else add structure JOINs if any loc pair targets a structure
                if (_locationState.IsActive &&
                    _locationState.PairTypes.Any(c => c == 's' || c == 'S'))
                {
                    _txtSetState.Sql.Append(
                        // INNER JOIN document_structure
                        "INNER JOIN document_structure ON " +
                        "occurrence.document_id=document_structure.document_id\n")
                        .Append("AND occurrence.position=document_structure.position\n")
                        .Append("INNER JOIN structure ON " +
                            "document_structure.structure_id=structure.id\n");
                }
            }
        }

        private void AppendTokenPairFilter(QuerySetPair pair, ITerminalNode id,
            string tokenTableAlias = null, string indent = null)
        {
            // pair and language are in token, all the others in occurrence
            string t = tokenTableAlias ??
                (pair.Name == "value" || pair.Name == "language"
                ? "token"
                : "occurrence");

            // token, privileged
            if (PrivilegedTokAttrs.Contains(pair.Name.ToLowerInvariant()))
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
                    .Append(BuildPairSql(pair.Name, pair.Operator, pair.Value,
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
                        .Append(BuildPairSql("value", pair.Operator, pair.Value,
                            id, "oa"));
                }
                _txtSetState.Sql.Append('\n').Append(")\n");
            }
        }

        private void AppendStructurePairFilter(QuerySetPair pair, ITerminalNode id,
            string structTableAlias = null, string indent = null)
        {
            string t = structTableAlias ?? "structure";

            // structure, privileged (value)
            if (PrivilegedStrAttrs.Contains(pair.Name.ToLowerInvariant()))
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
                    .Append(BuildPairSql(pair.Name, pair.Operator, pair.Value,
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
                        .Append(BuildPairSql("value", pair.Operator, pair.Value,
                            id, "sa"));
                }
                _txtSetState.Sql.Append('\n').Append(")\n");
            }
        }

        private void AppendFinalFnArgs(bool inside, StringBuilder sql)
        {
            // NS, MS, NE, ME for INSIDE
            if (inside)
            {
                sql.Append(GetMinArgValue(ARG_NS)).Append(", ")
                   .Append(GetMaxArgValue(ARG_MS)).Append(", ")
                   .Append(GetMinArgValue(ARG_NE)).Append(", ")
                   .Append(GetMaxArgValue(ARG_ME));
            }
            // N,M for all the others
            else
            {
                sql.Append(GetMinArgValue(ARG_N)).Append(", ")
                   .Append(GetMaxArgValue(ARG_M));
            }
        }

        private void AppendTokenCollocationFilter(string left, string right,
            StringBuilder sql)
        {
            // position filter
            int op = (int)_locopArgs[ARG_OP];
            bool inside = op == pythiaLexer.INSIDE || op == pythiaLexer.NOTINSIDE;
            string fn = _sqlHelper.GetLexerFnName((int)_locopArgs[ARG_OP]);

            Tuple<char, char> locTypes = _locationState.GetCurrentPairTypes();

            // TT
            if (locTypes.Item1 == 'T' || locTypes.Item1 == 't')
            {
                sql.Append("\n-- tt\n").Append(fn).Append('(');

                switch (op)
                {
                    case pythiaLexer.LALIGN:
                    case pythiaLexer.RALIGN:
                        sql.Append(left).Append(".position, ").Append(right)
                           .Append(".position, ");
                        break;
                    default:
                        sql.Append(left).Append(".position, ")
                           .Append(left).Append(".position, ").Append(right)
                           .Append(".position, ").Append(right).Append(".position, ");
                        break;
                }
            }
            // ST
            else
            {
                sql.Append("\n-- st\n").Append(fn).Append('(');

                switch (op)
                {
                    case pythiaLexer.LALIGN:
                        sql.Append(left).Append(".start_position, ")
                           .Append(right).Append(".position, ");
                        break;
                    case pythiaLexer.RALIGN:
                        sql.Append(left).Append(".end_position, ")
                           .Append(right).Append(".position, ");
                        break;
                    default:
                        sql.Append(left).Append(".start_position, ")
                           .Append(left).Append(".end_position, ").Append(right)
                           .Append(".position, ").Append(right).Append(".position, ");
                        break;
                }
            }

            // close fn call args and add =1
            AppendFinalFnArgs(inside, sql);
            sql.Append(")\n");

            // S-argument
            AppendSArgWithToken(left, right, sql);
        }

        private void AppendStructureCollocationFilter(string left, string right,
            StringBuilder sql)
        {
            // position filter
            int op = (int)_locopArgs[ARG_OP];
            bool inside = op == pythiaLexer.INSIDE || op == pythiaLexer.NOTINSIDE;
            string fn = _sqlHelper.GetLexerFnName((int)_locopArgs[ARG_OP]);

            Tuple<char, char> locTypes = _locationState.GetCurrentPairTypes();

            // TS
            if (locTypes.Item1 == 'T' || locTypes.Item1 == 't')
            {
                sql.Append("\n-- ts\n").Append(fn).Append('(');

                switch (op)
                {
                    case pythiaLexer.LALIGN:
                        sql.Append(left).Append(".position, ").Append(right)
                            .Append(".start_position, ");
                        break;
                    case pythiaLexer.RALIGN:
                        sql.Append(left).Append(".position, ").Append(right)
                            .Append(".end_position, ");
                        break;
                    default:
                        sql.Append(left).Append(".position, ").Append(left)
                           .Append(".position, ").Append(right)
                           .Append(".start_position, ").Append(right)
                           .Append(".end_position, ");
                        break;
                }
            }
            // SS
            else
            {
                sql.Append("\n-- ss\n").Append(fn).Append('(');

                switch (op)
                {
                    case LALIGN:
                        sql.Append(left).Append(".start_position, ")
                           .Append(right).Append(".start_position, ");
                        break;
                    case RALIGN:
                        sql.Append(left).Append(".end_position, ")
                           .Append(right).Append(".end_position, ");
                        break;
                    default:
                        sql.Append(left).Append(".start_position, ")
                           .Append(left).Append(".end_position, ")
                           .Append(right).Append(".start_position, ")
                           .Append(right).Append(".end_position, ");
                        break;
                }
            }
            AppendFinalFnArgs(inside, sql);
            sql.Append(")\n");

            // S-argument
            AppendSArgWithStructure(left, right, sql);
        }

        private void HandleTxtSetPair(QuerySetPair pair, ITerminalNode id)
        {
            if (_locationState.IsActive) _locationState.CurrentPairNumber++;

            AppendPairComment(pair, true, _txtSetState.Sql);
            if (pair.IsStructure)
            {
                // structures draw their document positions from document_structure
                _txtSetState.Sql
                    .Append("SELECT DISTINCT\ndocument_structure.document_id,\n")
                    .Append("document_structure.position,\n")
                    .Append("'s' AS entity_type,\n")
                    .Append("document_structure.structure_id AS entity_id,\n")
                    .Append("structure.start_position,\n")
                    .Append("structure.end_position\n")
                    .Append("FROM document_structure\n");
            }
            else
            {
                // tokens draw their document positions from occurrence,
                // joined with token to allow filters access value/language
                _txtSetState.Sql
                    .Append("SELECT DISTINCT\noccurrence.document_id,\n")
                    .Append("occurrence.position,\n")
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
            // string alias = _locPairNr == 2 ? "c" : null;
            if (pair.IsStructure)
            {
                //AppendStructurePairFilter(pair, id, alias,
                //   _locPairNr == 2 ? "  " : null);
                AppendStructurePairFilter(pair, id);
            }
            else
            {
                // AppendTokenPairFilter(pair, id, alias);
                AppendTokenPairFilter(pair, id);
            }
        }

        /// <summary>
        /// Handles the specified terminal node (pair or operator) in a text set.
        /// </summary>
        /// <param name="node">The node.</param>
        private void HandleTxtSetTerminal(ITerminalNode node)
        {
            // https://stackoverflow.com/questions/47911252/how-to-get-the-current-rulecontext-class-when-visiting-a-terminalnode
            switch (node.Symbol.Type)
            {
                case pythiaLexer.AND:
                    _cteResult.Append("INTERSECT\n");
                    break;
                case pythiaLexer.OR:
                    _cteResult.Append("UNION\n");
                    break;
                case pythiaLexer.ANDNOT:
                    _cteResult.Append("EXCEPT\n");
                    break;

                //case pythiaLexer.NEAR:
                //case pythiaLexer.NOTNEAR:
                //case pythiaLexer.BEFORE:
                //case pythiaLexer.NOTBEFORE:
                //case pythiaLexer.AFTER:
                //case pythiaLexer.NOTAFTER:
                //case pythiaLexer.OVERLAPS:
                //case pythiaLexer.NOTOVERLAPS:
                //case pythiaLexer.LALIGN:
                //case pythiaLexer.NOTLALIGN:
                //case pythiaLexer.RALIGN:
                //case pythiaLexer.NOTRALIGN:
                //    ReadNearOperatorArgs(node);
                //    break;
                //case pythiaLexer.INSIDE:
                //case pythiaLexer.NOTINSIDE:
                //    ReadInsideOperatorArgs(node);
                //    break;

                case pythiaLexer.LPAREN:
                    if (!(node.Parent.RuleContext is LocopContext))
                    {
                        _cteResult.Append("(\n");
                        _currentCteDepth++;
                    }
                    break;
                case pythiaLexer.RPAREN:
                    if (!(node.Parent.RuleContext is LocopContext))
                    {
                        _cteResult.Append(")\n");
                        _currentCteDepth--;
                    }
                    break;

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
                    // corpus ID terminal, like "alpha" in @@alpha beta;
                    // add the corpus ID to the list of corpora IDs
                    if (node.Symbol.Type == pythiaLexer.ID)
                    {
                        string text = node.GetText().ToLowerInvariant();
                        if (!string.IsNullOrEmpty(text)) _corporaIds.Add(text);
                    }
                    break;

                case QuerySet.Document:
                    // document set: @docExpr;
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
}
