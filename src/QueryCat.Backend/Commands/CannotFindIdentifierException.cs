using QueryCat.Backend.Core;

namespace QueryCat.Backend.Commands;

/// <summary>
/// The exception occurs when application cannot find
/// identifier within current scope.
/// </summary>
[Serializable]
#pragma warning disable CA2229
public class CannotFindIdentifierException : SemanticException
#pragma warning restore CA2229
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">The name of the identifier.</param>
    public CannotFindIdentifierException(string name)
        : base($"Column or variable '{name}' does not exist.")
    {
    }
}
