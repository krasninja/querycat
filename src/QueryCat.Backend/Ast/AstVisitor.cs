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
    public virtual void Run(IAstNode node)
    {
        AstTraversal.PreOrder(node);
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
    public void Run(IEnumerable<IAstNode?> nodes)
    {
        foreach (var node in nodes)
        {
            if (node != null)
            {
                Run(node);
            }
        }
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
    /// <returns>Instance of <see cref="IFuncUnit" />.</returns>
    public virtual IFuncUnit RunAndReturn(IAstNode node) => EmptyFuncUnit.Instance;

    /// <summary>
    /// Run the visitor and return the result as <see cref="IFuncUnit" />.
    /// </summary>
    /// <param name="node">Start node.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="IFuncUnit" />.</returns>
    public virtual ValueTask<IFuncUnit> RunAndReturnAsync(IAstNode node, CancellationToken cancellationToken)
        => ValueTask.FromResult((IFuncUnit)EmptyFuncUnit.Instance);

    #region General

    public virtual void Visit(AtTimeZoneNode node)
    {
    }

    public virtual ValueTask VisitAsync(AtTimeZoneNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(BetweenExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(BetweenExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(BinaryOperationExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(BinaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(BlockExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(BlockExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(CaseExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(CaseExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(CaseWhenThenNode node)
    {
    }

    public virtual ValueTask VisitAsync(CaseWhenThenNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(EmptyNode node)
    {
    }

    public virtual ValueTask VisitAsync(EmptyNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(ExpressionStatementNode node)
    {
    }

    public virtual ValueTask VisitAsync(ExpressionStatementNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(IdentifierExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(IdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(IdentifierFilterSelectorNode node)
    {
    }

    public virtual ValueTask VisitAsync(IdentifierFilterSelectorNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(IdentifierIndexSelectorNode node)
    {
    }

    public virtual ValueTask VisitAsync(IdentifierIndexSelectorNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(IdentifierPropertySelectorNode node)
    {
    }

    public virtual ValueTask VisitAsync(IdentifierPropertySelectorNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(InOperationExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(InOperationExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(InExpressionValuesNode node)
    {
    }

    public virtual ValueTask VisitAsync(InExpressionValuesNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(LiteralNode node)
    {
    }

    public virtual ValueTask VisitAsync(LiteralNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(ProgramNode node)
    {
    }

    public virtual ValueTask VisitAsync(ProgramNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(TernaryOperationExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(TernaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(TypeNode node)
    {
    }

    public virtual ValueTask VisitAsync(TypeNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(UnaryOperationExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(UnaryOperationExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Call

    public virtual void Visit(CallFunctionNode node)
    {
    }

    public virtual ValueTask VisitAsync(CallFunctionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(CallFunctionStatementNode node)
    {
    }

    public virtual ValueTask VisitAsync(CallFunctionStatementNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Function

    public virtual void Visit(FunctionCallArgumentNode node)
    {
    }

    public virtual ValueTask VisitAsync(FunctionCallArgumentNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(FunctionCallExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(FunctionCallExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(FunctionCallNode node)
    {
    }

    public virtual ValueTask VisitAsync(FunctionCallNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(FunctionCallStatementNode node)
    {
    }

    public virtual ValueTask VisitAsync(FunctionCallStatementNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(FunctionSignatureArgumentNode node)
    {
    }

    public virtual ValueTask VisitAsync(FunctionSignatureArgumentNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(FunctionSignatureNode node)
    {
    }

    public virtual ValueTask VisitAsync(FunctionSignatureNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(FunctionSignatureStatementNode node)
    {
    }

    public virtual ValueTask VisitAsync(FunctionSignatureStatementNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(FunctionTypeNode node)
    {
    }

    public virtual ValueTask VisitAsync(FunctionTypeNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region If

    public virtual void Visit(IfConditionItemNode node)
    {
    }

    public virtual ValueTask VisitAsync(IfConditionItemNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(IfConditionNode node)
    {
    }

    public virtual ValueTask VisitAsync(IfConditionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(IfConditionStatementNode node)
    {
    }

    public virtual ValueTask VisitAsync(IfConditionStatementNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Special functions

    public virtual void Visit(CastFunctionNode node)
    {
    }

    public virtual ValueTask VisitAsync(CastFunctionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(CoalesceFunctionNode node)
    {
    }

    public virtual ValueTask VisitAsync(CoalesceFunctionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Select

    public virtual void Visit(SelectAliasNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectAliasNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectColumnsExceptNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectColumnsExceptNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectColumnsListNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectColumnsListNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectColumnsSublistAll node)
    {
    }

    public virtual ValueTask VisitAsync(SelectColumnsSublistAll node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectColumnsSublistExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectColumnsSublistExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectColumnsSublistNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectColumnsSublistNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectColumnsSublistWindowNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectColumnsSublistWindowNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectIdentifierExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectIdentifierExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectDistinctNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectDistinctNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectExistsExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectExistsExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectFetchNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectFetchNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectGroupByNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectGroupByNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectHavingNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectHavingNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectOffsetNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectOffsetNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectOrderByNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectOrderByNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectOrderBySpecificationNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectOrderBySpecificationNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectQueryCombineNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectQueryCombineNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectQueryNode node)
    {
        if (node is SelectQueryCombineNode combineNode)
        {
            Visit(combineNode);
        }
        else if (node is SelectQuerySpecificationNode specificationNode)
        {
            Visit(specificationNode);
        }
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

    public virtual void Visit(SelectQuerySpecificationNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectQuerySpecificationNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectSearchConditionNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectSearchConditionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectStatementNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectStatementNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectSubqueryConditionExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectSubqueryConditionExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectSubqueryExpressionNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectSubqueryExpressionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectTableFunctionNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectTableFunctionNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectTableJoinedOnNode onNode)
    {
    }

    public virtual ValueTask VisitAsync(SelectTableJoinedOnNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectTableJoinedTypeNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectTableJoinedTypeNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectTableJoinedUsingNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectTableJoinedUsingNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectTableNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectTableNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectTableReferenceListNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectTableReferenceListNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectTableValuesNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectTableValuesNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectTableValuesRowNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectTableValuesRowNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectWindowDefinitionListNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectWindowDefinitionListNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectWindowNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectWindowNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectWindowOrderClauseNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectWindowOrderClauseNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectWindowPartitionClauseNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectWindowPartitionClauseNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectWindowSpecificationNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectWindowSpecificationNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectWithListNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectWithListNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SelectWithNode node)
    {
    }

    public virtual ValueTask VisitAsync(SelectWithNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Declare/Set

    public virtual void Visit(DeclareNode node)
    {
    }

    public virtual ValueTask VisitAsync(DeclareNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(DeclareStatementNode node)
    {
    }

    public virtual ValueTask VisitAsync(DeclareStatementNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SetNode node)
    {
    }

    public virtual ValueTask VisitAsync(SetNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(SetStatementNode node)
    {
    }

    public virtual ValueTask VisitAsync(SetStatementNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Insert

    public virtual void Visit(InsertColumnsListNode node)
    {
    }

    public virtual ValueTask VisitAsync(InsertColumnsListNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(InsertNode node)
    {
    }

    public virtual ValueTask VisitAsync(InsertNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(InsertStatementNode node)
    {
    }

    public virtual ValueTask VisitAsync(InsertStatementNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Update

    public virtual void Visit(UpdateNode node)
    {
    }

    public virtual ValueTask VisitAsync(UpdateNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(UpdateSetNode node)
    {
    }

    public virtual ValueTask VisitAsync(UpdateSetNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    public virtual void Visit(UpdateStatementNode node)
    {
    }

    public virtual ValueTask VisitAsync(UpdateStatementNode node, CancellationToken cancellationToken)
    {
        Visit(node);
        return ValueTask.CompletedTask;
    }

    #endregion
}
