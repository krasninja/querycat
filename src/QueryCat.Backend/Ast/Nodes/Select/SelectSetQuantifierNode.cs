namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectSetQuantifierNode : AstNode
{
    public bool IsDistinct { get; }

    /// <inheritdoc />
    public override string Code => "set_quant";

    public SelectSetQuantifierNode(bool isDistinct)
    {
        IsDistinct = isDistinct;
    }

    public SelectSetQuantifierNode(SelectSetQuantifierNode node) : this(node.IsDistinct)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectSetQuantifierNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
