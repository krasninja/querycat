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
    public virtual IReadOnlyList<QueryContextCondition> GetConditions() => new QueryContextCondition[] { };

    /// <summary>
    /// Get rows limit. Allows to select only needed amount of records.
    /// </summary>
    /// <returns>Limit count.</returns>
    public virtual long? GetLimit() => null;
}
