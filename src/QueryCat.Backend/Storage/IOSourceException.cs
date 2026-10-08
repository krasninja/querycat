using QueryCat.Backend.Core;

namespace QueryCat.Backend.Storage;

/// <summary>
/// This is the base exception for input/output storage
/// operations.
/// </summary>
// ReSharper disable once InconsistentNaming
public class IOSourceException : QueryCatException
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public IOSourceException(string message) : base(message)
    {
    }
}
