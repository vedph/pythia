//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.12.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from pythia.g4 by ANTLR 4.12.0

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace Pythia.Core.Query {
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete generic visitor for a parse tree produced
/// by <see cref="pythiaParser"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.12.0")]
[System.CLSCompliant(false)]
public interface IpythiaVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="pythiaParser.query"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitQuery([NotNull] pythiaParser.QueryContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="pythiaParser.corSet"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCorSet([NotNull] pythiaParser.CorSetContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="pythiaParser.docSet"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDocSet([NotNull] pythiaParser.DocSetContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="pythiaParser.docExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDocExpr([NotNull] pythiaParser.DocExprContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>teLogical</c>
	/// labeled alternative in <see cref="pythiaParser.txtExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTeLogical([NotNull] pythiaParser.TeLogicalContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>teLocation</c>
	/// labeled alternative in <see cref="pythiaParser.txtExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTeLocation([NotNull] pythiaParser.TeLocationContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>teParen</c>
	/// labeled alternative in <see cref="pythiaParser.txtExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTeParen([NotNull] pythiaParser.TeParenContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>tePair</c>
	/// labeled alternative in <see cref="pythiaParser.txtExpr"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTePair([NotNull] pythiaParser.TePairContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="pythiaParser.pair"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPair([NotNull] pythiaParser.PairContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="pythiaParser.spair"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSpair([NotNull] pythiaParser.SpairContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="pythiaParser.tpair"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTpair([NotNull] pythiaParser.TpairContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="pythiaParser.locop"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLocop([NotNull] pythiaParser.LocopContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="pythiaParser.locnArg"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLocnArg([NotNull] pythiaParser.LocnArgContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="pythiaParser.locsArg"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLocsArg([NotNull] pythiaParser.LocsArgContext context);
}
} // namespace Pythia.Core.Query
