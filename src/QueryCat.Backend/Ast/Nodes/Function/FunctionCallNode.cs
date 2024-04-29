namespace QueryCat.Backend.Ast.Nodes.Function;

/// <summary>
/// Call a function. Function always has name, arguments list and
/// return value (can be void).
/// </summary>
internal sealed class FunctionCallNode : ExpressionNode
{
    public string FunctionName { get; }

    public List<FunctionCallArgumentNode> Arguments { get; } = new();

    /// <inheritdoc />
    public override string Code => "func";

    /// <inheritdoc />
    public FunctionCallNode(string functionName, IEnumerable<FunctionCallArgumentNode>? arguments = null)
    {
        FunctionName = functionName;
        if (arguments != null)
        {
            Arguments.AddRange(arguments);
        }
    }

    public FunctionCallNode(string functionName, params FunctionCallArgumentNode[] arguments)
    {
        FunctionName = functionName;
        Arguments.AddRange(arguments);
    }

    public FunctionCallNode(FunctionCallNode node) :
        this(node.FunctionName, node.Arguments.Select(a => (FunctionCallArgumentNode)a.Clone()).ToArray())
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
