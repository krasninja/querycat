using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Event arguments on variable resolve.
/// </summary>
public class ResolveVariableEventArgs : EventArgs
{
    /// <summary>
    /// Variable name. Can be overriden.
    /// </summary>
    public string VariableName { get; set; }

    /// <summary>
    /// Current execution thread.
    /// </summary>
    public IExecutionThread ExecutionThread { get; }

    /// <summary>
    /// Result. Can be overriden.
    /// </summary>
    public VariantValue Result { get; set; }

    /// <summary>
    /// Is variable handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="variableName">Variable name.</param>
    /// <param name="executionThread">Execution thread.</param>
    public ResolveVariableEventArgs(string variableName, IExecutionThread executionThread)
    {
        VariableName = variableName;
        ExecutionThread = executionThread;
    }
}
