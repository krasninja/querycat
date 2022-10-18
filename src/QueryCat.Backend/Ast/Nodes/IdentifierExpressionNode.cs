namespace QueryCat.Backend.Ast.Nodes;

/// <summary>
/// Variable identifier.
/// </summary>
public sealed class IdentifierExpressionNode : ExpressionNode
{
    /// <summary>
    /// Identifier name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Optional source name. Should be empty for local variables.
    /// </summary>
    public string SourceName { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public override string Code => "id";

    /// <inheritdoc />
    public IdentifierExpressionNode(string name)
    {
        Name = name;
    }

    /// <inheritdoc />
    public IdentifierExpressionNode(string name, string sourceName) : this(name)
    {
        SourceName = sourceName;
    }

    public IdentifierExpressionNode(IdentifierExpressionNode node) : this(node.Name, node.SourceName)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new IdentifierExpressionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => !string.IsNullOrEmpty(SourceName) ? $"id: {SourceName}.{Name}" : $"id: {Name}";
}
