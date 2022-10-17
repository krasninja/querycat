//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.11.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from ../QueryCat.Backend/Parser/QueryCatParser.g4 by ANTLR 4.11.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

namespace QueryCat.Backend.Parser {
 #pragma warning disable 3021 
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete generic visitor for a parse tree produced
/// by <see cref="QueryCatParser"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.11.1")]
[System.CLSCompliant(false)]
public interface IQueryCatParserVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.program"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitProgram([NotNull] QueryCatParser.ProgramContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StatementSelectExpression</c>
	/// labeled alternative in <see cref="QueryCatParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStatementSelectExpression([NotNull] QueryCatParser.StatementSelectExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StatementFunctionCall</c>
	/// labeled alternative in <see cref="QueryCatParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStatementFunctionCall([NotNull] QueryCatParser.StatementFunctionCallContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StatementEcho</c>
	/// labeled alternative in <see cref="QueryCatParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStatementEcho([NotNull] QueryCatParser.StatementEchoContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StatementExpression</c>
	/// labeled alternative in <see cref="QueryCatParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStatementExpression([NotNull] QueryCatParser.StatementExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.functionSignature"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionSignature([NotNull] QueryCatParser.FunctionSignatureContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.functionType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionType([NotNull] QueryCatParser.FunctionTypeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.functionArg"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionArg([NotNull] QueryCatParser.FunctionArgContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectStatement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectStatement([NotNull] QueryCatParser.SelectStatementContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectExpression([NotNull] QueryCatParser.SelectExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectAlias"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectAlias([NotNull] QueryCatParser.SelectAliasContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectQueryFull</c>
	/// labeled alternative in <see cref="QueryCatParser.selectQuery"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectQueryFull([NotNull] QueryCatParser.SelectQueryFullContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectQuerySingle</c>
	/// labeled alternative in <see cref="QueryCatParser.selectQuery"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectQuerySingle([NotNull] QueryCatParser.SelectQuerySingleContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectList([NotNull] QueryCatParser.SelectListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectSetQuantifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectSetQuantifier([NotNull] QueryCatParser.SelectSetQuantifierContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectSublistAll</c>
	/// labeled alternative in <see cref="QueryCatParser.selectSublist"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectSublistAll([NotNull] QueryCatParser.SelectSublistAllContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectSublistExpression</c>
	/// labeled alternative in <see cref="QueryCatParser.selectSublist"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectSublistExpression([NotNull] QueryCatParser.SelectSublistExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectSublistIdentifier</c>
	/// labeled alternative in <see cref="QueryCatParser.selectSublist"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectSublistIdentifier([NotNull] QueryCatParser.SelectSublistIdentifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectTarget"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTarget([NotNull] QueryCatParser.SelectTargetContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectFromClause"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectFromClause([NotNull] QueryCatParser.SelectFromClauseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectTableReferenceList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTableReferenceList([NotNull] QueryCatParser.SelectTableReferenceListContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectTableReferenceNoFormat</c>
	/// labeled alternative in <see cref="QueryCatParser.selectTableReference"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTableReferenceNoFormat([NotNull] QueryCatParser.SelectTableReferenceNoFormatContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectTableReferenceWithFormat</c>
	/// labeled alternative in <see cref="QueryCatParser.selectTableReference"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTableReferenceWithFormat([NotNull] QueryCatParser.SelectTableReferenceWithFormatContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectTableReferenceSubquery</c>
	/// labeled alternative in <see cref="QueryCatParser.selectTableReference"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTableReferenceSubquery([NotNull] QueryCatParser.SelectTableReferenceSubqueryContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectGroupBy"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectGroupBy([NotNull] QueryCatParser.SelectGroupByContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectHaving"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectHaving([NotNull] QueryCatParser.SelectHavingContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectSearchCondition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectSearchCondition([NotNull] QueryCatParser.SelectSearchConditionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectOrderByClause"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectOrderByClause([NotNull] QueryCatParser.SelectOrderByClauseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectSortSpecification"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectSortSpecification([NotNull] QueryCatParser.SelectSortSpecificationContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectOffsetClause"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectOffsetClause([NotNull] QueryCatParser.SelectOffsetClauseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectFetchFirstClause"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectFetchFirstClause([NotNull] QueryCatParser.SelectFetchFirstClauseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.echoStatement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEchoStatement([NotNull] QueryCatParser.EchoStatementContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.functionCall"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionCall([NotNull] QueryCatParser.FunctionCallContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.functionCallArg"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionCallArg([NotNull] QueryCatParser.FunctionCallArgContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>standardFunctionCurrentDate</c>
	/// labeled alternative in <see cref="QueryCatParser.standardFunction"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStandardFunctionCurrentDate([NotNull] QueryCatParser.StandardFunctionCurrentDateContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>standardFunctionCurrentTimestamp</c>
	/// labeled alternative in <see cref="QueryCatParser.standardFunction"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStandardFunctionCurrentTimestamp([NotNull] QueryCatParser.StandardFunctionCurrentTimestampContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.type"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitType([NotNull] QueryCatParser.TypeContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionBinary</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionBinary([NotNull] QueryCatParser.ExpressionBinaryContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionStandardFunctionCall</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionStandardFunctionCall([NotNull] QueryCatParser.ExpressionStandardFunctionCallContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionBetween</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionBetween([NotNull] QueryCatParser.ExpressionBetweenContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionBinaryIn</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionBinaryIn([NotNull] QueryCatParser.ExpressionBinaryInContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionUnary</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionUnary([NotNull] QueryCatParser.ExpressionUnaryContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionInParens</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionInParens([NotNull] QueryCatParser.ExpressionInParensContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionSelect</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionSelect([NotNull] QueryCatParser.ExpressionSelectContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionIdentifier</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionIdentifier([NotNull] QueryCatParser.ExpressionIdentifierContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionLiteral</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionLiteral([NotNull] QueryCatParser.ExpressionLiteralContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionFunctionCall</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionFunctionCall([NotNull] QueryCatParser.ExpressionFunctionCallContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.array"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitArray([NotNull] QueryCatParser.ArrayContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.intervalLiteral"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIntervalLiteral([NotNull] QueryCatParser.IntervalLiteralContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SimpleExpressionLiteral</c>
	/// labeled alternative in <see cref="QueryCatParser.simpleExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleExpressionLiteral([NotNull] QueryCatParser.SimpleExpressionLiteralContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SimpleExpressionBinary</c>
	/// labeled alternative in <see cref="QueryCatParser.simpleExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleExpressionBinary([NotNull] QueryCatParser.SimpleExpressionBinaryContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>literalPlain</c>
	/// labeled alternative in <see cref="QueryCatParser.literal"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLiteralPlain([NotNull] QueryCatParser.LiteralPlainContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>literalInterval</c>
	/// labeled alternative in <see cref="QueryCatParser.literal"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLiteralInterval([NotNull] QueryCatParser.LiteralIntervalContext context);
}
} // namespace QueryCat.Backend.Parser
