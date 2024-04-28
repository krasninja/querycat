using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Ast.Nodes.Select;

namespace QueryCat.Backend.Ast.Nodes.Update;

internal sealed class UpdateNode : AstNode, ISelectAliasNode
{
    public ExpressionNode TargetExpressionNode { get; }

    public List<UpdateSetNode> SetNodes { get; } = new();

    public SelectSearchConditionNode? SearchConditionNode { get; set; }

    /// <inheritdoc />
    public string Alias { get; set; } = string.Empty;

    /// <inheritdoc />
    public override string Code => "update";

    /// <inheritdoc />
    public UpdateNode(ExpressionNode targetExpressionNode, IEnumerable<UpdateSetNode> setNodes)
    {
        TargetExpressionNode = targetExpressionNode;
        SetNodes.AddRange(setNodes);
    }

    public UpdateNode(UpdateNode node) : this(
        (ExpressionNode)node.TargetExpressionNode.Clone(),
        node.SetNodes.Select(n => (UpdateSetNode)n.Clone()))
    {
        if (node.SearchConditionNode != null)
        {
            SearchConditionNode = (SelectSearchConditionNode)node.SearchConditionNode.Clone();
        }
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new UpdateNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return TargetExpressionNode;
        foreach (var setNode in SetNodes)
        {
            yield return setNode;
        }
        if (SearchConditionNode != null)
        {
            yield return SearchConditionNode;
        }
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
