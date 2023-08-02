namespace QueryCat.Backend.Abstractions;

/// <summary>
/// Cursor seek origin to move from.
/// </summary>
public enum CursorSeekOrigin
{
    /// <summary>
    /// Start of the frame.
    /// </summary>
    Begin,

    /// <summary>
    /// Current cursor position.
    /// </summary>
    Current,

    /// <summary>
    /// End of the frame.
    /// </summary>
    End
}
