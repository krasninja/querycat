namespace QueryCat.Backend.Execution;

/// <summary>
/// The exception occurs when application cannot find
/// identifier within current scope.
/// </summary>
[Serializable]
#pragma warning disable CA2229
public class CannotFindIdentifierException : SemanticException
#pragma warning restore CA2229
{
    public CannotFindIdentifierException(string name)
        : base($"Column or variable '{name}' does not exist.")
    {
    }
}
