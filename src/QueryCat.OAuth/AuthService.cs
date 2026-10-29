using System.Net;
using QueryCat.Backend.Auth;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Plugins.Auth;

namespace QueryCat.OAuth;

/// <summary>
/// Manages plugins authentication: list providers, login and logout.
/// </summary>
public sealed class AuthService
{
    private readonly IAuthProvidersRegistry _providers;
    private readonly IAuthTokenStore _store;
    private readonly OAuth2Client _oauth2Client;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="providers">Authentication providers registry.</param>
    /// <param name="store">Tokens store.</param>
    /// <param name="oauth2Client">OAuth2 client. Default is used if null.</param>
    public AuthService(IAuthProvidersRegistry providers, IAuthTokenStore store, OAuth2Client? oauth2Client = null)
    {
        _providers = providers;
        _store = store;
        _oauth2Client = oauth2Client ?? new OAuth2Client();
    }

    /// <summary>
    /// Get authentication providers declared by plugins and their login status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Providers statuses.</returns>
    public async Task<IReadOnlyList<AuthProviderStatus>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var tokens = await _store.GetAllAsync(cancellationToken);
        var providers = _providers.GetProviders();
        var items = providers
            .Select(p => CreateStatus(p.Id, p.PluginName, GetProviderType(p), tokens.GetValueOrDefault(p.Id)))
            .ToList();
        // Also show stored credentials of removed plugins, so user can log out.
        items.AddRange(tokens
            .Where(t => providers.All(p => !p.Id.Equals(t.Key, StringComparison.OrdinalIgnoreCase)))
            .Select(t => CreateStatus(t.Key, string.Empty, string.Empty, t.Value)));
        return items;
    }

    /// <summary>
    /// Log in to the authentication provider and store the credentials.
    /// </summary>
    /// <param name="name">Provider identifier or plugin name.</param>
    /// <param name="options">Login options.</param>
    /// <param name="interaction">User interaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication provider.</returns>
    public async Task<AuthProviderInfo> LoginAsync(
        string name,
        AuthLoginOptions options,
        IAuthLoginInteraction interaction,
        CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider(name);

        // API key.
        if (provider.Type == AuthType.ApiKey || options.UseApiKey)
        {
            if (provider.Type == AuthType.OAuth2 && !provider.AllowApiKey)
            {
                throw new QueryCatException(string.Format(Resources.Errors.AuthApiKeyNotAllowed, provider.Id));
            }
            var apiKey = await interaction.ReadApiKeyAsync(provider, cancellationToken);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new QueryCatException(Resources.Errors.AuthEmptyApiKey);
            }
            await _store.SetAsync(provider.Id, new StoredAuthToken
            {
                Type = AuthType.ApiKey,
                AccessToken = apiKey.Trim(),
            }, cancellationToken);
            return provider;
        }

        // OAuth2.
        var clientId = options.ClientId ?? provider.ClientId;
        var clientSecret = options.ClientSecret ?? provider.ClientSecret;
        if (string.IsNullOrEmpty(clientId))
        {
            throw new QueryCatException(string.Format(Resources.Errors.AuthNoClientId, provider.Id));
        }
        var useDeviceFlow = options.UseDeviceFlow
            || (provider.PreferDeviceFlow && provider.SupportsDeviceFlow)
            || !provider.SupportsAuthorizationCodeFlow;

        var response = useDeviceFlow
            ? await LoginWithDeviceFlowAsync(provider, clientId, clientSecret, options, interaction, cancellationToken)
            : await LoginWithAuthorizationCodeAsync(provider, clientId, clientSecret, options, interaction, cancellationToken);

        var token = OAuth2Client.CreateStoredToken(response);
        // Keep overrides, they are needed to refresh the token.
        token.ClientId = options.ClientId ?? string.Empty;
        token.ClientSecret = options.ClientSecret ?? string.Empty;
        await _store.SetAsync(provider.Id, token, cancellationToken);
        return provider;
    }

    public sealed record LogoutResult(string ProviderId, bool Removed);

    /// <summary>
    /// Remove stored credentials of the authentication provider.
    /// </summary>
    /// <param name="name">Provider identifier or plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Provider identifier and whether the credentials have been removed.</returns>
    public async Task<LogoutResult> LogoutAsync(string name, CancellationToken cancellationToken = default)
    {
        // Try the exact identifier first, so credentials of removed plugins can be deleted too.
        var providerId = name;
        if (await _store.GetAsync(providerId, cancellationToken) == null)
        {
            providerId = ResolveProvider(name).Id;
        }
        var removed = await _store.RemoveAsync(providerId, cancellationToken);
        return new LogoutResult(providerId, removed);
    }

    /// <summary>
    /// Find provider by identifier or by plugin name if the plugin has only one provider.
    /// </summary>
    /// <param name="name">Provider identifier or plugin name.</param>
    /// <returns>Provider.</returns>
    public AuthProviderInfo ResolveProvider(string name)
    {
        var provider = _providers.Find(name);
        if (provider != null)
        {
            return provider;
        }

        // Plugin name can be full ("QueryCat.Plugins.Github") or short ("github").
        var pluginProviders = _providers.GetProviders()
            .Where(p => p.PluginName.Equals(name, StringComparison.OrdinalIgnoreCase)
                || p.PluginName.EndsWith('.' + name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (pluginProviders.Count == 1)
        {
            return pluginProviders[0];
        }
        if (pluginProviders.Count > 1)
        {
            throw new QueryCatException(string.Format(Resources.Errors.AuthProviderAmbiguous, name,
                string.Join(", ", pluginProviders.Select(p => p.Id))));
        }
        throw new QueryCatException(string.Format(Resources.Errors.AuthProviderNotFound, name));
    }

    private async Task<OAuth2Client.OAuth2TokenResponse> LoginWithDeviceFlowAsync(
        AuthProviderInfo provider,
        string clientId,
        string? clientSecret,
        AuthLoginOptions options,
        IAuthLoginInteraction interaction,
        CancellationToken cancellationToken)
    {
        if (!provider.SupportsDeviceFlow)
        {
            throw new QueryCatException(string.Format(Resources.Errors.AuthFlowNotSupported, provider.Id, "device"));
        }

        var device = await _oauth2Client.StartDeviceAuthorizationAsync(provider, clientId, cancellationToken);
        interaction.ShowDeviceCode(device.Uri, device.UserCode);
        if (options.OpenBrowser)
        {
            BrowserLauncher.TryOpen(device.VerificationUriComplete ?? device.Uri);
        }
        interaction.ShowWaiting();
        return await _oauth2Client.PollDeviceTokenAsync(provider, clientId, clientSecret, device, cancellationToken);
    }

    private async Task<OAuth2Client.OAuth2TokenResponse> LoginWithAuthorizationCodeAsync(
        AuthProviderInfo provider,
        string clientId,
        string? clientSecret,
        AuthLoginOptions options,
        IAuthLoginInteraction interaction,
        CancellationToken cancellationToken)
    {
        using var listener = new LoopbackRedirectListener(provider.RedirectPort);
        var pkce = OAuth2Client.CreatePkce();
        var state = OAuth2Client.CreateState();
        var url = OAuth2Client.BuildAuthorizationUrl(provider, clientId, listener.RedirectUri, state, pkce);

        interaction.ShowAuthorizationUrl(url);
        if (options.OpenBrowser)
        {
            BrowserLauncher.TryOpen(url);
        }
        interaction.ShowWaiting();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(options.CallbackTimeout);
        var query = await listener.WaitForCallbackAsync(CreateCallbackHtml, timeoutCts.Token);

        if (query.TryGetValue("error", out var error))
        {
            throw new QueryCatException(string.Format(Resources.Errors.AuthAuthorizationFailed,
                error, query.GetValueOrDefault("error_description")));
        }
        if (!query.TryGetValue("state", out var receivedState) || receivedState != state)
        {
            throw new QueryCatException(Resources.Errors.AuthStateMismatch);
        }
        if (!query.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
        {
            throw new QueryCatException(string.Format(Resources.Errors.AuthAuthorizationFailed, "no_code", string.Empty));
        }

        return await _oauth2Client.ExchangeCodeAsync(provider, clientId, clientSecret, code, listener.RedirectUri, pkce,
            cancellationToken);
    }

    private static string CreateCallbackHtml(IReadOnlyDictionary<string, string> query)
    {
        var message = query.TryGetValue("error", out var error)
            ? string.Format(Resources.Messages.AuthLogin_CallbackError,
                error + " " + query.GetValueOrDefault("error_description"))
            : Resources.Messages.AuthLogin_CallbackSuccess;
        return
            $"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8">
              <title>QueryCat</title>
              <meta name="viewport" content="width=device-width, initial-scale=1">
            </head>
            <body style="font-family: sans-serif; margin: 1em;">
                <p>{WebUtility.HtmlEncode(message)}</p>
            </body>
            </html>
            """;
    }

    private static string GetProviderType(AuthProviderInfo provider)
    {
        if (provider.Type == AuthType.ApiKey)
        {
            return "api_key";
        }
        return provider.AllowApiKey ? "oauth2,api_key" : "oauth2";
    }

    private static AuthProviderStatus CreateStatus(string id, string plugin, string providerType, StoredAuthToken? token)
    {
        if (token == null || string.IsNullOrEmpty(token.AccessToken))
        {
            return new AuthProviderStatus(id, plugin, providerType, AuthProviderStatus.StatusNotLoggedIn, null, null, string.Empty);
        }
        // Expired token with refresh token will be refreshed on use.
        var status = token.IsExpired() && string.IsNullOrEmpty(token.RefreshToken)
            ? AuthProviderStatus.StatusExpired
            : AuthProviderStatus.StatusLoggedIn;
        return new AuthProviderStatus(
            id,
            plugin,
            providerType,
            status,
            token.Type,
            token.ExpiresAt.HasValue ? DateTimeOffset.FromUnixTimeSeconds(token.ExpiresAt.Value).LocalDateTime : null,
            string.Join(' ', token.Scopes ?? []));
    }
}
