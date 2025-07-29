using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Break;
using QueryCat.Backend.Ast.Nodes.Call;
using QueryCat.Backend.Ast.Nodes.Continue;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Ast.Nodes.Delete;
using QueryCat.Backend.Ast.Nodes.For;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.If;
using QueryCat.Backend.Ast.Nodes.Insert;
using QueryCat.Backend.Ast.Nodes.Open;
using QueryCat.Backend.Ast.Nodes.Return;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Ast.Nodes.Update;

namespace QueryCat.Backend.Ast;

/// <summary>
/// The visitor that calls OnVisit abstract method on every node visit.
/// </summary>
internal abstract class DelegateVisitor : AstVisitor
{
    /// <summary>
    /// Callback method.
    /// </summary>
    /// <param name="node">Accepted node.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public abstract ValueTask OnVisitAsync(IAstNode node, CancellationToken cancellationToken);

    /// <inheritdoc />
    public override ValueTask RunAsync(IAstNode node, CancellationToken cancellationToken)
    {
        return AstTraversal.PreOrderAsync(node, cancellationToken);
    }

    #region General

    /// <inheritdoc />
    public override ValueTask VisitAsync(AtTimeZoneNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(BetweenExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(BinaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(BlockExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(CaseExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(CaseWhenThenNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(EmptyNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IdentifierFilterSelectorNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IdentifierIndexSelectorNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IdentifierPropertySelectorNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(InExpressionValuesNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(InOperationExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(LiteralNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(ProgramNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(TernaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(TypeNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(UnaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Call

    /// <inheritdoc />
    public override ValueTask VisitAsync(CallFunctionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(CallFunctionStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallArgumentNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionCallStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionSignatureArgumentNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionSignatureNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionSignatureStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(FunctionTypeNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region If

    /// <inheritdoc />
    public override ValueTask VisitAsync(IfConditionItemNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IfConditionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(IfConditionStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Special functions

    /// <inheritdoc />
    public override ValueTask VisitAsync(CastFunctionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(CoalesceFunctionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Select

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectAliasNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsExceptNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsListNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsSublistAll node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsSublistExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsSublistNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectColumnsSublistWindowNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectIdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectDistinctNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectExistsExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectFetchNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectGroupByNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectHavingNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectOffsetNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectOrderByNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectOrderBySpecificationNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectQueryCombineNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectQuerySpecificationNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectSearchConditionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectSubqueryConditionExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectSubqueryExpressionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableFunctionNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableJoinedOnNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableJoinedTypeNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableJoinedUsingNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableReferenceListNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableValuesNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectTableValuesRowNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectWindowDefinitionListNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectWindowNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectWindowOrderClauseNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectWindowPartitionClauseNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectWindowSpecificationNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectWithListNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SelectWithNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Declare

    /// <inheritdoc />
    public override ValueTask VisitAsync(DeclareNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(DeclareStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SetNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(SetStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Insert

    /// <inheritdoc />
    public override ValueTask VisitAsync(InsertColumnsListNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(InsertNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(InsertStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Delete

    public override ValueTask VisitAsync(DeleteNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    public override ValueTask VisitAsync(DeleteStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Update

    /// <inheritdoc />
    public override ValueTask VisitAsync(UpdateNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(UpdateSetNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(UpdateStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region For

    /// <inheritdoc />
    public override ValueTask VisitAsync(ForNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask VisitAsync(ForStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Continue

    public override ValueTask VisitAsync(ContinueNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    public override ValueTask VisitAsync(ContinueStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Break

    public override ValueTask VisitAsync(BreakNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    public override ValueTask VisitAsync(BreakStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Return

    public override ValueTask VisitAsync(ReturnNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    public override ValueTask VisitAsync(ReturnStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion

    #region Open

    public override ValueTask VisitAsync(OpenNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    public override ValueTask VisitAsync(OpenStatementNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    public override ValueTask VisitAsync(SelectOpenNode node, CancellationToken cancellationToken)
    {
        return OnVisitAsync(node, cancellationToken);
    }

    #endregion
}
