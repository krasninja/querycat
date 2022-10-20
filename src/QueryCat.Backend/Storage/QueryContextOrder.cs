using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Query column order.
/// </summary>
/// <param name="Column">Order by column.</param>
/// <param name="OrderDirection">Order by direction.</param>
public sealed record QueryContextOrder(
    Column Column,
    OrderDirection OrderDirection);
