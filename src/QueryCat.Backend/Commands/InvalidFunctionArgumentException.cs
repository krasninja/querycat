using QueryCat.Backend.Core;

namespace QueryCat.Backend.Commands;

/// <summary>
/// The exception occurs when function has invalid argument.
/// </summary>
public class InvalidFunctionArgumentException : QueryCatException
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public InvalidFunctionArgumentException(string message) : base(message)
    {
    }
}
