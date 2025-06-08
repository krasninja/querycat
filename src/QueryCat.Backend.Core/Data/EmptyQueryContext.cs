namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Empty query context.
/// </summary>
public sealed class NullQueryContext : QueryContext
{
    /// <summary>
    /// Empty instance of query context.
    /// </summary>
    public static NullQueryContext Instance { get; } = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    public NullQueryContext() : base(new(columns: [], limit: null))
    {
    }
}
