namespace QueryCat.Backend.Utils;

/// <summary>
/// The exception occurs on semantic error.
/// </summary>
[Serializable]
#pragma warning disable CA2229
public class SyntaxException : QueryCatException
#pragma warning restore CA2229
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public SyntaxException(string message) : base(message)
    {
    }
}
