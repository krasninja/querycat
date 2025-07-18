using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Class implements methods to get rows.
/// </summary>
public interface IRowsInput : IRowsSource, IRowsSchema, IRowsInputKeys
{
    /// <summary>
    /// Unique keys identifies rows input among others. It can be used by cache layer.
    /// </summary>
    string[] UniqueKey { get; }

    /// <summary>
    /// Read the column's value at current position. The column target type is expected, otherwise
    /// the cast will be performed.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="value">Return value.</param>
    /// <returns>Error code.</returns>
    ErrorCode ReadValue(int columnIndex, out VariantValue value);

    /// <summary>
    /// Read the next row.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>True if there are remain rows to read, false if no row was read.</returns>
    ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Write explain information about the current input.
    /// </summary>
    /// <param name="stringBuilder">String builder to write.</param>
    void Explain(IndentedStringBuilder stringBuilder);
}
