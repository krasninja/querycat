using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Ast.Nodes.Function;

/// <summary>
/// Function call argument node.
/// </summary>
internal sealed class FunctionCallArgumentNode : AstNode
{
    public string? Key { get; }

    public ExpressionNode ExpressionValueNode { get; }

    public bool IsPositional => string.IsNullOrEmpty(Key);

    /// <inheritdoc />
    public override string Code => "func_arg";

    public FunctionCallArgumentNode(string? key, ExpressionNode expressionValueNode)
    {
        if (!string.IsNullOrEmpty(key))
        {
            Key = FunctionFormatter.NormalizeName(key);
        }
        ExpressionValueNode = expressionValueNode;
    }

    public FunctionCallArgumentNode(ExpressionNode expressionValueNode)
    {
        ExpressionValueNode = expressionValueNode;
    }

    public FunctionCallArgumentNode(FunctionCallArgumentNode node) :
        this(node.Key, (ExpressionNode)node.ExpressionValueNode.Clone())
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ExpressionValueNode;
    }

    /// <inheritdoc />
    public override object Clone() => new FunctionCallArgumentNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => !string.IsNullOrEmpty(Key)
        ? $"{Key}=>{ExpressionValueNode}"
        : $"{ExpressionValueNode}";
}
