namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectTableReferenceListNode : AstNode
{
    public IList<ExpressionNode> TableFunctions { get; }

    /// <inheritdoc />
    public override string Code => "from_list";

    public SelectTableReferenceListNode(IList<ExpressionNode> tableFunctions)
    {
        TableFunctions = tableFunctions;
    }

    public SelectTableReferenceListNode(SelectTableReferenceListNode node) :
        this(node.TableFunctions.Select(tf => (ExpressionNode)tf.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => TableFunctions;

    /// <inheritdoc />
    public override object Clone() => new SelectTableReferenceListNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
