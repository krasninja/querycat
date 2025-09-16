using QueryCat.Backend.Core.Data;

namespace QueryCat.Tester;

public sealed class SimpleQueryContext : QueryContext
{
    /// <inheritdoc />
    public SimpleQueryContext(QueryContextQueryInfo queryInfo) : base(queryInfo)
    {
    }
}
