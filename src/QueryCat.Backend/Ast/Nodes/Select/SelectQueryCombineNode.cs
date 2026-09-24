using System.Text;

namespace QueryCat.Backend.Ast.Nodes.Select;

internal sealed class SelectQueryCombineNode : SelectQueryNode
{
    public SelectQueryNode LeftQueryNode { get; }

    public SelectQueryNode RightQueryNode { get; }

    public SelectQueryCombineType CombineType { get; }

    public bool IsDistinct { get; }

    /// <inheritdoc />
    public override string Code => "select_query_combine";

    public SelectQueryCombineNode(
        SelectQueryNode leftQueryNode,
        SelectQueryNode rightQueryNode,
        SelectQueryCombineType combineType,
        bool isDistinct = true,
        string? alias = null) : base(leftQueryNode.ColumnsListNode)
    {
        LeftQueryNode = leftQueryNode;
        RightQueryNode = rightQueryNode;
        CombineType = combineType;
        IsDistinct = isDistinct;
        if (!string.IsNullOrEmpty(alias))
        {
            Alias = alias;
        }
    }

    public SelectQueryCombineNode(SelectQueryCombineNode node) : this(
        (SelectQueryNode)node.LeftQueryNode.Clone(),
        (SelectQueryNode)node.RightQueryNode.Clone(),
        node.CombineType,
        node.IsDistinct)
    {
        CopyClausesFrom(node);
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override bool IsSingleValue() => false;

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren()
    {
        yield return LeftQueryNode;
        yield return RightQueryNode;
        foreach (var astNode in base.GetChildren())
        {
            if (!ReferenceEquals(astNode, ColumnsListNode))
            {
                yield return astNode;
            }
        }
    }

    /// <inheritdoc />
    public override object Clone() => new SelectQueryCombineNode(this);

    /// <inheritdoc />
    public override ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken)
        => visitor.VisitAsync(this, cancellationToken);

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder()
            .Append('(')
            .Append($" {LeftQueryNode}")
            .Append($" {CombineType}")
            .Append($" {(IsDistinct ? "Distinct" : "All")}")
            .Append($" {RightQueryNode}")
            .Append(')');
        if (OffsetNode != null)
        {
            sb.Append($" Offset {OffsetNode}");
        }
        if (FetchNode != null)
        {
            sb.Append($" Fetch {FetchNode}");
        }
        if (OrderByNode != null)
        {
            sb.Append($" Order {OrderByNode}");
        }
        return sb.ToString();
    }
}
