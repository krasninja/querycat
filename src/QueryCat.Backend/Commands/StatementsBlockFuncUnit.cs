using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal class StatementsBlockFuncUnit : IFuncUnit, IExecutionFlowFuncUnit, IDisposable, IAsyncDisposable
{
    private readonly AstVisitor _statementsVisitor;
    private readonly StatementNode[] _statements;
    private readonly HashSet<object> _disposablesList = new();

    /// <inheritdoc />
    public DataType OutputType => DataType.Void;

    public ExecutionJump Jump { get; protected set; }

    public StatementsBlockFuncUnit(
        StatementsVisitor statementsVisitor,
        StatementNode[] statements)
    {
        _statementsVisitor = statementsVisitor;
        _statements = statements;
    }

    /// <inheritdoc />
    public virtual async ValueTask<VariantValue> InvokeAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        if (_statements.Length < 1)
        {
            return VariantValue.Null;
        }

        var currentStatement = _statements[0];
        var result = VariantValue.Null;
        while (currentStatement != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Evaluate the command.
            var commandContext = await _statementsVisitor.RunAndReturnAsync(currentStatement, cancellationToken);
            if (commandContext is IDisposable || commandContext is IAsyncDisposable)
            {
                _disposablesList.Add(commandContext);
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
        if (Jump == ExecutionJump.Next && funcUnit is IExecutionFlowFuncUnit executionFlowFuncUnit)
        {
            Jump = executionFlowFuncUnit.Jump;
        }
        return result;
    }

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var obj in _disposablesList)
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
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
        foreach (var obj in _disposablesList)
        {
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (obj is IDisposable disposable)
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
