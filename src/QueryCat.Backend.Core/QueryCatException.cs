namespace QueryCat.Backend.Core;

/// <summary>
/// Base application exception.
/// </summary>
[Serializable]
#pragma warning disable CA2229
public class QueryCatException : Exception
#pragma warning restore CA2229
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
