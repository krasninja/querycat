using QueryCat.Backend.Ast.Nodes;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Event arguments for <see cref="DefaultExecutionThread" />.
/// </summary>
public sealed class ExecuteEventArgs : EventArgs
{
    /// <summary>
    /// <c>True</c> to continue query execution, <c>false</c>
    /// will break query execute and do not run next statement.
    /// </summary>
    public bool ContinueExecution { get; set; } = true;

    /// <summary>
    /// Current executing statement.
    /// </summary>
    internal StatementNode ExecutingStatementNode { get; set; }

    internal ExecuteEventArgs(StatementNode executingStatementNode)
    {
        ExecutingStatementNode = executingStatementNode;
    }
}
