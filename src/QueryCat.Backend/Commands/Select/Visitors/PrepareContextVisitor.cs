using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// The visitor creates <see cref="SelectCommandContext" /> and initializes correct
/// child-parent relationship.
/// </summary>
internal sealed class PrepareContextVisitor : AstVisitor
{
    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node)
    {
        CreateContext(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectQueryCombineNode node)
    {
        CreateContext(node);
    }

    private void CreateContext(SelectQueryNode node)
    {
        var context = new SelectCommandContext();
        var parentQueryNode = AstTraversal.GetFirstParent<SelectQueryNode>(n => n.Id != node.Id);
        if (parentQueryNode != null)
        {
            context.SetParent(parentQueryNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ContextKey));
        }
        node.SetAttribute(AstAttributeKeys.ContextKey, context);
    }
}
