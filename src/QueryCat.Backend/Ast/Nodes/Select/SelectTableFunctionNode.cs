using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectTableFunctionNode : ExpressionNode
{
    public FunctionCallNode TableFunction { get; }

    public string Alias { get; set; }

    public List<SelectTableJoinedNode> JoinedNodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "tablefunc";

    /// <inheritdoc />
    public SelectTableFunctionNode(FunctionCallNode tableFunction, string alias)
    {
        TableFunction = tableFunction;
        Alias = alias;
    }

    public SelectTableFunctionNode(FunctionCallNode tableFunction)
    {
        TableFunction = tableFunction;
        Alias = string.Empty;
    }

    public SelectTableFunctionNode(SelectTableFunctionNode node)
        : this((FunctionCallNode)node.TableFunction.Clone(), node.Alias)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return TableFunction;
        foreach (var joinedNode in JoinedNodes)
        {
            yield return joinedNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectTableFunctionNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
