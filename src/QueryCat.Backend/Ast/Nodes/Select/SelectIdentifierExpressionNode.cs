using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Select;

/// <summary>
/// The special node is used to distinguish between identifier and
/// another identifier that can be only used for CTE expression.
/// </summary>
public sealed class SelectIdentifierExpressionNode : IdentifierExpressionNode, ISelectAliasNode
{
    /// <inheritdoc />
    public string Alias { get; set; } = string.Empty;

    /// <inheritdoc />
    public override string Code => "cte_or_id";

    /// <summary>
    /// Optional formatter.
    /// </summary>
    public FunctionCallNode? Format { get; set; }

    /// <inheritdoc />
    public SelectIdentifierExpressionNode(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public SelectIdentifierExpressionNode(string name, string sourceName) : base(name, sourceName)
    {
    }

    /// <inheritdoc />
    public SelectIdentifierExpressionNode(IdentifierExpressionNode node) : base(node)
    {
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override object Clone() => new SelectIdentifierExpressionNode(this);
}
