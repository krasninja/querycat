using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Execution;

namespace QueryCat.Backend.Commands.Select.Visitors;

internal sealed class SelectCreateContextVisitor : AstVisitor
{
    private readonly ExecutionThread _executionThread;
    private readonly AstTraversal _astTraversal;

    public SelectCreateContextVisitor(ExecutionThread executionThread)
    {
        this._executionThread = executionThread;
        this._astTraversal = new AstTraversal(this);
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        _astTraversal.PostOrder(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectQuerySpecificationNode node)
    {
        var selectContextCreator = new SelectContextCreator(_executionThread, GetParentContexts(node));
        selectContextCreator.CreateForQuery(node);
    }

    private SelectCommandContext[] GetParentContexts(IAstNode node)
    {
        var parentContexts = new List<SelectCommandContext>();
        foreach (var parentNode in _astTraversal.GetParents().OfType<SelectQuerySpecificationNode>())
        {
            if (parentNode.Equals(node) || parentNode.TableExpression == null)
            {
                continue;
            }
            var parentContext = parentNode.GetRequiredAttribute<SelectCommandContext>(AstAttributeKeys.ResultKey);
            parentContexts.Add(parentContext);
        }
        return parentContexts.ToArray();
    }
}
