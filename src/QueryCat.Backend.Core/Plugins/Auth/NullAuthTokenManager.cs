namespace QueryCat.Backend.Core.Plugins.Auth;

/// <summary>
/// Token manager that never returns tokens and ignores providers registration.
/// </summary>
public sealed class NullAuthTokenManager : IAuthTokenManager
{
    /// <summary>
    /// Default instance of <see cref="NullAuthTokenManager" />.
    /// </summary>
    public static NullAuthTokenManager Instance { get; } = new();

    private sealed class NullAuthProvidersRegistry : IAuthProvidersRegistry
    {
        /// <inheritdoc />
        public void Register(AuthProviderInfo provider)
        {
        }

        /// <inheritdoc />
        public AuthProviderInfo? Find(string id) => null;

        /// <inheritdoc />
        public IReadOnlyList<AuthProviderInfo> GetProviders() => [];
    }

    /// <inheritdoc />
    public IAuthProvidersRegistry Providers { get; } = new NullAuthProvidersRegistry();

    /// <inheritdoc />
    public ValueTask<AuthToken?> GetTokenOrDefaultAsync(
        string providerId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default) => ValueTask.FromResult<AuthToken?>(null);
}
