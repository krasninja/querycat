using QueryCat.Backend.Core;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Exception occurs when not safe operation is executed in safe mode.
/// </summary>
[Serializable]
public sealed class SafeModeException : QueryCatException
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public SafeModeException() : base(Resources.Errors.CannotRunInSafeMode)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Wrong operation message.</param>
    public SafeModeException(string message) : base(message)
    {
    }
}
