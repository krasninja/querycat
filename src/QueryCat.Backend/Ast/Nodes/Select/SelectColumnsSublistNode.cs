namespace QueryCat.Backend.Ast.Nodes.Select;

public abstract class SelectColumnsSublistNode : AstNode
{
    public string Alias { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
