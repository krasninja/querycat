namespace QueryCat.Backend.Ast.Nodes.Function;

/// <summary>
/// Call a function. Function always has name, arguments list and
/// return value (can be void).
/// </summary>
public class FunctionCallNode : ExpressionNode
{
    public string FunctionName { get; }

    public IList<FunctionCallArgumentNode> Arguments { get; }

    /// <inheritdoc />
    public override string Code => "func";

    /// <inheritdoc />
    public FunctionCallNode(string functionName, List<FunctionCallArgumentNode>? arguments = null)
    {
        FunctionName = functionName;
        Arguments = arguments ?? new List<FunctionCallArgumentNode>();
    }

    public FunctionCallNode(string functionName, params FunctionCallArgumentNode[] arguments)
    {
        FunctionName = functionName;
        Arguments = arguments.ToList();
    }

    public FunctionCallNode(FunctionCallNode node) :
        this(node.FunctionName, node.Arguments.Select(a => (FunctionCallArgumentNode)a.Clone()).ToList())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => Arguments;

    /// <inheritdoc />
    public override object Clone() => new FunctionCallNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() =>
        $"{FunctionName}({string.Join(", ", Arguments.Select(a => a.ToString()))})";
}
