using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Can be used to explore more about executing query.
/// </summary>
public abstract class QueryContext
{
    /// <summary>
    /// Get columns for select.
    /// </summary>
    /// <returns>Columns for select.</returns>
    public abstract IReadOnlyList<Column> GetColumns();

    /// <summary>
    /// Get query conditions.
    /// </summary>
    /// <returns>List of conditions.</returns>
    public abstract IReadOnlyList<QueryContextCondition> GetConditions();
}
