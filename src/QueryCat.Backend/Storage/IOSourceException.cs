using QueryCat.Backend.Core;

namespace QueryCat.Backend.Storage;

/// <summary>
/// This is the base exception for input/output storage
/// operations.
/// </summary>
[Serializable]
#pragma warning disable CA2229
// ReSharper disable once InconsistentNaming
public class IOSourceException : QueryCatException
#pragma warning restore CA2229
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public IOSourceException(string message) : base(message)
    {
    }
}
