using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Call;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.If;
using QueryCat.Backend.Ast.Nodes.Insert;
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
    public abstract void OnVisit(IAstNode node);

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        AstTraversal.PreOrder(node);
    }

    #region General

    /// <inheritdoc />
    public override void Visit(AtTimeZoneNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(BetweenExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(BinaryOperationExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(BlockExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(CaseExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(CaseWhenThenNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(EmptyNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(IdentifierExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(IdentifierFilterSelectorNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(IdentifierIndexSelectorNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(IdentifierPropertySelectorNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(InExpressionValuesNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(InOperationExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(LiteralNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(ProgramNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(TernaryOperationExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(TypeNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(UnaryOperationExpressionNode node)
    {
        OnVisit(node);
    }

    #endregion

    #region Call

    /// <inheritdoc />
    public override void Visit(CallFunctionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(CallFunctionStatementNode node)
    {
        OnVisit(node);
    }

    #endregion

    #region Function

    /// <inheritdoc />
    public override void Visit(FunctionCallArgumentNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallStatementNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(FunctionSignatureArgumentNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(FunctionSignatureNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(FunctionSignatureStatementNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(FunctionTypeNode node)
    {
        OnVisit(node);
    }

    #endregion

    #region If

    /// <inheritdoc />
    public override void Visit(IfConditionItemNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(IfConditionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(IfConditionStatementNode node)
    {
        OnVisit(node);
    }

    #endregion

    #region Special functions

    /// <inheritdoc />
    public override void Visit(CastFunctionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(CoalesceFunctionNode node)
    {
        OnVisit(node);
    }

    #endregion

    #region Select

    /// <inheritdoc />
    public override void Visit(SelectAliasNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsExceptNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsListNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistAll node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistWindowNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectIdentifierExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectDistinctNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectExistsExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectFetchNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectGroupByNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectHavingNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectOffsetNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectOrderByNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectOrderBySpecificationNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectQueryCombineNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectSearchConditionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectStatementNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectSubqueryConditionExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectSubqueryExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableFunctionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableJoinedOnNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableJoinedTypeNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableJoinedUsingNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableReferenceListNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableValuesNode valuesNode)
    {
        OnVisit(valuesNode);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableValuesRowNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectWindowDefinitionListNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectWindowNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectWindowOrderClauseNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectWindowPartitionClauseNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectWindowSpecificationNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectWithListNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectWithNode node)
    {
        OnVisit(node);
    }

    #endregion

    #region Declare

    /// <inheritdoc />
    public override void Visit(DeclareNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(DeclareStatementNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SetNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SetStatementNode node)
    {
        OnVisit(node);
    }

    #endregion

    #region Insert

    /// <inheritdoc />
    public override void Visit(InsertColumnsListNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(InsertNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(InsertStatementNode node)
    {
        OnVisit(node);
    }

    #endregion

    #region Update

    /// <inheritdoc />
    public override void Visit(UpdateNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(UpdateSetNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(UpdateStatementNode node)
    {
        OnVisit(node);
    }

    #endregion
}
