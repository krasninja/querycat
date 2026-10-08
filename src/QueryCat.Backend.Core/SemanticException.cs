namespace QueryCat.Backend.Core;

/// <summary>
/// The exception occurs on semantic error.
/// </summary>
public class SemanticException : QueryCatException
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public SemanticException(string message) : base(message)
    {
    }
}
