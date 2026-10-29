namespace QueryCat.Backend.Core.Plugins.Auth;

/// <summary>
/// The exception occurs when user is not logged in for the authentication provider.
/// </summary>
public sealed class AuthRequiredException : PluginException
{
    /// <summary>
    /// Provider identifier.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="providerId">Provider identifier.</param>
    public AuthRequiredException(string providerId)
        : base(string.Format(Resources.Errors.AuthRequired, providerId))
    {
        ProviderId = providerId;
    }
}
