namespace QueryCat.Backend.Execution;

/// <summary>
/// Event arguments for <see cref="ExecutionThread" />.
/// </summary>
public sealed class ExecuteEventArgs : EventArgs
{
    /// <summary>
    /// <c>True</c> to continue execute the query, <c>false</c>
    /// will break query execute and do not run next statement.
    /// </summary>
    public bool ContinueExecution { get; set; } = true;
}
