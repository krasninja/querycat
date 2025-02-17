namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectTableReferenceListNode : AstNode
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
        this(node.TableFunctionsNodes.Select(tf => (ExpressionNode)tf.Clone()).ToArray())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => TableFunctionsNodes;

    /// <inheritdoc />
    public override object Clone() => new SelectTableReferenceListNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => string.Join(", ", TableFunctionsNodes.Select(tf => tf.ToString()));
}
