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

    /// <summary>
    /// Get columns order.
    /// </summary>
    /// <returns>Columns with orders.</returns>
    internal virtual IReadOnlyList<QueryContextOrder> GetColumnsOrders() => new QueryContextOrder[] { };

    /// <summary>
    /// The rows input should set it to <c>true</c> if it can implement whole ordering.
    /// Otherwise, it will be done on client side.
    /// </summary>
    internal virtual bool CanOrder { get; set; }
}
