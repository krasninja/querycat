namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Rows iterator with cursor. It is possible to set cursor position and change
/// current row.
/// </summary>
public interface ICursorRowsIterator : IRowsIterator
{
    /// <summary>
    /// Current cursor position.
    /// </summary>
    int Position { get; }

    /// <summary>
    /// Total rows.
    /// </summary>
    int TotalRows { get; }

    /// <summary>
    /// Move cursor to the specific position. -1 is the special initial position.
    /// </summary>
    /// <param name="offset">Position to move.</param>
    /// <param name="origin">Specifies seek mode.</param>
    void Seek(int offset, CursorSeekOrigin origin);
}
