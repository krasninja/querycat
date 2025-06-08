namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Can be used to explore more about executing query.
/// </summary>
public abstract class QueryContext
{
    /// <summary>
    /// Information about the query where the rows input is used.
    /// </summary>
    public QueryContextQueryInfo QueryInfo { get; }

    /// <summary>
    /// Input config storage.
    /// </summary>
    public IInputConfigStorage ConfigStorage { get; protected set; } = NullInputConfigStorage.Instance;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="queryInfo">Query info.</param>
    public QueryContext(QueryContextQueryInfo queryInfo)
    {
        QueryInfo = queryInfo;
    }
}
