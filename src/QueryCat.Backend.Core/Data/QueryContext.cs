namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Can be used to explore more about executing query.
/// </summary>
public abstract class QueryContext
{
    private const int DefaultPrereadRowsCount = 10;

    /// <summary>
    /// Information about the query where the rows input is used.
    /// </summary>
    public QueryContextQueryInfo QueryInfo { get; }

    /// <summary>
    /// Config storage.
    /// </summary>
    public IConfigStorage ConfigStorage { get; protected set; } = NullConfigStorage.Instance;

    /// <summary>
    /// Desired number of rows to read on source open.
    /// </summary>
    public int PrereadRowsCount { get; set; } = DefaultPrereadRowsCount;

    /// <summary>
    /// Skip read if columns cannot be detected.
    /// </summary>
    internal bool SkipIfNoColumns { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="queryInfo">Query info.</param>
    public QueryContext(QueryContextQueryInfo queryInfo)
    {
        QueryInfo = queryInfo;
    }
}
