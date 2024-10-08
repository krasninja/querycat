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
    /// Run the visitor and return the result as <see cref="IFuncUnit" />.
    /// </summary>
    /// <param name="node">Start node.</param>
    /// <returns>Instance of <see cref="IFuncUnit" />.</returns>
    public virtual IFuncUnit RunAndReturn(IAstNode node) => EmptyFuncUnit.Instance;

    #region General

    public virtual void Visit(AtTimeZoneNode node)
    {
    }

    public virtual void Visit(BetweenExpressionNode node)
    {
    }

    public virtual void Visit(BinaryOperationExpressionNode node)
    {
    }

    public virtual void Visit(BlockExpressionNode node)
    {
    }

    public virtual void Visit(CaseExpressionNode node)
    {
    }

    public virtual void Visit(CaseWhenThenNode node)
    {
    }

    public virtual void Visit(EmptyNode node)
    {
    }

    public virtual void Visit(ExpressionStatementNode node)
    {
    }

    public virtual void Visit(IdentifierExpressionNode node)
    {
    }

    public virtual void Visit(IdentifierFilterSelectorNode node)
    {
    }

    public virtual void Visit(IdentifierIndexSelectorNode node)
    {
    }

    public virtual void Visit(IdentifierPropertySelectorNode node)
    {
    }

    public virtual void Visit(InOperationExpressionNode node)
    {
    }

    public virtual void Visit(InExpressionValuesNode node)
    {
    }

    public virtual void Visit(LiteralNode node)
    {
    }

    public virtual void Visit(ProgramNode node)
    {
    }

    public virtual void Visit(TernaryOperationExpressionNode node)
    {
    }

    public virtual void Visit(TypeNode node)
    {
    }

    public virtual void Visit(UnaryOperationExpressionNode node)
    {
    }

    #endregion

    #region Call

    public virtual void Visit(CallFunctionNode node)
    {
    }

    public virtual void Visit(CallFunctionStatementNode node)
    {
    }

    #endregion

    #region Function

    public virtual void Visit(FunctionCallArgumentNode node)
    {
    }

    public virtual void Visit(FunctionCallExpressionNode node)
    {
    }

    public virtual void Visit(FunctionCallNode node)
    {
    }

    public virtual void Visit(FunctionCallStatementNode node)
    {
    }

    public virtual void Visit(FunctionSignatureArgumentNode node)
    {
    }

    public virtual void Visit(FunctionSignatureNode node)
    {
    }

    public virtual void Visit(FunctionSignatureStatementNode node)
    {
    }

    public virtual void Visit(FunctionTypeNode node)
    {
    }

    #endregion

    #region If

    public virtual void Visit(IfConditionItemNode node)
    {
    }

    public virtual void Visit(IfConditionNode node)
    {
    }

    public virtual void Visit(IfConditionStatementNode node)
    {
    }

    #endregion

    #region Special functions

    public virtual void Visit(CastFunctionNode node)
    {
    }

    public virtual void Visit(CoalesceFunctionNode node)
    {
    }

    #endregion

    #region Select

    public virtual void Visit(SelectAliasNode node)
    {
    }

    public virtual void Visit(SelectColumnsExceptNode node)
    {
    }

    public virtual void Visit(SelectColumnsListNode node)
    {
    }

    public virtual void Visit(SelectColumnsSublistAll node)
    {
    }

    public virtual void Visit(SelectColumnsSublistExpressionNode node)
    {
    }

    public virtual void Visit(SelectColumnsSublistNode node)
    {
    }

    public virtual void Visit(SelectColumnsSublistWindowNode node)
    {
    }

    public virtual void Visit(SelectIdentifierExpressionNode node)
    {
    }

    public virtual void Visit(SelectDistinctNode node)
    {
    }

    public virtual void Visit(SelectExistsExpressionNode node)
    {
    }

    public virtual void Visit(SelectFetchNode node)
    {
    }

    public virtual void Visit(SelectGroupByNode node)
    {
    }

    public virtual void Visit(SelectHavingNode node)
    {
    }

    public virtual void Visit(SelectOffsetNode node)
    {
    }

    public virtual void Visit(SelectOrderByNode node)
    {
    }

    public virtual void Visit(SelectOrderBySpecificationNode node)
    {
    }

    public virtual void Visit(SelectQueryCombineNode node)
    {
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

    public virtual void Visit(SelectQuerySpecificationNode node)
    {
    }

    public virtual void Visit(SelectSearchConditionNode node)
    {
    }

    public virtual void Visit(SelectStatementNode node)
    {
    }

    public virtual void Visit(SelectSubqueryConditionExpressionNode node)
    {
    }

    public virtual void Visit(SelectSubqueryExpressionNode node)
    {
    }

    public virtual void Visit(SelectTableFunctionNode node)
    {
    }

    public virtual void Visit(SelectTableJoinedOnNode onNode)
    {
    }

    public virtual void Visit(SelectTableJoinedTypeNode node)
    {
    }

    public virtual void Visit(SelectTableJoinedUsingNode node)
    {
    }

    public virtual void Visit(SelectTableNode node)
    {
    }

    public virtual void Visit(SelectTableReferenceListNode node)
    {
    }

    public virtual void Visit(SelectTableValuesNode node)
    {
    }

    public virtual void Visit(SelectTableValuesRowNode node)
    {
    }

    public virtual void Visit(SelectWindowDefinitionListNode node)
    {
    }

    public virtual void Visit(SelectWindowNode node)
    {
    }

    public virtual void Visit(SelectWindowOrderClauseNode node)
    {
    }

    public virtual void Visit(SelectWindowPartitionClauseNode node)
    {
    }

    public virtual void Visit(SelectWindowSpecificationNode node)
    {
    }

    public virtual void Visit(SelectWithListNode node)
    {
    }

    public virtual void Visit(SelectWithNode node)
    {
    }

    #endregion

    #region Declare/Set

    public virtual void Visit(DeclareNode node)
    {
    }

    public virtual void Visit(DeclareStatementNode node)
    {
    }

    public virtual void Visit(SetNode node)
    {
    }

    public virtual void Visit(SetStatementNode node)
    {
    }

    #endregion

    #region Insert

    public virtual void Visit(InsertColumnsListNode node)
    {
    }

    public virtual void Visit(InsertNode node)
    {
    }

    public virtual void Visit(InsertStatementNode node)
    {
    }

    #endregion

    #region Update

    public virtual void Visit(UpdateNode node)
    {
    }

    public virtual void Visit(UpdateSetNode node)
    {
    }

    public virtual void Visit(UpdateStatementNode node)
    {
    }

    #endregion
}
