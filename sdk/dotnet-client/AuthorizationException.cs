using System;
using QueryCat.Backend.Core;

namespace QueryCat.Plugins.Client;

/// <summary>
/// The exception occurs if wrong token provided.
/// </summary>
[Serializable]
public sealed class AuthorizationException : QueryCatException
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public AuthorizationException() : base(Resources.Errors.AuthorizationError)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public AuthorizationException(string message) : base(message)
    {
    }
}
