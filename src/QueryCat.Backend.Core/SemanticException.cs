namespace QueryCat.Backend.Core;

/// <summary>
/// The exception occurs on semantic error.
/// </summary>
[Serializable]
#pragma warning disable CA2229
public class SemanticException : QueryCatException
#pragma warning restore CA2229
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public SemanticException(string message) : base(message)
    {
    }
}
