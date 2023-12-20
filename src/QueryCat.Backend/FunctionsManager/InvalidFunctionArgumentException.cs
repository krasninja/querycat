using QueryCat.Backend.Core;

namespace QueryCat.Backend.FunctionsManager;

/// <summary>
/// The exception occurs when function has invalid argument.
/// </summary>
[Serializable]
#pragma warning disable CA2229
public class InvalidFunctionArgumentException : QueryCatException
#pragma warning restore CA2229
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public InvalidFunctionArgumentException(string message) : base(message)
    {
    }
}
