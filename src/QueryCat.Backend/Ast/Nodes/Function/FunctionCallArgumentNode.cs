namespace QueryCat.Backend.Ast.Nodes.Function;

/// <summary>
/// Function call argument node.
/// </summary>
public sealed class FunctionCallArgumentNode : AstNode
{
    public string? Key { get; }

    public ExpressionNode ExpressionValue { get; }

    public bool IsPositional => string.IsNullOrEmpty(Key);

    /// <inheritdoc />
    public override string Code => "func_arg";

    public FunctionCallArgumentNode(string? key, ExpressionNode expressionValue)
    {
        Key = key;
        ExpressionValue = expressionValue;
    }

    public FunctionCallArgumentNode(ExpressionNode expressionValue)
    {
        ExpressionValue = expressionValue;
    }

    public FunctionCallArgumentNode(FunctionCallArgumentNode node) :
        this(node.Key, (ExpressionNode)node.ExpressionValue.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ExpressionValue;
    }

    /// <inheritdoc />
    public override object Clone() => new FunctionCallArgumentNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => !string.IsNullOrEmpty(Key)
        ? $"{Key}=>{ExpressionValue}"
        : $"{ExpressionValue}";
}
