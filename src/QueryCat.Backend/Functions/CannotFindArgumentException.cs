namespace QueryCat.Backend.Functions;

/// <summary>
/// The exception occurs when provided argument is not part of
/// function signature.
/// </summary>
[Serializable]
#pragma warning disable CA2229
public class CannotFindArgumentException : QueryCatException
#pragma warning restore CA2229
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="argumentName">Function argument.</param>
    public CannotFindArgumentException(string functionName, string argumentName) :
        base($"Cannot find argument '{argumentName}' in function '{functionName}'.")
    {
    }
}
