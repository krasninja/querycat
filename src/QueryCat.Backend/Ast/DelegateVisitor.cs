using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Ast.Nodes.SpecialFunctions;

namespace QueryCat.Backend.Ast;

/// <summary>
/// The visitor that calls OnVisit abstract method on every node visit.
/// </summary>
public abstract class DelegateVisitor : AstVisitor
{
    private readonly AstTraversal _astTraversal;

    public AstTraversal AstTraversal => _astTraversal;

    protected DelegateVisitor()
    {
        _astTraversal = new AstTraversal(this);
    }

    /// <summary>
    /// Callback method.
    /// </summary>
    /// <param name="node">Accepted node.</param>
    public abstract void OnVisit(IAstNode node);

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        _astTraversal.PreOrder(node);
    }

    #region General

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

    #region Echo

    /// <inheritdoc />
    public override void Visit(ExpressionStatementNode node)
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
    public override void Visit(SelectColumnsSublistNameNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectColumnsSublistNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectCteIdentifierExpressionNode node)
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
    public override void Visit(SelectTableExpressionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableFunctionNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableJoinedNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableJoinedTypeNode node)
    {
        OnVisit(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableReferenceListNode node)
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
}
