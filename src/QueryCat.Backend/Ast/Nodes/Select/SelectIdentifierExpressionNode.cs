using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Select;

/// <summary>
/// The special node is used to distinguish between identifier and
/// another identifier that can be only used for CTE expression.
/// </summary>
internal sealed class SelectIdentifierExpressionNode : IdentifierExpressionNode, ISelectAliasNode
{
    /// <inheritdoc />
    public string Alias { get; set; }

    /// <inheritdoc />
    public override string Code => "cte_or_id";

    /// <summary>
    /// Optional formatter.
    /// </summary>
    public FunctionCallNode? Format { get; set; }

    public List<SelectTableJoinedNode> JoinedNodes { get; } = new();

    /// <inheritdoc />
    public SelectIdentifierExpressionNode(IdentifierExpressionNode identifierExpressionNode, string alias)
        : base(identifierExpressionNode)
    {
        Alias = !string.IsNullOrEmpty(alias) ? alias : identifierExpressionNode.Name;
    }

    /// <inheritdoc />
    public SelectIdentifierExpressionNode(SelectIdentifierExpressionNode node) : base((SelectIdentifierExpressionNode)node.Clone())
    {
        Alias = node.Alias;
        if (node.Format != null)
        {
            Format = (FunctionCallNode)node.Format.Clone();
        }
        JoinedNodes = node.JoinedNodes.Select(jn => (SelectTableJoinedNode)jn.Clone()).ToList();
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        foreach (var astNode in base.GetChildren())
        {
            yield return astNode;
        }
        if (Format != null)
        {
            yield return Format;
        }
        foreach (var selectTableJoinedNode in JoinedNodes)
        {
            yield return selectTableJoinedNode;
        }
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override object Clone() => new SelectIdentifierExpressionNode(this);
}
