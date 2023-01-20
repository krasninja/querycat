using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectColumnsSublistWindowNode : SelectColumnsSublistNode
{
    public FunctionCallNode AggregateFunctionNode { get; }

    public SelectWindowSpecificationNode WindowSpecificationNode { get; }

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
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return AggregateFunctionNode;
        yield return WindowSpecificationNode;
    }

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsSublistWindowNode(this);
}
