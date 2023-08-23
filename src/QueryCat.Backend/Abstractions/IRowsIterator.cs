namespace QueryCat.Backend.Abstractions;

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
    /// <returns><c>True</c> if cursor was moved and data is available, <c>false</c> if there is no row anymore.</returns>
    bool MoveNext();

    /// <summary>
    /// Sets the iterator to its initial position.
    /// </summary>
    void Reset();

    /// <summary>
    /// Write explain information about the current iterator.
    /// </summary>
    /// <param name="stringBuilder">String builder to write.</param>
    void Explain(IndentedStringBuilder stringBuilder);
}
