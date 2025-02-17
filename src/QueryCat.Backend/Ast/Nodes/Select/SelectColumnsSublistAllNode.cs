namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectColumnsSublistAll : SelectColumnsSublistNode
{
    /// <inheritdoc />
    public override string Code => "column_all";

    public IdentifierExpressionNode? PrefixIdentifier { get; set; }

    public SelectColumnsSublistAll()
    {
    }

    public SelectColumnsSublistAll(SelectColumnsSublistAll node)
    {
        if (node.PrefixIdentifier != null)
        {
            PrefixIdentifier = (IdentifierExpressionNode)node.PrefixIdentifier.Clone();
        }
        Alias = node.Alias;
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsSublistAll(this);

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        if (PrefixIdentifier != null)
        {
            yield return PrefixIdentifier;
        }
    }

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => "*";
}
