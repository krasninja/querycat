using QueryCat.Backend.Abstractions;

namespace QueryCat.Backend.Indexes;

/// <summary>
/// Index that allows ordered access.
/// </summary>
public interface IOrderIndex : IIndex
{
    /// <summary>
    /// Get order iterator.
    /// </summary>
    /// <returns>The instance of <see cref="IRowsIterator" />.</returns>
    ICursorRowsIterator GetOrderIterator();
}
