using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Can be used to explore more about executing query.
/// </summary>
public abstract class QueryContext
{
    /// <summary>
    /// Query filter condition.
    /// </summary>
    /// <param name="Column">Filter column.</param>
    /// <param name="Operation">Filter operation.</param>
    /// <param name="Value">Filter value.</param>
    public sealed record QueryCondition(Column Column, VariantValue.Operation Operation, VariantValue Value);

    /// <summary>
    /// Get columns for select.
    /// </summary>
    /// <returns>Columns for select.</returns>
    public abstract IReadOnlyList<Column> GetColumns();
}
