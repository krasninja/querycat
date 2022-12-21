namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectQueryExpressionCombineNode : AstNode
{
    public SelectQuerySpecificationNode QueryNode { get; }

    public SelectQueryExpressionCombineType CombineType { get; }

    public bool IsDistinct { get; }

    /// <inheritdoc />
    public override string Code => "combine";

    public SelectQueryExpressionCombineNode(
        SelectQuerySpecificationNode queryNode,
        SelectQueryExpressionCombineType combineType,
        bool isDistinct = true)
    {
        QueryNode = queryNode;
        CombineType = combineType;
        IsDistinct = isDistinct;
    }

    public SelectQueryExpressionCombineNode(SelectQueryExpressionCombineNode node)
        : this((SelectQuerySpecificationNode)node.QueryNode.Clone(), node.CombineType, node.IsDistinct)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectQueryExpressionCombineNode(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return QueryNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => $" {CombineType} {(IsDistinct ? "Distinct" : "All")} {QueryNode}";
}
