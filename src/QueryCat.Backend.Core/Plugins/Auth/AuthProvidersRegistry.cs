using System.Collections.Concurrent;

namespace QueryCat.Backend.Core.Plugins.Auth;

/// <summary>
/// Thread-safe in-memory registry of authentication providers.
/// </summary>
public sealed class AuthProvidersRegistry : IAuthProvidersRegistry
{
    private readonly ConcurrentDictionary<string, AuthProviderInfo> _providers = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void Register(AuthProviderInfo provider)
    {
        ArgumentException.ThrowIfNullOrEmpty(provider.Id);

        var existing = _providers.GetOrAdd(provider.Id, provider);
        if (ReferenceEquals(existing, provider))
        {
            return;
        }
        // The same plugin can re-register the provider (f.e. after cache load and actual plugin load).
        if (string.IsNullOrEmpty(existing.PluginName) || string.IsNullOrEmpty(provider.PluginName)
            || existing.PluginName == provider.PluginName)
        {
            _providers[provider.Id] = provider;
            return;
        }
        throw new PluginException(string.Format(Resources.Errors.AuthProviderAlreadyRegistered,
            provider.Id, existing.PluginName));
    }

    /// <inheritdoc />
    public AuthProviderInfo? Find(string id) => _providers.GetValueOrDefault(id);

    /// <inheritdoc />
    public IReadOnlyList<AuthProviderInfo> GetProviders() => _providers.Values.OrderBy(p => p.Id).ToList();
}
