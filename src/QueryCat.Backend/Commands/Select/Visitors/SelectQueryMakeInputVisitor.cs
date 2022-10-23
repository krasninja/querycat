using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select.Visitors;

/// <summary>
/// The visitor tries to find all input functions (in FROM clause),
/// resolve it and open. The result is saved to <see cref="AstAttributeKeys.RowsInputKey" />.
/// </summary>
internal sealed class SelectQueryMakeInputVisitor : AstVisitor
{
    private readonly ExecutionThread _executionThread;

    public SelectQueryMakeInputVisitor(ExecutionThread executionThread)
    {
        this._executionThread = executionThread;
    }

    /// <inheritdoc />
    public override void Run(IAstNode node)
    {
        var traversal = new AstTraversal(this);
        traversal.PostOrder(node);
    }

    /// <inheritdoc />
    public override void Visit(SelectTableFunctionNode node)
    {
        new ResolveTypesVisitor(_executionThread).Run(node);
        var source = new MakeDelegateVisitor(_executionThread).RunAndReturn(node.TableFunction).Invoke();

        if (DataTypeUtils.IsSimple(source.GetInternalType()))
        {
            node.SetAttribute(AstAttributeKeys.RowsInputKey, new SingleValueRowsInput(source));
            return;
        }
        if (source.AsObject is IRowsInput)
        {
            var rowsInput = (IRowsInput)source.AsObject;
            rowsInput.Open();
            Logger.Instance.Debug($"Open rows input {rowsInput}.", nameof(SelectQueryBodyVisitor));
            node.SetAttribute(AstAttributeKeys.RowsInputKey, rowsInput);
            return;
        }
        if (source.AsObject is IRowsIterator rowsIterator)
        {
            node.SetAttribute(AstAttributeKeys.RowsInputKey, new RowsIteratorInput(rowsIterator));
            return;
        }

        throw new QueryCatException("Invalid rows input.");
    }
}
