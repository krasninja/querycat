namespace QueryCat.Backend.Commands;

/// <summary>
/// Different execution flow jumps.
/// </summary>
internal enum ExecutionJump
{
    /// <summary>
    /// Normal flow, go to the next statement.
    /// </summary>
    Next,

    /// <summary>
    /// Break statement.
    /// </summary>
    Break,

    /// <summary>
    /// Continue statement.
    /// </summary>
    Continue,

    /// <summary>
    /// Return statement.
    /// </summary>
    Return,

    /// <summary>
    /// Halt program execution.
    /// </summary>
    Halt,
}
