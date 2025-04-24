using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal class StatementsBlockFuncUnit : IFuncUnit, IDisposable, IAsyncDisposable
{
    private readonly AstVisitor _statementsVisitor;
    private readonly ProgramBodyNode _programBodyNode;
    private readonly HashSet<IDisposable> _disposablesList = new();

    /// <inheritdoc />
    public DataType OutputType => DataType.Void;

    public ExecutionJump Jump { get; protected set; }

    public StatementsBlockFuncUnit(
        StatementsVisitor statementsVisitor,
        ProgramBodyNode programBodyNode)
    {
        _statementsVisitor = statementsVisitor;
        _programBodyNode = programBodyNode;
    }

    /// <inheritdoc />
    public virtual async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        if (_programBodyNode.Statements.Count < 1)
        {
            return VariantValue.Null;
        }

        var currentStatement = _programBodyNode.Statements[0];
        var result = VariantValue.Null;
        while (currentStatement != null && !cancellationToken.IsCancellationRequested)
        {
            // Evaluate the command.
            var commandContext = await _statementsVisitor.RunAndReturnAsync(currentStatement, cancellationToken);
            if (commandContext is IDisposable disposable)
            {
                _disposablesList.Add(disposable);
            }

            // Invoke statement.
            result = await InvokeStatementAsync(thread, commandContext, currentStatement, cancellationToken);
            if (Jump != ExecutionJump.Next)
            {
                break;
            }

            // Get the next statement to execute.
            currentStatement = currentStatement.NextNode;
        }

        return result;
    }

    protected virtual async ValueTask<VariantValue> InvokeStatementAsync(
        IExecutionThread thread,
        IFuncUnit funcUnit,
        StatementNode statementNode,
        CancellationToken cancellationToken = default)
    {
        var result = await funcUnit.InvokeAsync(thread, cancellationToken);
        if (Jump == ExecutionJump.Next &&funcUnit is StatementsBlockFuncUnit statementsBlockFuncUnit)
        {
            Jump = statementsBlockFuncUnit.Jump;
        }
        return result;
    }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var disposable in _disposablesList)
            {
                disposable.Dispose();
            }
            _disposablesList.Clear();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        foreach (var disposable in _disposablesList)
        {
            if (disposable is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                disposable.Dispose();
            }
        }
        _disposablesList.Clear();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    #endregion
}
