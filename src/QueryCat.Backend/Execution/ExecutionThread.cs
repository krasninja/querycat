using System.Diagnostics;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Commands;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Execution thread that includes statements to be executed, local variables, options and statistic.
/// </summary>
public sealed class ExecutionThread
{
    private readonly StatementsVisitor _statementsVisitor;

    /// <summary>
    /// Root scope.
    /// </summary>
    internal ExecutionScope RootScope { get; }

    /// <summary>
    /// Functions manager.
    /// </summary>
    public FunctionsManager FunctionsManager { get; } = new();

    /// <summary>
    /// Current executing statement.
    /// </summary>
    internal StatementNode? ExecutingStatement { get; set; }

    /// <summary>
    /// Execution options.
    /// </summary>
    public ExecutionOptions Options { get; }

    /// <summary>
    /// Statistic.
    /// </summary>
    public ExecutionStatistic Statistic { get; } = new();

    /// <summary>
    /// Last execution statement return value.
    /// </summary>
    public VariantValue LastResult { get; private set; } = VariantValue.Null;

    /// <summary>
    /// The event to be called before any statement execution.
    /// </summary>
    public event EventHandler<ExecuteEventArgs>? BeforeStatementExecute;

    /// <summary>
    /// The event to be called after any statement execution.
    /// </summary>
    public event EventHandler<ExecuteEventArgs>? AfterStatementExecute;

    public static readonly ExecutionThread Empty = new();

    public ExecutionThread(
        ExecutionOptions? options = null)
    {
        RootScope = new ExecutionScope();
        Options = options ?? new ExecutionOptions();
        _statementsVisitor = new StatementsVisitor(this);
    }

    /// <summary>
    /// Run the execution flow.
    /// </summary>
    public void Run()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        while (ExecutingStatement != null)
        {
            _statementsVisitor.Run(ExecutingStatement);

            // Fire "before" event.
            var executeEventArgs = new ExecuteEventArgs();
            BeforeStatementExecute?.Invoke(this, executeEventArgs);
            if (!executeEventArgs.ContinueExecution)
            {
                break;
            }

            var result = _statementsVisitor.ResultDelegate.Invoke();
            LastResult = result;

            // Fire "after" event.
            AfterStatementExecute?.Invoke(this, executeEventArgs);
            if (!executeEventArgs.ContinueExecution)
            {
                break;
            }

            if (Options.DefaultRowsOutput != NullRowsOutput.Instance)
            {
                Options.DefaultRowsOutput.Write(result);
            }
            ExecutingStatement = ExecutingStatement.Next;
        }

        stopwatch.Stop();
        Statistic.ExecutionTime = stopwatch.Elapsed;
    }
}
