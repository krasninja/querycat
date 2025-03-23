using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Call;
using QueryCat.Backend.Ast.Nodes.Declare;
using QueryCat.Backend.Ast.Nodes.Delete;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.If;
using QueryCat.Backend.Ast.Nodes.Insert;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;
using QueryCat.Backend.Ast.Nodes.Update;

namespace QueryCat.Backend.Ast;

internal abstract class AstVisitor
{
    /// <summary>
    /// AST traversal.
    /// </summary>
    internal AstTraversal AstTraversal { get; }

    public AstVisitor()
    {
        AstTraversal = new AstTraversal(this);
    }

    /// <summary>
    /// Run visitor for the node.
    /// </summary>
    /// <param name="node">Start node.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public virtual ValueTask RunAsync(IAstNode node, CancellationToken cancellationToken)
    {
        return AstTraversal.PreOrderAsync(node, cancellationToken);
    }

    /// <summary>
    /// Run the visitor.
    /// </summary>
    /// <param name="nodes">Nodes to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask RunAsync(IEnumerable<IAstNode?> nodes, CancellationToken cancellationToken)
    {
        foreach (var node in nodes)
        {
            if (node != null)
            {
                await RunAsync(node, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Run the visitor and return the result as <see cref="IFuncUnit" />.
    /// </summary>
    /// <param name="node">Start node.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="IFuncUnit" />.</returns>
    public virtual ValueTask<IFuncUnit> RunAndReturnAsync(IAstNode node, CancellationToken cancellationToken)
        => ValueTask.FromResult((IFuncUnit)EmptyFuncUnit.Instance);

    #region General

    public virtual ValueTask VisitAsync(AtTimeZoneNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(BetweenExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(BinaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(BlockExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(CaseExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(CaseWhenThenNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(EmptyNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(ExpressionStatementNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(IdentifierFilterSelectorNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(IdentifierIndexSelectorNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(IdentifierPropertySelectorNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(InOperationExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(InExpressionValuesNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(LiteralNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(ProgramNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(TernaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(TypeNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(UnaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Call

    public virtual ValueTask VisitAsync(CallFunctionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(CallFunctionStatementNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Function

    public virtual ValueTask VisitAsync(FunctionCallArgumentNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(FunctionCallExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(FunctionCallStatementNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(FunctionSignatureArgumentNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(FunctionSignatureNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(FunctionSignatureStatementNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(FunctionTypeNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region If

    public virtual ValueTask VisitAsync(IfConditionItemNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(IfConditionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(IfConditionStatementNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Special functions

    public virtual ValueTask VisitAsync(CastFunctionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(CoalesceFunctionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Select

    public virtual ValueTask VisitAsync(SelectAliasNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectColumnsExceptNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectColumnsListNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectColumnsSublistAll node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectColumnsSublistExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectColumnsSublistNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectColumnsSublistWindowNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectIdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectDistinctNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectExistsExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectFetchNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectGroupByNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectHavingNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectOffsetNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectOrderByNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectOrderBySpecificationNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectQueryCombineNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual async ValueTask VisitAsync(SelectQueryNode node, CancellationToken cancellationToken)
    {
        if (node is SelectQueryCombineNode combineNode)
        {
            await VisitAsync(combineNode, cancellationToken);
        }
        else if (node is SelectQuerySpecificationNode specificationNode)
        {
            await VisitAsync(specificationNode, cancellationToken);
        }
    }

    public virtual ValueTask VisitAsync(SelectQuerySpecificationNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectSearchConditionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectStatementNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectSubqueryConditionExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectSubqueryExpressionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectTableFunctionNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectTableJoinedOnNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectTableJoinedTypeNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectTableJoinedUsingNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectTableNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectTableReferenceListNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectTableValuesNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectTableValuesRowNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectWindowDefinitionListNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectWindowNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectWindowOrderClauseNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectWindowPartitionClauseNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectWindowSpecificationNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectWithListNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SelectWithNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Declare/Set

    public virtual ValueTask VisitAsync(DeclareNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(DeclareStatementNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SetNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(SetStatementNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Delete

    public virtual ValueTask VisitAsync(DeleteNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(DeleteStatementNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Insert

    public virtual ValueTask VisitAsync(InsertColumnsListNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(InsertNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(InsertStatementNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Update

    public virtual ValueTask VisitAsync(UpdateNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(UpdateSetNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask VisitAsync(UpdateStatementNode node, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    #endregion
}
