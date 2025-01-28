namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Rows iterator.
/// </summary>
public interface IRowsIterator : IRowsSchema
{
    /// <summary>
    /// Current row. If you want to use it outside the iterator - please
    /// copy it. The iterator uses the same instance of row and just
    /// updates values.
    /// </summary>
    Row Current { get; }

    /// <summary>
    /// Move cursor to the next row.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns><c>True</c> if cursor was moved and data is available, <c>false</c> if there is no row anymore.</returns>
    ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the iterator to its initial position.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>Awaitable task.</returns>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Write explain information about the current iterator.
    /// </summary>
    /// <param name="stringBuilder">String builder to write.</param>
    void Explain(IndentedStringBuilder stringBuilder);
}
