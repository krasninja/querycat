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
    public CannotFindArgumentException(string functionName, string argumentName) :
        base(string.Format(Resources.Errors.CannotFindArgument, argumentName, functionName))
    {
    }
}
