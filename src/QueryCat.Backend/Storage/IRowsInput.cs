using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Class implements methods to get rows.
/// </summary>
public interface IRowsInput : IRowsSource, IRowsSchema
{
    /// <summary>
    /// Read the column's value at current position. The column target type is expected, otherwise
    /// the cast will be performed.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="value">Return value.</param>
    /// <returns>Error code.</returns>
    ErrorCode ReadValue(int columnIndex, out VariantValue value);

    /// <summary>
    /// Read next row.
    /// </summary>
    /// <returns>True if there are remain rows to read, false if no row was read.</returns>
    bool ReadNext();

    /// <summary>
    /// Sets the input to its initial position.
    /// </summary>
    void Reset();

    /// <summary>
    /// Write explain information about the current input.
    /// </summary>
    /// <param name="stringBuilder">String builder to write.</param>
    void Explain(IndentedStringBuilder stringBuilder);
}
