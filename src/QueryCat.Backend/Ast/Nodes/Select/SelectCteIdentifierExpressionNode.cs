namespace QueryCat.Backend.Ast.Nodes.Select;

/// <summary>
/// The special node is used to distinguish between identifier and
/// another identifier that can be only used for CTE expression.
/// </summary>
public sealed class SelectCteIdentifierExpressionNode : IdentifierExpressionNode, ISelectAliasNode
{
    /// <inheritdoc />
    public string Alias { get; set; } = string.Empty;

    /// <inheritdoc />
    public override string Code => "cte_id";

    /// <inheritdoc />
    public SelectCteIdentifierExpressionNode(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public SelectCteIdentifierExpressionNode(string name, string sourceName) : base(name, sourceName)
    {
    }

    /// <inheritdoc />
    public SelectCteIdentifierExpressionNode(IdentifierExpressionNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override object Clone() => new SelectCteIdentifierExpressionNode(this);
}
