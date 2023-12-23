using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// Allows to create rows input from .NET objects using remote source.
/// </summary>
/// <typeparam name="TClass">Object class.</typeparam>
public abstract class FetchRowsInput<TClass> : EnumerableRowsInput<TClass> where TClass : class
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public FetchRowsInput() : base(Array.Empty<TClass>())
    {
    }

    /// <summary>
    /// Create rows frame.
    /// </summary>
    /// <param name="builder">Rows frame builder.</param>
    protected abstract void Initialize(ClassRowsFrameBuilder<TClass> builder);

    /// <inheritdoc />
    public override void Open()
    {
        Initialize(Builder);
        Columns = Builder.Columns.ToArray();
    }

    /// <inheritdoc />
    protected override void Load()
    {
        base.Load();

        var fetch = new Fetcher<TClass>();
        var queryLimit = QueryContext.QueryInfo.Limit + QueryContext.QueryInfo.Offset;
        if (queryLimit.HasValue && AreAllKeyColumnsSet)
        {
            fetch.Limit = Math.Min((int)queryLimit.Value, fetch.Limit);
        }
        Enumerator = GetData(fetch).GetEnumerator();
    }

    /// <summary>
    /// Get data.
    /// </summary>
    /// <param name="fetcher">Remote data fetcher.</param>
    /// <returns>Objects.</returns>
    protected abstract IEnumerable<TClass> GetData(Fetcher<TClass> fetcher);
}
