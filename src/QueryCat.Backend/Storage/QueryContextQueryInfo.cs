using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

public sealed class QueryContextQueryInfo
{
    /// <summary>
    /// Get columns for select.
    /// </summary>
    public IReadOnlyList<Column> Columns { get; internal set; }

    private readonly List<QueryContextCondition> _queryContextConditions = new();

    /// <summary>
    /// Get query conditions.
    /// </summary>
    public IReadOnlyList<QueryContextCondition> Conditions => _queryContextConditions;

    /// <summary>
    /// Rows offset.
    /// </summary>
    public long Offset { get; internal set; }

    /// <summary>
    /// Get rows limit. Allows to select only needed amount of records.
    /// </summary>
    public long? Limit { get; internal set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columns">Select columns.</param>
    /// <param name="limit">Select limit.</param>
    public QueryContextQueryInfo(IReadOnlyList<Column> columns, long? limit = null)
    {
        Columns = columns;
        Limit = limit;
    }

    /// <summary>
    /// Add condition.
    /// </summary>
    /// <param name="condition">Condition instance.</param>
    internal void AddCondition(QueryContextCondition condition)
    {
        _queryContextConditions.Add(condition);
    }

    /// <summary>
    /// Add condition.
    /// </summary>
    /// <param name="column">Condition column.</param>
    /// <param name="operation">Condition operation.</param>
    /// <param name="valueFuncs">Value functions.</param>
    internal QueryContextCondition AddCondition(
        Column column,
        VariantValue.Operation operation,
        params FuncUnit[] valueFuncs)
    {
        var queryContextCondition = new QueryContextCondition(column, operation, valueFuncs);
        _queryContextConditions.Add(queryContextCondition);
        return queryContextCondition;
    }
}
