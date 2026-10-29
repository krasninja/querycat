namespace QueryCat.Backend.Core.Plugins.Auth;

/// <summary>
/// Registry of authentication providers declared by plugins.
/// </summary>
public interface IAuthProvidersRegistry
{
    /// <summary>
    /// Register authentication provider.
    /// </summary>
    /// <param name="provider">Provider.</param>
    void Register(AuthProviderInfo provider);

    /// <summary>
    /// Find provider by identifier.
    /// </summary>
    /// <param name="id">Provider identifier.</param>
    /// <returns>Provider or null if not found.</returns>
    AuthProviderInfo? Find(string id);

    /// <summary>
    /// Get all registered providers.
    /// </summary>
    /// <returns>Providers.</returns>
    IReadOnlyList<AuthProviderInfo> GetProviders();
}
