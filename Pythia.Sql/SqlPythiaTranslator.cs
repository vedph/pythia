using Antlr4.Runtime;
using Pythia.Core;
using Pythia.Core.Analysis;
using Pythia.Core.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Pythia.Core.Query.pythiaParser;

namespace Pythia.Sql;

/// <summary>
/// Translator of a Pythia query parse tree into SQL.
/// </summary>
/// <remarks>
/// <para>The SQL has this structure:</para>
/// <list type="number">
/// <item><description>an optional <c>d</c> CTE with the IDs of the documents
/// in the search scope, as defined by the corpus and document sets.
/// </description></item>
/// <item><description>a list of CTEs named <c>s1</c>, <c>s2</c>, etc. Each
/// selects the spans matching a filter, i.e. a pair, or a combination of
/// pairs connected by <c>AND</c>, <c>AND NOT</c> or <c>OR</c>: as all these
/// pairs refer to the same span, they are combined in the same <c>WHERE</c>
/// clause.</description></item>
/// <item><description>the <c>r</c> CTE with the result, which connects
/// the sN CTEs: location operators keep the spans of their left operand
/// having (or not having) at least a span of their right operand in the
/// required position, via <c>EXISTS</c> (or <c>NOT EXISTS</c>); logical
/// operators between location expressions become <c>INTERSECT</c>,
/// <c>EXCEPT</c> or <c>UNION</c>.</description></item>
/// <item><description>a final select, which joins the result with document
/// data, sorts and pages it; or just counts the results.</description></item>
/// </list>
/// <para>The result never contains duplicate spans, so that results and their
/// total count are consistent.</para>
/// </remarks>
public sealed class SqlPythiaTranslator
{
    // escape in pair's value: &HHHH;
    private static readonly Regex _escRegex = new("&([0-9a-fA-F]{1,4});",
        RegexOptions.Compiled);
    private static readonly char[] _wildcards = ['*', '?'];
    private static readonly string[] _insideArgs = ["ns", "ms", "ne", "me", "s"];
    private static readonly string[] _nearArgs = ["n", "m", "s"];

    private const string SPAN_COLUMNS = "span.id, span.document_id, span.type, " +
        "span.p1, span.p2, span.index, span.length, span.value";
    // the scope is an array rather than a subquery: a join with d would let
    // the database find the spans of the right operands of location
    // operators by document via d, rather than by position via their left
    // operand (an empty scope produces NULL, which matches no span)
    private const string SCOPE_FILTER =
        "span.document_id = ANY ((SELECT array_agg(id) FROM d)::int[])";

    /// <summary>
    /// A node of the text expression: either a filter on a single span,
    /// or a relation, i.e. a SQL query returning spans.
    /// </summary>
    private sealed class Node
    {
        /// <summary>The filter (WHERE condition on span) if any.</summary>
        public string? Filter { get; init; }

        /// <summary>The SQL query returning spans, if any.</summary>
        public string? Sql { get; set; }

        /// <summary>The CTE name corresponding to <see cref="Sql"/>, if any.
        /// </summary>
        public string? Table { get; set; }
    }

    /// <summary>
    /// Location operator arguments.
    /// </summary>
    private sealed class LocopArgs
    {
        public int Op { get; init; }
        public bool Not { get; init; }
        public string Name { get; init; } = "";
        public int N { get; set; }
        public int M { get; set; } = int.MaxValue;
        public int NS { get; set; }
        public int MS { get; set; } = int.MaxValue;
        public int NE { get; set; }
        public int ME { get; set; } = int.MaxValue;
        public string? S { get; set; }
    }

    private readonly ISqlHelper _sqlHelper;
    private readonly IList<ILiteralFilter> _literalFilters;
    private readonly List<string> _ctes = [];
    // the CTEs with the maximum length of the spans of a type or relation,
    // keyed by type or relation
    private readonly Dictionary<string, string> _lengthCtes = [];
    private bool _hasScope;
    // the depth of right operands of location operators being built: their
    // spans are always in the same document of a span of the left operand,
    // which is already in the search scope, so they need no scope filter
    // (which would also prevent finding them by position)
    private int _rightDepth;
    private int _pairNumber;
    private int _cteNumber;
    private int _aliasNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlPythiaTranslator"/>
    /// class.
    /// </summary>
    /// <param name="sqlHelper">The SQL helper.</param>
    /// <param name="literalFilters">The optional literal filters to apply
    /// to the literal values compared with the value of tokens.</param>
    /// <exception cref="ArgumentNullException">sqlHelper</exception>
    public SqlPythiaTranslator(ISqlHelper sqlHelper,
        IList<ILiteralFilter>? literalFilters = null)
    {
        _sqlHelper = sqlHelper ?? throw new ArgumentNullException(nameof(sqlHelper));
        _literalFilters = literalFilters ?? [];
    }

    #region Helpers
    private static PythiaQueryException CreateException(IToken token,
        string template, params object[] args)
    {
        object[] all = new object[args.Length + 2];
        all[0] = token.Line;
        all[1] = token.Column;
        Array.Copy(args, 0, all, 2, args.Length);

        return new PythiaQueryException(LocalizedStrings.Format(template, all))
        {
            Line = token.Line,
            Column = token.Column,
            Index = token.StartIndex,
            Length = token.StopIndex - token.StartIndex + 1
        };
    }

    /// <summary>
    /// Gets the value of the specified quoted value token, unwrapped from
    /// its quotes and with escapes (<c>&amp;HHHH;</c>) resolved.
    /// </summary>
    private static string DecodeValue(string quoted)
    {
        string value = quoted.Length > 1 && quoted[0] == '"' && quoted[^1] == '"'
            ? quoted[1..^1]
            : quoted;
        return value.Contains('&')
            ? _escRegex.Replace(value, m => new string((char)int.Parse(
                m.Groups[1].Value, NumberStyles.HexNumber,
                CultureInfo.InvariantCulture), 1))
            : value;
    }

    /// <summary>
    /// Builds a SQL string literal.
    /// </summary>
    private static string Lit(string text) =>
        "'" + text.Replace("\0", "").Replace("'", "''") + "'";

    /// <summary>
    /// Escapes the LIKE wildcards in the specified text, using <c>!</c>
    /// as the escape character.
    /// </summary>
    private static string EscapeLike(string text) =>
        text.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_");

    /// <summary>
    /// Makes the specified text safe for a single-line SQL comment.
    /// </summary>
    private static string Comment(string text) =>
        text.Replace('\r', ' ').Replace('\n', ' ');

    private string ApplyLiteralFilters(string text, bool apply)
    {
        if (!apply || _literalFilters.Count == 0) return text;
        StringBuilder sb = new(text);
        foreach (ILiteralFilter filter in _literalFilters)
        {
            filter.Apply(sb);
            if (sb.Length == 0) break;
        }
        return sb.ToString();
    }

    private static string GetNumber(string value, IToken token, string name)
    {
        if (!double.TryParse(value.Trim(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out double d) || !double.IsFinite(d))
        {
            throw CreateException(token,
                Properties.Resources.InvalidNumericValue, value, name);
        }
        return d.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string? GetNumericOperator(int op) => op switch
    {
        pythiaLexer.EQN => "=",
        pythiaLexer.NEQN => "<>",
        pythiaLexer.LT => "<",
        pythiaLexer.LTEQ => "<=",
        pythiaLexer.GT => ">",
        pythiaLexer.GTEQ => ">=",
        _ => null
    };

    private static string Like(string column, string pattern) =>
        $"LOWER({column}) LIKE LOWER({Lit(pattern)}) ESCAPE '!'";
    #endregion

    #region Pairs
    /// <summary>
    /// Builds the condition comparing the specified column with the specified
    /// value using the specified operator.
    /// </summary>
    /// <param name="column">The column (or column expression).</param>
    /// <param name="op">The operator token type.</param>
    /// <param name="value">The value.</param>
    /// <param name="numeric">True if the column is numeric.</param>
    /// <param name="filter">True to apply literal filters to the value.</param>
    /// <param name="opToken">The operator token, for errors.</param>
    /// <param name="name">The attribute name, for errors.</param>
    /// <returns>SQL condition.</returns>
    private string BuildCondition(string column, int op, string value,
        bool numeric, bool filter, IToken opToken, string name)
    {
        string? numOp = GetNumericOperator(op);
        if (numOp != null)
        {
            string number = GetNumber(value, opToken, name);
            return numeric
                ? $"{column} {numOp} {number}"
                : $"{_sqlHelper.BuildTextAsNumber(column)} {numOp} {number}";
        }

        // numeric columns are compared as numbers also with = and <>,
        // and do not support any other operator
        if (numeric)
        {
            return op switch
            {
                pythiaLexer.EQ =>
                    $"{column} = {GetNumber(value, opToken, name)}",
                pythiaLexer.NEQ =>
                    $"{column} <> {GetNumber(value, opToken, name)}",
                _ => throw CreateException(opToken,
                    Properties.Resources.InvalidOperatorForNumericField,
                    opToken.Text)
            };
        }

        switch (op)
        {
            case pythiaLexer.EQ:
                return $"LOWER({column}) = " +
                    $"LOWER({Lit(ApplyLiteralFilters(value, filter))})";

            case pythiaLexer.NEQ:
                return $"LOWER({column}) <> " +
                    $"LOWER({Lit(ApplyLiteralFilters(value, filter))})";

            case pythiaLexer.CONTAINS:
                return Like(column,
                    "%" + EscapeLike(ApplyLiteralFilters(value, filter)) + "%");

            case pythiaLexer.STARTSWITH:
                return Like(column,
                    EscapeLike(ApplyLiteralFilters(value, filter)) + "%");

            case pythiaLexer.ENDSWITH:
                return Like(column,
                    "%" + EscapeLike(ApplyLiteralFilters(value, filter)));

            case pythiaLexer.WILDCARDS:
                // without wildcards, this is an equality
                if (value.IndexOfAny(_wildcards) == -1)
                {
                    goto case pythiaLexer.EQ;
                }
                // escape the literal parts, and translate * into % and ? into _
                StringBuilder pattern = new();
                int start = 0;
                for (int i = 0; i <= value.Length; i++)
                {
                    if (i < value.Length && value[i] != '*' && value[i] != '?')
                        continue;
                    pattern.Append(EscapeLike(ApplyLiteralFilters(
                        value[start..i], filter)));
                    if (i < value.Length) pattern.Append(value[i] == '*' ? '%' : '_');
                    start = i + 1;
                }
                return Like(column, pattern.ToString());

            case pythiaLexer.REGEXP:
                return _sqlHelper.BuildRegexMatch(column, value);

            case pythiaLexer.SIMILAR:
                return _sqlHelper.BuildFuzzyMatch(column, value);

            default:
                throw new InvalidOperationException($"Unexpected operator {op}");
        }
    }

    /// <summary>
    /// Builds the condition for the specified span attribute.
    /// </summary>
    /// <param name="name">The attribute name.</param>
    /// <param name="op">The operator token, or null for a short pair.</param>
    /// <param name="value">The value, or null for a short pair.</param>
    /// <param name="nameToken">The name token, for errors.</param>
    /// <param name="token">True if the span is a token.</param>
    /// <returns>SQL condition.</returns>
    private string BuildSpanAttrCondition(string name, IToken? op, string? value,
        IToken nameToken, bool token)
    {
        string lower = name.ToLowerInvariant();

        // privileged attributes are columns of span
        if (TextSpan.IsPrivilegedSpanAttr(lower))
        {
            if (op == null)
            {
                throw CreateException(nameToken,
                    Properties.Resources.InvalidShortPair, nameToken.Text);
            }
            return BuildCondition("span." + _sqlHelper.EscapeKeyword(lower),
                op.Type, value!,
                TextSpan.IsNumericPrivilegedSpanAttr(lower),
                token && lower == "value",
                op, name);
        }

        // other attributes are in span_attribute: they just need to exist,
        // or to have the specified value
        StringBuilder sb = new();
        sb.Append("EXISTS (SELECT 1 FROM span_attribute AS sa " +
            "WHERE sa.span_id = span.id AND LOWER(sa.name) = LOWER(")
          .Append(Lit(name)).Append(')');
        if (op != null)
        {
            sb.Append(" AND ").Append(BuildCondition("sa.value", op.Type, value!,
                false, false, op, name));
        }
        sb.Append(')');
        return sb.ToString();
    }

    /// <summary>
    /// Builds the condition for the specified structure attribute.
    /// The name of a structure is its span type.
    /// </summary>
    private string BuildStructureAttrCondition(string name, IToken? op,
        string? value, IToken nameToken)
    {
        if (name.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            // any structure
            if (op == null) return "span.type <> 'tok'";

            // types are compared literally, so that their index can be used
            if (op.Type == pythiaLexer.EQ)
                return $"span.type <> 'tok' AND span.type = {Lit(value!)}";

            return "span.type <> 'tok' AND " +
                BuildCondition("span.type", op.Type, value!, false, false,
                    op, name);
        }

        return "span.type <> 'tok' AND " +
            BuildSpanAttrCondition(name, op, value, nameToken, false);
    }

    /// <summary>
    /// Builds the filter for the specified pair.
    /// </summary>
    private string BuildPairFilter(PairContext pair)
    {
        int n = ++_pairNumber;
        SpairContext? spair = pair.spair();
        TpairContext? tpair = pair.tpair();
        IToken nameToken = spair?.name ?? tpair!.name;
        IToken? op = spair?.@operator ?? tpair?.@operator;
        IToken? valueToken = spair?.value ?? tpair?.value;
        string? value = valueToken != null ? DecodeValue(valueToken.Text) : null;
        string name = nameToken.Text;

        StringBuilder sb = new();
        sb.Append("-- p").Append(n).Append(": ").Append(Comment(name));
        if (op != null)
        {
            sb.Append(' ').Append(pythiaLexer.DefaultVocabulary
                .GetSymbolicName(op.Type))
              .Append(" \"").Append(Comment(value!)).Append('"');
        }
        sb.Append('\n');

        // structure: $NAME or $ATTR OP VALUE ($_ATTR is a legacy alias of $ATTR)
        if (spair != null)
        {
            string attr = name[1..];
            bool legacy = attr.Length > 1 && attr[0] == '_';
            if (legacy) attr = attr[1..];

            sb.Append(op == null && !legacy
                ? $"span.type = {Lit(attr)}"
                : BuildStructureAttrCondition(attr, op, value, nameToken));
            return sb.ToString();
        }

        // _ATTR: legacy alias of $ATTR
        if (name.Length > 1 && name[0] == '_')
        {
            sb.Append(BuildStructureAttrCondition(name[1..], op, value, nameToken));
            return sb.ToString();
        }

        // token
        sb.Append("span.type = 'tok' AND ")
          .Append(BuildSpanAttrCondition(name, op, value, nameToken, true));
        return sb.ToString();
    }
    #endregion

    #region Scope
    private string BuildDocPairCondition(TpairContext pair)
    {
        IToken nameToken = pair.name;
        string name = nameToken.Text;
        string lower = name.ToLowerInvariant();
        IToken? op = pair.@operator;
        string? value = pair.value != null ? DecodeValue(pair.value.Text) : null;

        string comment = "-- " + Comment(name) + (op != null
            ? $" {pythiaLexer.DefaultVocabulary.GetSymbolicName(op.Type)} " +
              $"\"{Comment(value!)}\""
            : "") + "\n";

        // privileged attributes are columns of document
        if (lower == "id" || TextSpan.IsPrivilegedDocAttr(lower))
        {
            if (op == null)
            {
                throw CreateException(nameToken,
                    Properties.Resources.InvalidShortPair, name);
            }
            return comment + BuildCondition("document." + lower, op.Type, value!,
                lower == "id" || lower == "date_value", false, op, name);
        }

        // other attributes are in document_attribute
        StringBuilder sb = new(comment);
        sb.Append("EXISTS (SELECT 1 FROM document_attribute AS da " +
            "WHERE da.document_id = document.id AND LOWER(da.name) = LOWER(")
          .Append(Lit(name)).Append(')');
        if (op != null)
        {
            sb.Append(" AND ").Append(BuildCondition("da.value", op.Type, value!,
                false, false, op, name));
        }
        sb.Append(')');
        return sb.ToString();
    }

    private string BuildDocExpr(DocExprContext context)
    {
        switch (context)
        {
            case DeParenContext paren:
                return BuildDocExpr(paren.docExpr());
            case DePairContext pair:
                return BuildDocPairCondition(pair.tpair());
            case DeAndContext and:
                return $"({BuildDocExpr(and.docExpr(0))})\n" +
                    (and.ANDNOT() != null ? "AND NOT" : "AND") +
                    $" ({BuildDocExpr(and.docExpr(1))})";
            case DeOrContext or:
                return $"({BuildDocExpr(or.docExpr(0))})\n" +
                    (or.ORNOT() != null ? "OR NOT" : "OR") +
                    $" ({BuildDocExpr(or.docExpr(1))})";
            default:
                throw new InvalidOperationException(
                    "Unexpected document expression: " + context.GetText());
        }
    }

    /// <summary>
    /// Builds the query selecting the IDs of the documents in the search
    /// scope, or null if the search scope is not limited.
    /// </summary>
    private string? BuildScope(CorSetContext? corSet, DocSetContext? docSet)
    {
        List<string> conditions = [];
        if (docSet != null) conditions.Add(BuildDocExpr(docSet.docExpr()));

        if (corSet != null)
        {
            // corpus IDs: identifiers or quoted values
            string[] ids = [.. corSet.children
                .Where(c => c is Antlr4.Runtime.Tree.ITerminalNode t &&
                    (t.Symbol.Type == pythiaLexer.ID ||
                     t.Symbol.Type == pythiaLexer.QVALUE))
                .Select(c => c.GetText().StartsWith('"')
                    ? DecodeValue(c.GetText())
                    : c.GetText())
                .Distinct()];

            conditions.Add("EXISTS (SELECT 1 FROM document_corpus AS dc " +
                "WHERE dc.document_id = document.id AND LOWER(dc.corpus_id) IN (" +
                string.Join(", ", ids.Select(id => $"LOWER({Lit(id)})")) + "))");
        }

        if (conditions.Count == 0) return null;
        return "SELECT document.id FROM document\nWHERE " +
            string.Join("\nAND ", conditions.Select(c => $"({c})")) + "\n";
    }
    #endregion

    #region Text
    /// <summary>
    /// Gets the SQL query corresponding to the specified node, adding
    /// a CTE for it when it is a filter.
    /// </summary>
    private string GetSql(Node node)
    {
        if (node.Sql != null) return node.Sql;

        string name = "s" + ++_cteNumber;
        _ctes.Add($"{name} AS NOT MATERIALIZED\n(\nSELECT {SPAN_COLUMNS}\n" +
            "FROM span\nWHERE " +
            (_hasScope && _rightDepth == 0 ? SCOPE_FILTER + "\nAND " : "") +
            $"(\n{node.Filter}\n)\n) -- {name}\n");
        node.Table = name;
        node.Sql = "SELECT * FROM " + name;
        return node.Sql;
    }

    private string GetRelation(Node node)
    {
        string sql = GetSql(node);
        return node.Table ?? $"(\n{sql}\n)";
    }

    private static string Offset(string expression, long offset)
    {
        if (offset == 0) return expression;
        // avoid integer overflows with huge distances
        if (Math.Abs(offset) > 1_000_000_000)
            expression += "::bigint";
        return offset > 0
            ? $"{expression} + {offset.ToString(CultureInfo.InvariantCulture)}"
            : $"{expression} - {(-offset).ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Builds a condition requiring that <paramref name="column"/> is between
    /// <paramref name="expression"/> plus <paramref name="min"/> and
    /// <paramref name="expression"/> plus <paramref name="max"/>, where null
    /// means unbounded.
    /// </summary>
    private static string Between(string column, string expression,
        long? min, long? max)
    {
        if (min.HasValue && max.HasValue && min == max)
            return $"{column} = {Offset(expression, min.Value)}";

        List<string> conditions = [];
        if (min.HasValue) conditions.Add($"{column} >= {Offset(expression, min.Value)}");
        if (max.HasValue) conditions.Add($"{column} <= {Offset(expression, max.Value)}");
        return string.Join(" AND ", conditions);
    }

    private static long? Max(int value) =>
        value == int.MaxValue ? null : value;

    /// <summary>
    /// Builds the position conditions of span <paramref name="b"/> with
    /// reference to span <paramref name="a"/>. Each condition is an
    /// alternative (NEAR has two, before and after). All the conditions are
    /// expressed on the columns of b, so that indexes can be used to find b.
    /// </summary>
    /// <param name="args">The location operator arguments.</param>
    /// <param name="a">The alias of A.</param>
    /// <param name="b">The alias of B.</param>
    /// <param name="bLength">An expression with the maximum length of B
    /// spans, used to add a lower bound to the start of B when B must
    /// include or overlap A; or null.</param>
    private static List<string> BuildPositionConditions(LocopArgs args,
        string a, string b, string? bLength)
    {
        switch (args.Op)
        {
            case pythiaLexer.BEFORE:
                // A before B: B starts after A ends, at distance N-M
                return [Between($"{b}.p1", $"{a}.p2", 1L + args.N,
                    Max(args.M) + 1L)];

            case pythiaLexer.AFTER:
                // A after B: B ends before A starts, at distance N-M
                return [Between($"{b}.p2", $"{a}.p1", -1L - Max(args.M),
                    -1L - args.N)];

            case pythiaLexer.NEAR:
                return [
                    Between($"{b}.p1", $"{a}.p2", 1L + args.N, Max(args.M) + 1L),
                    Between($"{b}.p2", $"{a}.p1", -1L - Max(args.M), -1L - args.N)
                ];

            case pythiaLexer.INSIDE:
            {
                // A inside B: B starts NS-MS before A, and ends NE-ME after A
                string inside = Between($"{b}.p1", $"{a}.p1",
                        -(long?)Max(args.MS), -(long)args.NS) +
                    " AND " +
                    Between($"{b}.p2", $"{a}.p2", args.NE, Max(args.ME));
                // B ends after A ends, so it cannot start before A end
                // minus the maximum length of B
                if (args.MS == int.MaxValue && bLength != null)
                    inside += $" AND {b}.p1 >= {a}.p2 - {bLength}";
                return [inside];
            }

            case pythiaLexer.OVERLAPS:
            {
                // A overlaps B by N-M positions
                string overlap = $"{b}.p1 <= {a}.p2 AND {b}.p2 >= {a}.p1";
                string count = $"LEAST({a}.p2, {b}.p2) - " +
                    $"GREATEST({a}.p1, {b}.p1) + 1";
                if (args.N > 1) overlap += $" AND {count} >= {args.N}";
                if (args.M != int.MaxValue) overlap += $" AND {count} <= {args.M}";
                // B ends after A starts, so it cannot start before A start
                // minus the maximum length of B
                if (bLength != null) overlap += $" AND {b}.p1 >= {a}.p1 - {bLength}";
                return [overlap];
            }

            case pythiaLexer.LALIGN:
                // A starts N-M positions after the start of B
                return [Between($"{b}.p1", $"{a}.p1", -(long?)Max(args.M),
                    -(long)args.N)];

            case pythiaLexer.RALIGN:
                // A ends N-M positions before the end of B
                return [Between($"{b}.p2", $"{a}.p2", args.N, Max(args.M))];

            default:
                throw new InvalidOperationException(
                    $"Unexpected location operator {args.Op}");
        }
    }

    private static int GetBaseOperator(int op) => op switch
    {
        pythiaLexer.NOTNEAR => pythiaLexer.NEAR,
        pythiaLexer.NOTBEFORE => pythiaLexer.BEFORE,
        pythiaLexer.NOTAFTER => pythiaLexer.AFTER,
        pythiaLexer.NOTINSIDE => pythiaLexer.INSIDE,
        pythiaLexer.NOTOVERLAPS => pythiaLexer.OVERLAPS,
        pythiaLexer.NOTLALIGN => pythiaLexer.LALIGN,
        pythiaLexer.NOTRALIGN => pythiaLexer.RALIGN,
        _ => op
    };

    private static LocopArgs ParseLocop(LocopContext context)
    {
        IToken opToken = context.@operator;
        int op = GetBaseOperator(opToken.Type);
        string opName = Regex.Replace(opToken.Text, @"\s+", " ");
        LocopArgs args = new()
        {
            Op = op,
            Not = op != opToken.Type,
            Name = opName
        };
        string[] allowed = op == pythiaLexer.INSIDE ? _insideArgs : _nearArgs;
        HashSet<string> found = [];
        Dictionary<string, IToken> tokens = [];

        foreach (LocArgContext arg in context.locArg())
        {
            string name = arg.name.Text.ToLowerInvariant();
            if (Array.IndexOf(allowed, name) == -1)
            {
                throw CreateException(arg.name,
                    Properties.Resources.ArgopUnknownArg, opName, arg.name.Text,
                    string.Join(", ", allowed));
            }
            if (!found.Add(name))
            {
                throw CreateException(arg.name,
                    Properties.Resources.ArgopDuplicateArg, opName, arg.name.Text);
            }
            tokens[name] = arg.name;

            // the value is the 3rd child: name = value
            IToken valueToken = ((Antlr4.Runtime.Tree.ITerminalNode)
                arg.GetChild(2)).Symbol;

            if (name == "s")
            {
                if (valueToken.Type != pythiaLexer.ID)
                {
                    throw CreateException(valueToken,
                        Properties.Resources.ArgopInvalidSArg, opName,
                        valueToken.Text);
                }
                args.S = valueToken.Text;
                continue;
            }

            if (valueToken.Type != pythiaLexer.INT ||
                !int.TryParse(valueToken.Text, NumberStyles.None,
                    CultureInfo.InvariantCulture, out int n))
            {
                throw CreateException(valueToken,
                    Properties.Resources.ArgopInvalidNArg, opName, valueToken.Text);
            }
            switch (name)
            {
                case "n": args.N = n; break;
                case "m": args.M = n; break;
                case "ns": args.NS = n; break;
                case "ms": args.MS = n; break;
                case "ne": args.NE = n; break;
                case "me": args.ME = n; break;
            }
        }

        // minimum values cannot be greater than maximum values
        if (args.N > args.M)
        {
            throw CreateException(tokens.GetValueOrDefault("n") ?? opToken,
                Properties.Resources.ArgopNGreaterThanM, opName, args.N, args.M);
        }
        if (args.NS > args.MS)
        {
            throw CreateException(tokens.GetValueOrDefault("ns") ?? opToken,
                Properties.Resources.ArgopNSGreaterThanMS, opName, args.NS,
                args.MS);
        }
        if (args.NE > args.ME)
        {
            throw CreateException(tokens.GetValueOrDefault("ne") ?? opToken,
                Properties.Resources.ArgopNEGreaterThanME, opName, args.NE,
                args.ME);
        }
        return args;
    }

    private static string GetArgsComment(LocopArgs args)
    {
        List<string> list = [];
        if (args.Op == pythiaLexer.INSIDE)
        {
            list.Add($"ns={args.NS}");
            if (args.MS != int.MaxValue) list.Add($"ms={args.MS}");
            list.Add($"ne={args.NE}");
            if (args.ME != int.MaxValue) list.Add($"me={args.ME}");
        }
        else
        {
            list.Add($"n={args.N}");
            if (args.M != int.MaxValue) list.Add($"m={args.M}");
        }
        if (args.S != null) list.Add($"s={args.S}");
        return string.Join(",", list);
    }

    /// <summary>
    /// Gets an expression with the maximum length (p2 - p1) of the spans
    /// selected by the specified SQL, adding a CTE for it if not already
    /// present. This is used to bound the start of spans which must
    /// include or overlap other spans, so that indexes on positions can
    /// limit their search.
    /// </summary>
    /// <param name="key">The key used to share the CTE.</param>
    /// <param name="sql">The SQL selecting spans as <c>span</c>.</param>
    /// <returns>Expression.</returns>
    private string GetMaxLength(string key, string sql)
    {
        if (!_lengthCtes.TryGetValue(key, out string? name))
        {
            name = "l" + (_lengthCtes.Count + 1);
            _lengthCtes[key] = name;
            _ctes.Add($"{name} AS MATERIALIZED\n(\n" +
                $"SELECT COALESCE(MAX(span.p2 - span.p1), 0) AS len FROM {sql}\n" +
                $") -- {name}\n");
        }
        return $"(SELECT len FROM {name})";
    }

    private Node BuildLocation(TeLocationContext context)
    {
        LocopArgs args = ParseLocop(context.locop());

        // operands are built in their order, so that CTEs are numbered
        // in the order of the pairs in the query
        string leftRel = GetRelation(BuildTxtExpr(context.txtExpr(0)));
        _rightDepth++;
        Node right = BuildTxtExpr(context.txtExpr(1));
        string rightRel = GetRelation(right);
        _rightDepth--;
        int k = ++_aliasNumber;
        string a = "a" + k, b = "b" + k, c = "c" + k;

        // the optional structure including both A and B: its start is
        // bounded by the maximum length of its type, so that its search
        // does not scan all the structures of the document
        string structure = args.S == null
            ? ""
            : $"\n  AND EXISTS (SELECT 1 FROM span AS {c} " +
              $"WHERE {c}.document_id = {a}.document_id AND {c}.type = {Lit(args.S)}" +
              $" AND {c}.p1 <= LEAST({a}.p1, {b}.p1)" +
              $" AND {c}.p1 >= GREATEST({a}.p2, {b}.p2) - " +
              GetMaxLength("type:" + args.S,
                  $"span WHERE span.type = {Lit(args.S)}") +
              $" AND {c}.p2 >= GREATEST({a}.p2, {b}.p2))";

        // the maximum length of B, when B must include or overlap A
        // and B is a filter (a CTE)
        string? bLength = right.Table != null &&
            (args.Op == pythiaLexer.INSIDE || args.Op == pythiaLexer.OVERLAPS)
            ? GetMaxLength("cte:" + right.Table, $"{right.Table} AS span")
            : null;

        string comment = $"-- {args.Name}({GetArgsComment(args)})\n";

        // a single EXISTS (rather than one for each alternative position)
        // lets the database turn it into a semi-join (or anti-join)
        List<string> positions = BuildPositionConditions(args, a, b, bLength);
        string position = positions.Count == 1
            ? positions[0]
            : "(" + string.Join("\n    OR ", positions.Select(p => $"({p})")) + ")";
        string test = $"EXISTS (SELECT 1 FROM {rightRel} AS {b}\n" +
            $"  WHERE {b}.document_id = {a}.document_id AND {position}" +
            $"{structure})";

        return new Node
        {
            Sql = comment +
                $"SELECT {a}.* FROM {leftRel} AS {a}\n" +
                $"WHERE {(args.Not ? "NOT " : "")}{test}"
        };
    }

    private Node BuildTxtExpr(TxtExprContext context)
    {
        switch (context)
        {
            case TeParenContext paren:
                return BuildTxtExpr(paren.txtExpr());

            case TePairContext pair:
                return new Node { Filter = BuildPairFilter(pair.pair()) };

            case TeAndContext and:
            {
                Node left = BuildTxtExpr(and.txtExpr(0));
                Node right = BuildTxtExpr(and.txtExpr(1));
                bool not = and.ANDNOT() != null;

                // filters refer to the same span, so they can be combined
                if (left.Filter != null && right.Filter != null)
                {
                    return new Node
                    {
                        Filter = not
                            ? $"({left.Filter}\n)\nAND (({right.Filter}\n) IS NOT TRUE)"
                            : $"({left.Filter}\n)\nAND ({right.Filter}\n)"
                    };
                }
                return new Node
                {
                    Sql = $"({GetSql(left)})\n{(not ? "EXCEPT" : "INTERSECT")}\n" +
                        $"({GetSql(right)})"
                };
            }

            case TeOrContext or:
            {
                Node left = BuildTxtExpr(or.txtExpr(0));
                Node right = BuildTxtExpr(or.txtExpr(1));
                if (left.Filter != null && right.Filter != null)
                {
                    return new Node
                    {
                        Filter = $"({left.Filter}\n)\nOR ({right.Filter}\n)"
                    };
                }
                return new Node
                {
                    Sql = $"({GetSql(left)})\nUNION\n({GetSql(right)})"
                };
            }

            case TeLocationContext location:
                return BuildLocation(location);

            default:
                throw new InvalidOperationException(
                    "Unexpected text expression: " + context.GetText());
        }
    }
    #endregion

    #region Final
    /// <summary>
    /// Builds the ORDER BY clause content for the specified sort fields.
    /// The default is <c>value, sort_key, p1</c>. The result ID is always
    /// appended as the last field, so that paging is deterministic.
    /// </summary>
    private static string BuildSort(IEnumerable<string>? sortFields)
    {
        List<string> fields = [];
        foreach (string field in sortFields?.Where(f => f.Length > 0) ?? [])
        {
            // parse optional +/- prefix for ASC/DESC
            string f = field.ToLowerInvariant();
            bool desc = false;
            if (f[0] == '+' || f[0] == '-')
            {
                desc = f[0] == '-';
                f = f[1..];
            }
            if (f.Length == 0) continue;

            string sql = f switch
            {
                // result
                "p1" or "p2" or "type" or "index" or "length" or "value" => "r." + f,
                // corner case: reversed value (computed)
                "reversed_value" => "REVERSE(r.value)",
                // document
                "id" or "author" or "title" or "source" or "profile_id" or
                "user_id" or "date_value" or "sort_key" or "last_modified"
                    => "document." + f,
                // custom document attribute
                _ => "(SELECT da.value FROM document_attribute AS da " +
                     "WHERE da.document_id = document.id AND " +
                     $"LOWER(da.name) = LOWER({Lit(f)}) ORDER BY da.value LIMIT 1)"
            };
            fields.Add(desc ? sql + " DESC" : sql);
        }

        if (fields.Count == 0)
            fields.AddRange(["r.value", "document.sort_key", "r.p1"]);
        fields.Add("r.id");
        return string.Join(", ", fields);
    }

    /// <summary>
    /// Translates the specified query.
    /// </summary>
    /// <param name="query">The query parse tree.</param>
    /// <param name="pageNumber">The page number (1-N).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="sortFields">The optional sort fields. If not specified,
    /// the results are sorted by value, document sort key, and position.
    /// Otherwise, they are sorted by all the fields specified here, in their
    /// order, each optionally prefixed by <c>-</c> for descending order.
    /// Valid fields are: value, reversed_value, p1, p2, type, index, length,
    /// id, author, title, source, profile_id, user_id, date_value, sort_key,
    /// last_modified, or any document attribute name.</param>
    /// <returns>Tuple with 1=results page SQL query and 2=total count SQL
    /// query.</returns>
    /// <exception cref="ArgumentNullException">query</exception>
    /// <exception cref="PythiaQueryException">query error</exception>
    public Tuple<string, string> Translate(QueryContext query, int pageNumber,
        int pageSize, IEnumerable<string>? sortFields = null)
    {
        ArgumentNullException.ThrowIfNull(query);

        _ctes.Clear();
        _rightDepth = 0;
        _lengthCtes.Clear();
        _pairNumber = _cteNumber = _aliasNumber = 0;

        string? scope = BuildScope(query.corSet(), query.docSet());
        _hasScope = scope != null;
        string result = GetSql(BuildTxtExpr(query.txtExpr()));

        StringBuilder sql = new("WITH ");
        if (scope != null)
            sql.Append("d AS MATERIALIZED\n(\n").Append(scope).Append(") -- d\n, ");
        foreach (string cte in _ctes) sql.Append(cte).Append(", ");
        // r is materialized, so that its rows are computed once and then
        // sorted: else, for a page of results the database might scan an
        // index sorted by value hoping to stop early, which with rare
        // matches means scanning almost the whole index
        sql.Append("r AS MATERIALIZED\n(\n").Append(result).Append("\n) -- r\n");
        string head = sql.ToString();

        string data = head +
            "SELECT r.id, r.document_id, r.p1, r.p2, r.type,\n" +
            "r.index, r.length, r.value,\n" +
            "document.author, document.title, document.sort_key\n" +
            "FROM r\n" +
            "INNER JOIN document ON r.document_id = document.id\n" +
            "ORDER BY " + BuildSort(sortFields) + "\n" +
            _sqlHelper.BuildPaging((pageNumber - 1) * pageSize, pageSize);

        string count = head + "SELECT COUNT(*) FROM r\n";

        return Tuple.Create(data, count);
    }
    #endregion
}
