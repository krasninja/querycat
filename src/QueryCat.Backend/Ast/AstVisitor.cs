using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;

namespace QueryCat.Backend.Ast;

public abstract class AstVisitor
{
    /// <summary>
    /// Run visitor for the node.
    /// </summary>
    /// <param name="node">Start node.</param>
    public abstract void Run(IAstNode node);

    /// <summary>
    /// Run the visitor.
    /// </summary>
    /// <param name="nodes">Nodes to process.</param>
    public void Run(IEnumerable<IAstNode> nodes)
    {
        foreach (var node in nodes)
        {
            Run(node);
        }
    }

    #region General

    public virtual void Visit(BetweenExpressionNode node)
    {
    }

    public virtual void Visit(BinaryOperationExpressionNode node)
    {
    }

    public virtual void Visit(CastNode node)
    {
    }

    public virtual void Visit(EmptyNode node)
    {
    }

    public virtual void Visit(IdentifierExpressionNode node)
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

    #region Echo

    public virtual void Visit(ExpressionStatementNode node)
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

    #region Select

    public virtual void Visit(SelectAliasNode node)
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

    public virtual void Visit(SelectColumnsSublistNameNode node)
    {
    }

    public virtual void Visit(SelectColumnsSublistNode node)
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

    public virtual void Visit(SelectQuerySpecificationNode node)
    {
    }

    public virtual void Visit(SelectSearchConditionNode node)
    {
    }

    public virtual void Visit(SelectSetQuantifierNode node)
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

    public virtual void Visit(SelectQueryExpressionBodyNode node)
    {
    }

    public virtual void Visit(SelectTableExpressionNode node)
    {
    }

    public virtual void Visit(SelectTableFunctionNode node)
    {
    }

    public virtual void Visit(SelectTableReferenceListNode node)
    {
    }

    #endregion
}
