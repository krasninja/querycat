# Plugin Authentication

Plugins that access remote services can delegate authentication to QueryCat host. The plugin declares authentication providers (OAuth2 settings or API key), the user logs in with `qcat plugin auth login <provider>`, and the plugin requests the active token when it needs it. The host stores tokens and refreshes them.

```
qcat plugin auth login github  ->  OAuth2 provider  ->  auth.json
plugin function  ->  thread.AuthTokensManager  ->  (Thrift) GetAuthToken  ->  host refreshes if needed  ->  token
```

## Declare Providers

Add the `RegisterAuthProviders` method to the `Registration` class. It is discovered the same way as `RegisterFunctions`, so it works for plugins loaded by the proxy and by the assembly plugins loader.

```csharp
public static class Registration
{
    public const string AuthProviderId = "github";

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        // ...
    }

    public static void RegisterAuthProviders(IAuthProvidersRegistry registry)
    {
        registry.Register(new AuthProviderInfo
        {
            Id = AuthProviderId,
            DisplayName = "GitHub",
            AuthorizationUrl = "https://github.com/login/oauth/authorize",
            TokenUrl = "https://github.com/login/oauth/access_token",
            DeviceAuthorizationUrl = "https://github.com/login/device/code",
            ClientId = "<public OAuth app client id>",
            Scopes = ["repo", "read:org"],
            PreferDeviceFlow = true,
            AllowApiKey = true, // Allow "qcat plugin auth login github --api-key" with personal access token.
        });
    }
}
```

If the plugin has its own entry point (`ThriftPluginClient`), register providers before `StartAsync`:

```csharp
Registration.RegisterFunctions(client.FunctionsManager);
Registration.RegisterAuthProviders(client.AuthProviders);
await client.StartAsync(cancellationToken: ct);
```

Provider properties:

| Property | Description |
| --- | --- |
| `Id` | Provider identifier. It must be unique across all plugins. |
| `Type` | `OAuth2` (default) or `ApiKey`. |
| `AuthorizationUrl`, `TokenUrl` | Enable authorization code flow with PKCE. |
| `DeviceAuthorizationUrl`, `TokenUrl` | Enable device authorization flow (RFC 8628). |
| `ClientId`, `ClientSecret` | OAuth2 client. Plugins are public clients, avoid shipping secrets. Users can override them with `--client-id` and `--client-secret`. |
| `Scopes`, `ExtraParams` | Requested scopes and additional authorization parameters (`audience`, `access_type=offline`, etc). |
| `RedirectPort` | Fixed loopback redirect port. Random port is used by default. The redirect URI is `http://127.0.0.1:<port>/callback`. |
| `PreferDeviceFlow` | Use device flow by default. |
| `AllowApiKey` | Allow user to provide API key instead of OAuth2 login. |

## Get Token

Use `IExecutionThread.AuthTokensManager` within the function:

```csharp
[SafeFunction]
[FunctionSignature("github_branches(): object<IRowsInput>")]
public static async ValueTask<VariantValue> GitHubBranchesFunction(IExecutionThread thread, CancellationToken cancellationToken)
{
    // Returns null if the user is not logged in.
    var token = await thread.AuthTokensManager.GetTokenOrDefaultAsync(Registration.AuthProviderId, cancellationToken: cancellationToken);
    // Or throw AuthRequiredException with "run qcat plugin auth login github" message.
    // var token = await thread.AuthTokensManager.GetTokenAsync(Registration.AuthProviderId, cancellationToken: cancellationToken);
    return VariantValue.CreateFromObject(new BranchesRowsInput(token?.AccessToken));
}
```

The host refreshes OAuth2 token if it expires within 60 seconds. If the remote service responds with HTTP 401, request the token again once with `forceRefresh: true`.

Notes:

- The plugin can only get tokens of the providers it declared. Requests for other providers are rejected.
- Tokens are not exposed to SQL queries.
- The plugin functions cache (`func-cache`) also stores providers, so `qcat plugin auth list` does not need to start plugins.
