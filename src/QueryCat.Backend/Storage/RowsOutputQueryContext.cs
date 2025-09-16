using QueryCat.Backend.Core.Data;

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
}
