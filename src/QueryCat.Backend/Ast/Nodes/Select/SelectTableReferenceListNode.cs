namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectTableReferenceListNode : AstNode
{
    public List<ExpressionNode> TableFunctionsNodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "from_list";

    public SelectTableReferenceListNode(IEnumerable<ExpressionNode> tableFunctionsNodes)
    {
        TableFunctionsNodes.AddRange(tableFunctionsNodes);
    }

    public SelectTableReferenceListNode(params ExpressionNode[] tableFunctionsNodes)
    {
        TableFunctionsNodes.AddRange(tableFunctionsNodes);
    }

    public SelectTableReferenceListNode(SelectTableReferenceListNode node) :
        this(node.TableFunctionsNodes.Select(tf => (ExpressionNode)tf.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => TableFunctionsNodes;

    /// <inheritdoc />
    public override object Clone() => new SelectTableReferenceListNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => string.Join(", ", TableFunctionsNodes.Select(tf => tf.ToString()));
}
