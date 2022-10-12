using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Query filter condition.
/// </summary>
/// <param name="Column">Filter column.</param>
/// <param name="Operation">Filter operation.</param>
/// <param name="Value">Filter value.</param>
public sealed record QueryContextCondition(Column Column, VariantValue.Operation Operation, VariantValue Value);
