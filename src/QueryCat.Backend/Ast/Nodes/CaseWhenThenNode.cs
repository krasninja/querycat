namespace QueryCat.Backend.Ast.Nodes;

public sealed class CaseWhenThenNode : AstNode
{
    public ExpressionNode ConditionNode { get; }

    public ExpressionNode ResultNode { get; }

    /// <inheritdoc />
    public override string Code => "case_when";

    /// <inheritdoc />
    public CaseWhenThenNode(ExpressionNode conditionNode, ExpressionNode resultNode)
    {
        ConditionNode = conditionNode;
        ResultNode = resultNode;
    }

    public CaseWhenThenNode(CaseWhenThenNode thenNode)
        : this(
            (ExpressionNode)thenNode.ConditionNode.Clone(),
            (ExpressionNode)thenNode.ResultNode.Clone())
    {
        thenNode.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return ConditionNode;
        yield return ResultNode;
    }

    /// <inheritdoc />
    public override object Clone() => new CaseWhenThenNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);
}
