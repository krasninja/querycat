using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Context for rows output.
/// </summary>
public class RowsOutputQueryContext : QueryContext
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columns">Column of output.</param>
    /// <param name="configStorage">Configuration storage.</param>
    public RowsOutputQueryContext(Column[] columns, IConfigStorage configStorage)
        : base(new QueryContextQueryInfo(columns))
    {
        ConfigStorage = configStorage;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columns">Column of output.</param>
    public RowsOutputQueryContext(Column[] columns) : this(columns, NullConfigStorage.Instance)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columns">Column of output.</param>
    /// <param name="executionThread">Execution thread.</param>
    public RowsOutputQueryContext(Column[] columns, IExecutionThread<ExecutionOptions> executionThread)
        : base(new QueryContextQueryInfo(columns))
    {
        ConfigStorage = executionThread.ConfigStorage;
        PrereadRowsCount = executionThread.Options.AnalyzeRowsCount;
        SkipIfNoColumns = executionThread.Options.SkipIfNoColumns;
    }
}
