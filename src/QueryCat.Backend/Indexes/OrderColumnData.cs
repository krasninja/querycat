using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Indexes;

/// <summary>
/// Data to specify how to order the column.
/// </summary>
internal sealed class OrderColumnData
{
    public int Index { get; }

    public OrderDirection Direction { get; }

    public NullOrder NullOrder { get; }

    public OrderColumnData(int index, OrderDirection direction, NullOrder nullOrder = NullOrder.NullsLast)
    {
        Index = index;
        Direction = direction;
        NullOrder = nullOrder;
    }
}
