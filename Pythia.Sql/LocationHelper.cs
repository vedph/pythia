using Antlr4.Runtime.Tree;
using Antlr4.Runtime;
using Pythia.Core.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static Pythia.Core.Query.pythiaParser;

namespace Pythia.Sql;

/// <summary>
/// Location operator helper.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LocationState"/> class.
/// </remarks>
/// <param name="vocabulary">The vocabulary, used to get symbol names.
/// </param>
/// <param name="sqlHelper">The SQL helper.</param>
public sealed class LocationHelper(IVocabulary vocabulary, ISqlHelper sqlHelper)
{
    #region Constants
    // keys used for locop arguments in parsing query    
    /// <summary>
    /// The op argument.
    /// </summary>
    public const string ARG_OP = "op";
    /// <summary>
    /// The not argument.
    /// </summary>
    public const string ARG_NOT = "not";
    /// <summary>
    /// The n argument.
    /// </summary>
    public const string ARG_N = "n";
    /// <summary>
    /// The m argument.
    /// </summary>
    public const string ARG_M = "m";
    /// <summary>
    /// The s argument.
    /// </summary>
    public const string ARG_S = "s";
    /// <summary>
    /// The ns argument.
    /// </summary>
    public const string ARG_NS = "ns";
    /// <summary>
    /// The ms argument.
    /// </summary>
    public const string ARG_MS = "ms";
    /// <summary>
    /// The ne argument.
    /// </summary>
    public const string ARG_NE = "ne";
    /// <summary>
    /// The me argument.
    /// </summary>
    public const string ARG_ME = "me";
    #endregion

    private readonly IVocabulary _vocabulary = vocabulary
            ?? throw new ArgumentNullException(nameof(vocabulary));
    private readonly ISqlHelper _sqlHelper = sqlHelper
            ?? throw new ArgumentNullException(nameof(sqlHelper));

    /// <summary>
    /// Gets the current locop arguments.
    /// </summary>
    public Dictionary<string, object> LocopArgs { get; } = [];

    /// <summary>
    /// Resets this state.
    /// </summary>
    /// query-wide data.</param>
    public void Reset()
    {
        LocopArgs.Clear();
    }

    /// <summary>
    /// Gets the minimum value for the argument with the specified name,
    /// or its default.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentNullException">name</exception>
    public int GetMinArgValue(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return LocopArgs.TryGetValue(name, out object? value) ?
            (int)value :
            0;
    }

    /// <summary>
    /// Gets the maximum value for the argument with the specified name,
    /// or its default.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentNullException">name</exception>
    public int GetMaxArgValue(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return LocopArgs.TryGetValue(name, out object? value) ?
            (int)value :
            int.MaxValue;
    }

    /// <summary>
    /// Sets the current locop operation type and negation.
    /// </summary>
    /// <param name="op">The operation type.</param>
    /// <param name="not">if set to <c>true</c>, the operation is negated with
    /// a <c>not</c>.</param>
    public void SetLocop(int op, bool not)
    {
        LocopArgs[ARG_OP] = op;
        if (not) LocopArgs[ARG_NOT] = true;
    }

    /// <summary>
    /// Finds the first terminal node descending from <paramref name="context"/>
    /// and matching <paramref name="filter"/>.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="filter">The filter.</param>
    /// <returns>Terminal node or null if not found.</returns>
    private static ITerminalNode? FindTerminalFrom(IRuleNode context,
        Func<ITerminalNode, bool> filter)
    {
        for (int i = 0; i < context.ChildCount; i++)
        {
            IParseTree child = context.GetChild(i);
            ITerminalNode leaf = (child as ITerminalNode)!;
            if (leaf == null)
            {
                IRuleNode rule = (child as IRuleNode)!;
                if (rule != null) return FindTerminalFrom(rule, filter);
            }
            else if (filter(leaf))
            {
                return leaf;
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
    private void ValidateNearOperatorArgs(LocopContext context)
    {
        // supply defaults if missing N, M
        if (!LocopArgs.ContainsKey(ARG_N))
            LocopArgs[ARG_N] = 0;
        if (!LocopArgs.ContainsKey(ARG_M))
            LocopArgs[ARG_M] = int.MaxValue;

        // S is not allowed with NOT (S is the context shared between
        // two items, while NOT negates the second item)
        if (LocopArgs.ContainsKey(ARG_S))
        {
            bool not = LocopArgs.ContainsKey(ARG_NOT) &&
                (bool)LocopArgs[ARG_NOT];

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
        int n = (int)LocopArgs[ARG_N];
        int m = (int)LocopArgs[ARG_M];
        if (n > m)
        {
            // the reference node for the error is the n/m child
            ITerminalNode node =
                FindTerminalFrom(context, leaf => leaf.GetText() == "n") ??
                FindTerminalFrom(context, leaf => leaf.GetText() == "m")!;

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
    private void ValidateInsideOperatorArgs(LocopContext context)
    {
        // supply defaults if missing NS, MS, NE, ME
        if (!LocopArgs.ContainsKey(ARG_NS)) LocopArgs[ARG_NS] = 0;
        if (!LocopArgs.ContainsKey(ARG_MS)) LocopArgs[ARG_MS] = int.MaxValue;
        if (!LocopArgs.ContainsKey(ARG_NE)) LocopArgs[ARG_NE] = 0;
        if (!LocopArgs.ContainsKey(ARG_ME)) LocopArgs[ARG_ME] = int.MaxValue;

        // ns cannot be greater than ms
        int ns = (int)LocopArgs[ARG_NS];
        int ms = (int)LocopArgs[ARG_MS];
        if (ns > ms)
        {
            // the reference node for the error is the ns/ms child
            ITerminalNode node =
                FindTerminalFrom(context, leaf => leaf.GetText() == "ns") ??
                FindTerminalFrom(context, leaf => leaf.GetText() == "ms")!;

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
        int ne = (int)LocopArgs[ARG_NE];
        int me = (int)LocopArgs[ARG_ME];
        if (ne > me)
        {
            // the reference node for the error is the ns/ms child
            ITerminalNode node =
                FindTerminalFrom(context, leaf => leaf.GetText() == "ne") ??
                FindTerminalFrom(context, leaf => leaf.GetText() == "me")!;

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
        if (LocopArgs.ContainsKey(ARG_S))
        {
            bool not = LocopArgs.ContainsKey(ARG_NOT) &&
                (bool)LocopArgs[ARG_NOT];

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

    /// <summary>
    /// Determines whether the specified symbol type corresponds to a negated
    /// function.
    /// </summary>
    /// <param name="symbolType">Type of the symbol.</param>
    /// <returns><c>true</c> if negated; otherwise, <c>false</c>.</returns>
    public static bool IsNotFn(int symbolType)
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
    /// Determines whether the specified symbol type corresponds to a location
    /// function, either negated or not.
    /// </summary>
    /// <param name="symbolType">Type of the symbol.</param>
    /// <returns>true if location function; otherwise, <c>false</c>.</returns>
    public static bool IsFn(int symbolType)
    {
        return symbolType == pythiaLexer.AFTER ||
            symbolType == pythiaLexer.BEFORE ||
            symbolType == pythiaLexer.INSIDE ||
            symbolType == pythiaLexer.LALIGN ||
            symbolType == pythiaLexer.NEAR ||
            symbolType == pythiaLexer.OVERLAPS ||
            symbolType == pythiaLexer.RALIGN ||
            IsNotFn(symbolType);
    }

    /// <summary>
    /// Validates the current locop arguments, throwing an exception if invalid.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="vocabulary">The vocabulary.</param>
    /// <exception cref="ArgumentNullException">context or vocabulary</exception>
    /// <exception cref="PythiaQueryException">invalid</exception>
    public void ValidateArgs(LocopContext context, IVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(vocabulary);

        switch (LocopArgs[ARG_OP])
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
    /// Appends the locop function call to the specified SQL builder.
    /// </summary>
    /// <param name="left">The left subquery name.</param>
    /// <param name="right">The right subquery name.</param>
    /// <param name="sql">The target SQL builder.</param>
    /// <exception cref="ArgumentNullException">sql</exception>
    public void AppendLocopFn(string left, string right, StringBuilder sql)
    {
        ArgumentNullException.ThrowIfNull(sql);

        // fn
        int op = (int)LocopArgs[ARG_OP];
        sql.Append(_sqlHelper.GetLexerFnName(op)).Append('(');

        switch (op)
        {
            case pythiaLexer.NEAR:
            case pythiaLexer.NOTNEAR:
            case pythiaLexer.BEFORE:
            case pythiaLexer.NOTBEFORE:
            case pythiaLexer.AFTER:
            case pythiaLexer.NOTAFTER:
            case pythiaLexer.OVERLAPS:
            case pythiaLexer.NOTOVERLAPS:
                // pyt_is_near_within(a1, a2, b1, b2, n, m)
                sql.Append(left).Append(".p1, ")
                   .Append(left).Append(".p2, ")
                   .Append(right).Append(".p1, ")
                   .Append(right).Append(".p2, ")
                   .Append(GetMinArgValue(ARG_N)).Append(", ")
                   .Append(GetMaxArgValue(ARG_M));
                break;
            case pythiaLexer.LALIGN:
            case pythiaLexer.NOTLALIGN:
                // pyt_is_left_aligned(a1, b1, n, m)
                sql.Append(left).Append(".p1, ")
                   .Append(right).Append(".p1, ")
                   .Append(GetMinArgValue(ARG_N)).Append(", ")
                   .Append(GetMaxArgValue(ARG_M));
                break;
            case pythiaLexer.RALIGN:
            case pythiaLexer.NOTRALIGN:
                // pyt_is_right_aligned(a2, b2, n, m)
                sql.Append(left).Append(".p2, ")
                   .Append(right).Append(".p2, ")
                   .Append(GetMinArgValue(ARG_N)).Append(", ")
                   .Append(GetMaxArgValue(ARG_M));
                break;
            case pythiaLexer.INSIDE:
            case pythiaLexer.NOTINSIDE:
                // pyt_is_inside_within(a1, a2, b1, b2, ns, ms, ne, me)
                sql.Append(left).Append(".p1, ")
                   .Append(left).Append(".p2, ")
                   .Append(right).Append(".p1, ")
                   .Append(right).Append(".p2, ")
                   .Append(GetMinArgValue(ARG_NS)).Append(", ")
                   .Append(GetMaxArgValue(ARG_MS)).Append(", ")
                   .Append(GetMinArgValue(ARG_NE)).Append(", ")
                   .Append(GetMaxArgValue(ARG_ME));
                break;
        }

        sql.AppendLine(")");
    }

    private static string MapMaxValue(int m) =>
        m == int.MaxValue ? "MAX" : m.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Appends the locop function call comment.
    /// </summary>
    /// <param name="sql">The target SQL builder.</param>
    /// <param name="noArgs">True to avoid adding arguments.</param>
    /// <exception cref="ArgumentNullException">sql</exception>
    public void AppendLocopFnComment(StringBuilder sql, bool noArgs = false)
    {
        ArgumentNullException.ThrowIfNull(sql);

        sql.Append("-- ");

        // NOT
        if (LocopArgs.TryGetValue(ARG_NOT, out object? not) && (bool)not)
            sql.Append("NOT ");

        // fn
        int op = (int)LocopArgs[ARG_OP];
        sql.Append(_sqlHelper.GetLexerFnName(op));

        // args
        if (!noArgs)
        {
            sql.Append('(');

            switch (op)
            {
                case pythiaLexer.NEAR:
                case pythiaLexer.NOTNEAR:
                case pythiaLexer.BEFORE:
                case pythiaLexer.NOTBEFORE:
                case pythiaLexer.AFTER:
                case pythiaLexer.NOTAFTER:
                case pythiaLexer.OVERLAPS:
                case pythiaLexer.NOTOVERLAPS:
                    // pyt_is_near_within(a1, a2, b1, b2, n, m)
                    sql.Append("a.p1, a.p2, b.p1, b.p2, ")
                       .Append("n=").Append(GetMinArgValue(ARG_N)).Append(", ")
                       .Append("m=").Append(MapMaxValue(GetMaxArgValue(ARG_M)));
                    break;
                case pythiaLexer.LALIGN:
                case pythiaLexer.NOTLALIGN:
                case pythiaLexer.RALIGN:
                case pythiaLexer.NOTRALIGN:
                    // pyt_is_left_aligned(a1, b1, n, m)
                    sql.Append("a.p1, a.p2, b.p1, ")
                       .Append("n=").Append(GetMinArgValue(ARG_N)).Append(", ")
                       .Append("m=").Append(MapMaxValue(GetMaxArgValue(ARG_M)));
                    break;
                case pythiaLexer.INSIDE:
                case pythiaLexer.NOTINSIDE:
                    // pyt_is_inside_within(a1, a2, b1, b2, ns, ms, ne, me)
                    sql.Append("a.p1, a.p2, b.p1, b.p2, ")
                       .Append("ns=").Append(GetMinArgValue(ARG_NS)).Append(", ")
                       .Append("ms=").Append(MapMaxValue(GetMaxArgValue(ARG_MS)))
                       .Append(", ")
                       .Append("ne=").Append(GetMinArgValue(ARG_NE)).Append(", ")
                       .Append("me=").Append(MapMaxValue(GetMaxArgValue(ARG_ME)));
                    break;
            }

            sql.AppendLine(")");
        }
        else
        {
            sql.AppendLine();
        }
    }
}
