using System.Text;
using QueryCat.Backend.Ast.Nodes.Function;

namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectTableFunctionNode : ExpressionNode, ISelectAliasNode
{
    public FunctionCallNode TableFunctionNode { get; }

    /// <inheritdoc />
    public string Alias { get; set; }

    public List<SelectTableJoinedNode> JoinedNodes { get; } = new();

    /// <inheritdoc />
    public override string Code => "tablefunc";

    /// <inheritdoc />
    public SelectTableFunctionNode(FunctionCallNode tableFunctionNode, string alias)
    {
        TableFunctionNode = tableFunctionNode;
        Alias = alias;
    }

    public SelectTableFunctionNode(FunctionCallNode tableFunctionNode)
    {
        TableFunctionNode = tableFunctionNode;
        Alias = string.Empty;
    }

    public SelectTableFunctionNode(SelectTableFunctionNode node)
        : this((FunctionCallNode)node.TableFunctionNode.Clone(), node.Alias)
    {
        JoinedNodes.AddRange(node.JoinedNodes.Select(n => (SelectTableJoinedNode)n.Clone()));
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return TableFunctionNode;
        foreach (var joinedNode in JoinedNodes)
        {
            yield return joinedNode;
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectTableFunctionNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder()
            .Append(TableFunctionNode);
        if (!string.IsNullOrEmpty(Alias))
        {
            sb.Append(" As " + Alias);
        }
        foreach (var joinedNode in JoinedNodes)
        {
            sb.Append($" {joinedNode}");
        }
        return sb.ToString();
    }
}
