using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Can be used to explore more about executing query.
/// </summary>
public abstract class QueryContext
{
    /// <summary>
    /// Information about the rows input.
    /// </summary>
    public QueryContextInputInfo InputInfo { get; set; } = new(NullRowsInput.Instance);

    /// <summary>
    /// Information about the query where the rows input is used.
    /// </summary>
    public abstract QueryContextQueryInfo QueryInfo { get; }

    /// <summary>
    /// Current execution thread.
    /// </summary>
    public IExecutionThread ExecutionThread { get; }

    /// <summary>
    /// Input config storage.
    /// </summary>
    public IInputConfigStorage InputConfigStorage { get; internal set; } = new MemoryInputConfigStorage();

    public QueryContext(IExecutionThread executionThread)
    {
        ExecutionThread = executionThread;
    }

    /// <summary>
    /// Merge arguments from the source query context.
    /// </summary>
    /// <param name="sourceContext">Source query context.</param>
    /// <returns>Instance of <see cref="QueryContext" />.</returns>
    internal virtual QueryContext Merge(QueryContext sourceContext)
    {
        InputInfo.MergeInputArguments(sourceContext.InputInfo.InputArguments);
        return this;
    }
}
