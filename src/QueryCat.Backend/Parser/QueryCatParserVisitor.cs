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
	/// Visit a parse tree produced by the <c>StatementDeclareVariable</c>
	/// labeled alternative in <see cref="QueryCatParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStatementDeclareVariable([NotNull] QueryCatParser.StatementDeclareVariableContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>StatementSetVariable</c>
	/// labeled alternative in <see cref="QueryCatParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStatementSetVariable([NotNull] QueryCatParser.StatementSetVariableContext context);
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
	/// Visit a parse tree produced by <see cref="QueryCatParser.declareVariable"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDeclareVariable([NotNull] QueryCatParser.DeclareVariableContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.setVariable"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSetVariable([NotNull] QueryCatParser.SetVariableContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectStatement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectStatement([NotNull] QueryCatParser.SelectStatementContext context);
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
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectAlias"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectAlias([NotNull] QueryCatParser.SelectAliasContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectQueryExpressionSimple</c>
	/// labeled alternative in <see cref="QueryCatParser.selectQueryExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectQueryExpressionSimple([NotNull] QueryCatParser.SelectQueryExpressionSimpleContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectQueryExpressionFull</c>
	/// labeled alternative in <see cref="QueryCatParser.selectQueryExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectQueryExpressionFull([NotNull] QueryCatParser.SelectQueryExpressionFullContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectQueryExpressionBodyUnionExcept</c>
	/// labeled alternative in <see cref="QueryCatParser.selectQueryExpressionBody"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectQueryExpressionBodyUnionExcept([NotNull] QueryCatParser.SelectQueryExpressionBodyUnionExceptContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectQueryExpressionBodyIntersect</c>
	/// labeled alternative in <see cref="QueryCatParser.selectQueryExpressionBody"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectQueryExpressionBodyIntersect([NotNull] QueryCatParser.SelectQueryExpressionBodyIntersectContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectQueryExpressionBodyPrimary</c>
	/// labeled alternative in <see cref="QueryCatParser.selectQueryExpressionBody"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectQueryExpressionBodyPrimary([NotNull] QueryCatParser.SelectQueryExpressionBodyPrimaryContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectQueryPrimaryNoParens</c>
	/// labeled alternative in <see cref="QueryCatParser.selectQueryPrimary"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectQueryPrimaryNoParens([NotNull] QueryCatParser.SelectQueryPrimaryNoParensContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectQueryPrimaryParens</c>
	/// labeled alternative in <see cref="QueryCatParser.selectQueryPrimary"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectQueryPrimaryParens([NotNull] QueryCatParser.SelectQueryPrimaryParensContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectQuerySpecificationFull</c>
	/// labeled alternative in <see cref="QueryCatParser.selectQuerySpecification"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectQuerySpecificationFull([NotNull] QueryCatParser.SelectQuerySpecificationFullContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectQuerySpecificationSingle</c>
	/// labeled alternative in <see cref="QueryCatParser.selectQuerySpecification"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectQuerySpecificationSingle([NotNull] QueryCatParser.SelectQuerySpecificationSingleContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectList([NotNull] QueryCatParser.SelectListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectDistinctClause"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectDistinctClause([NotNull] QueryCatParser.SelectDistinctClauseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectDistinctOnClause"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectDistinctOnClause([NotNull] QueryCatParser.SelectDistinctOnClauseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectWithClause"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectWithClause([NotNull] QueryCatParser.SelectWithClauseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectWithElement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectWithElement([NotNull] QueryCatParser.SelectWithElementContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectWithColumnList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectWithColumnList([NotNull] QueryCatParser.SelectWithColumnListContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectSublistAll</c>
	/// labeled alternative in <see cref="QueryCatParser.selectSublist"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectSublistAll([NotNull] QueryCatParser.SelectSublistAllContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectSublistWindow</c>
	/// labeled alternative in <see cref="QueryCatParser.selectSublist"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectSublistWindow([NotNull] QueryCatParser.SelectSublistWindowContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectSublistExpression</c>
	/// labeled alternative in <see cref="QueryCatParser.selectSublist"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectSublistExpression([NotNull] QueryCatParser.SelectSublistExpressionContext context);
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
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectTableReference"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTableReference([NotNull] QueryCatParser.SelectTableReferenceContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectTablePrimaryNoFormat</c>
	/// labeled alternative in <see cref="QueryCatParser.selectTablePrimary"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTablePrimaryNoFormat([NotNull] QueryCatParser.SelectTablePrimaryNoFormatContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectTablePrimaryStdin</c>
	/// labeled alternative in <see cref="QueryCatParser.selectTablePrimary"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTablePrimaryStdin([NotNull] QueryCatParser.SelectTablePrimaryStdinContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectTablePrimaryWithFormat</c>
	/// labeled alternative in <see cref="QueryCatParser.selectTablePrimary"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTablePrimaryWithFormat([NotNull] QueryCatParser.SelectTablePrimaryWithFormatContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectTablePrimarySubquery</c>
	/// labeled alternative in <see cref="QueryCatParser.selectTablePrimary"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTablePrimarySubquery([NotNull] QueryCatParser.SelectTablePrimarySubqueryContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectTablePrimaryIdentifier</c>
	/// labeled alternative in <see cref="QueryCatParser.selectTablePrimary"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTablePrimaryIdentifier([NotNull] QueryCatParser.SelectTablePrimaryIdentifierContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectTableJoinedOn</c>
	/// labeled alternative in <see cref="QueryCatParser.selectTableJoined"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTableJoinedOn([NotNull] QueryCatParser.SelectTableJoinedOnContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SelectTableJoinedUsing</c>
	/// labeled alternative in <see cref="QueryCatParser.selectTableJoined"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTableJoinedUsing([NotNull] QueryCatParser.SelectTableJoinedUsingContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectJoinType"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectJoinType([NotNull] QueryCatParser.SelectJoinTypeContext context);
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
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectWindowSpecification"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectWindowSpecification([NotNull] QueryCatParser.SelectWindowSpecificationContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectWindowPartitionClause"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectWindowPartitionClause([NotNull] QueryCatParser.SelectWindowPartitionClauseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectWindowOrderClause"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectWindowOrderClause([NotNull] QueryCatParser.SelectWindowOrderClauseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectWindow"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectWindow([NotNull] QueryCatParser.SelectWindowContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectWindowDefinitionList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectWindowDefinitionList([NotNull] QueryCatParser.SelectWindowDefinitionListContext context);
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
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectTopClause"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectTopClause([NotNull] QueryCatParser.SelectTopClauseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.selectLimitClause"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectLimitClause([NotNull] QueryCatParser.SelectLimitClauseContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.echoStatement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEchoStatement([NotNull] QueryCatParser.EchoStatementContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>identifierChainFull</c>
	/// labeled alternative in <see cref="QueryCatParser.identifierChain"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdentifierChainFull([NotNull] QueryCatParser.IdentifierChainFullContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>identifierChainSimple</c>
	/// labeled alternative in <see cref="QueryCatParser.identifierChain"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdentifierChainSimple([NotNull] QueryCatParser.IdentifierChainSimpleContext context);
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
	/// Visit a parse tree produced by <see cref="QueryCatParser.castOperand"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCastOperand([NotNull] QueryCatParser.CastOperandContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.caseExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCaseExpression([NotNull] QueryCatParser.CaseExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.caseWhen"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCaseWhen([NotNull] QueryCatParser.CaseWhenContext context);
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
	/// Visit a parse tree produced by the <c>standardFunctionTrim</c>
	/// labeled alternative in <see cref="QueryCatParser.standardFunction"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStandardFunctionTrim([NotNull] QueryCatParser.StandardFunctionTrimContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>standardFunctionPosition</c>
	/// labeled alternative in <see cref="QueryCatParser.standardFunction"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStandardFunctionPosition([NotNull] QueryCatParser.StandardFunctionPositionContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>standardFunctionExtract</c>
	/// labeled alternative in <see cref="QueryCatParser.standardFunction"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStandardFunctionExtract([NotNull] QueryCatParser.StandardFunctionExtractContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>standardFunctionCoalesce</c>
	/// labeled alternative in <see cref="QueryCatParser.standardFunction"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStandardFunctionCoalesce([NotNull] QueryCatParser.StandardFunctionCoalesceContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="QueryCatParser.dateTimeField"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDateTimeField([NotNull] QueryCatParser.DateTimeFieldContext context);
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
	/// Visit a parse tree produced by the <c>ExpressionInParens</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionInParens([NotNull] QueryCatParser.ExpressionInParensContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionCase</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionCase([NotNull] QueryCatParser.ExpressionCaseContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionCast</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionCast([NotNull] QueryCatParser.ExpressionCastContext context);
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
	/// Visit a parse tree produced by the <c>ExpressionSubquery</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionSubquery([NotNull] QueryCatParser.ExpressionSubqueryContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>ExpressionBinaryCast</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionBinaryCast([NotNull] QueryCatParser.ExpressionBinaryCastContext context);
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
	/// Visit a parse tree produced by the <c>ExpressionExists</c>
	/// labeled alternative in <see cref="QueryCatParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionExists([NotNull] QueryCatParser.ExpressionExistsContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SimpleExpressionCase</c>
	/// labeled alternative in <see cref="QueryCatParser.simpleExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleExpressionCase([NotNull] QueryCatParser.SimpleExpressionCaseContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SimpleExpressionLiteral</c>
	/// labeled alternative in <see cref="QueryCatParser.simpleExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleExpressionLiteral([NotNull] QueryCatParser.SimpleExpressionLiteralContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SimpleExpressionCast</c>
	/// labeled alternative in <see cref="QueryCatParser.simpleExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleExpressionCast([NotNull] QueryCatParser.SimpleExpressionCastContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SimpleExpressionStandardFunctionCall</c>
	/// labeled alternative in <see cref="QueryCatParser.simpleExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleExpressionStandardFunctionCall([NotNull] QueryCatParser.SimpleExpressionStandardFunctionCallContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SimpleExpressionUnary</c>
	/// labeled alternative in <see cref="QueryCatParser.simpleExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleExpressionUnary([NotNull] QueryCatParser.SimpleExpressionUnaryContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SimpleExpressionBinary</c>
	/// labeled alternative in <see cref="QueryCatParser.simpleExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleExpressionBinary([NotNull] QueryCatParser.SimpleExpressionBinaryContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SimpleExpressionBinaryCast</c>
	/// labeled alternative in <see cref="QueryCatParser.simpleExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleExpressionBinaryCast([NotNull] QueryCatParser.SimpleExpressionBinaryCastContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SimpleExpressionFunctionCall</c>
	/// labeled alternative in <see cref="QueryCatParser.simpleExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleExpressionFunctionCall([NotNull] QueryCatParser.SimpleExpressionFunctionCallContext context);
	/// <summary>
	/// Visit a parse tree produced by the <c>SimpleExpressionIdentifier</c>
	/// labeled alternative in <see cref="QueryCatParser.simpleExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSimpleExpressionIdentifier([NotNull] QueryCatParser.SimpleExpressionIdentifierContext context);
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
