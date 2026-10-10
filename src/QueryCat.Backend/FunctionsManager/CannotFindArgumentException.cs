using QueryCat.Backend.Core;

namespace QueryCat.Backend.FunctionsManager;

/// <summary>
/// The exception occurs when provided argument is not part of
/// function signature.
/// </summary>
public sealed class CannotFindArgumentException : QueryCatException
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="argumentName">Function argument.</param>
    public CannotFindArgumentException(string functionName, string argumentName) :
        base(string.Format(Resources.Errors.CannotFindArgumentInFunction, argumentName, functionName))
    {
    }
}
