namespace QueryCat.Backend.Storage;

/// <summary>
/// Can be used to explore more about executing query.
/// </summary>
public abstract class QueryContext
{
    /// <summary>
    /// Information about the query where the rows input is used.
    /// </summary>
    public abstract QueryContextQueryInfo QueryInfo { get; }

    /// <summary>
    /// Input config storage.
    /// </summary>
    public IInputConfigStorage InputConfigStorage { get; internal set; } = new MemoryInputConfigStorage();
}
