using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// Make AST transformations to simplify executor factory implementation.
/// </summary>
internal class TransformQueryAstVisitor : AstVisitor
{
    private readonly List<Action> _transformations = new();

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        base.Run(node);

        foreach (var transformation in _transformations)
        {
            transformation.Invoke();
        }
    }

    /// <inheritdoc />
    public override void Visit(FunctionCallNode node)
    {
        // Replace COUNT() by COUNT(1).
        if (node.Arguments.Count == 0
            && node.FunctionName.Equals("count", StringComparison.OrdinalIgnoreCase))
        {
            _transformations.Add(() =>
            {
                node.Arguments.Add(new FunctionCallArgumentNode(new LiteralNode(VariantValue.OneIntegerValue)));
            });
        }
    }
}
