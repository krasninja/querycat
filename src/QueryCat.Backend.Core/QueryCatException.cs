namespace QueryCat.Backend.Core;

/// <summary>
/// Base QueryCat exception.
/// </summary>
public class QueryCatException : Exception
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public QueryCatException()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public QueryCatException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="innerException">Inner exception.</param>
    public QueryCatException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
