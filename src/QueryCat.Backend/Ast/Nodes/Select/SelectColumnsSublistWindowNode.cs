using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectColumnsSublistWindowNode : SelectColumnsSublistNode
{
    public FunctionCallNode AggregateFunctionNode { get; }

    public SelectWindowSpecificationNode WindowSpecificationNode { get; internal set; }

    /// <inheritdoc />
    public override string Code => "column_window";

    /// <inheritdoc />
    public SelectColumnsSublistWindowNode(FunctionCallNode aggregateFunctionNode,
        SelectWindowSpecificationNode windowSpecificationNode)
    {
        AggregateFunctionNode = aggregateFunctionNode;
        WindowSpecificationNode = windowSpecificationNode;
    }

    public SelectColumnsSublistWindowNode(SelectColumnsSublistWindowNode node)
        : this(
            (FunctionCallNode)node.AggregateFunctionNode.Clone(),
            (SelectWindowSpecificationNode)node.WindowSpecificationNode.Clone())
    {
        Alias = node.Alias;
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return AggregateFunctionNode;
        yield return WindowSpecificationNode;
    }

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsSublistWindowNode(this);
}
