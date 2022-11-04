using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

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
    /// Get cache key for the specific query.
    /// </summary>
    /// <returns>Cache key instance.</returns>
    internal abstract CacheKey GetCacheKey();

    /// <summary>
    /// Add input function keys/arguments.
    /// </summary>
    /// <param name="keys">Arguments.</param>
    /// <returns>Instance of <see cref="QueryContext" />.</returns>
    public virtual QueryContext AddInputArguments(params string[] keys)
    {
        return this;
    }

    /// <summary>
    /// Add key column information.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operations">Key operations.</param>
    /// <returns>Instance of <see cref="QueryContext" />.</returns>
    public virtual QueryContext AddKeyColumn(string columnName, params VariantValue.Operation[] operations)
    {
        return this;
    }

    /// <summary>
    /// Returns <c>true</c> if we can find key column condition.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Column operation.</param>
    /// <param name="value">Condition value.</param>
    /// <returns><c>True</c> if found, <c>false</c> otherwise.</returns>
    public bool HasKeyCondition(string columnName, VariantValue.Operation operation, out VariantValue value)
        => HasKeyCondition(columnName, operation, operation, out value);

    /// <summary>
    /// Returns <c>true</c> if we can find key column condition.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Column operation.</param>
    /// <param name="orOperation">Alternative condition value.</param>
    /// <param name="value">Condition value.</param>
    /// <returns><c>True</c> if found, <c>false</c> otherwise.</returns>
    public virtual bool HasKeyCondition(string columnName, VariantValue.Operation operation,
        VariantValue.Operation orOperation, out VariantValue value)
    {
        value = VariantValue.Null;
        return false;
    }
}
