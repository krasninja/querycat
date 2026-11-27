using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins.Auth;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Sample.Functions;

/// <summary>
/// Test OAuth authentication.
/// </summary>
/// <remarks>
/// Use mock server:
/// `docker run --rm -p 8080:8080 ghcr.io/navikt/mock-oauth2-server:6.1.1`.
/// </remarks>
internal static class TestAuth
{
    public const string ProviderId = "mock";

    /// <summary>
    /// Sample authentication provider. Login with "qcat plugin auth login sample --client-id ..."
    /// or "qcat plugin auth login sample --api-key".
    /// </summary>
    public static AuthProviderInfo Provider { get; } = new()
    {
        Id = ProviderId,
        ClientId = "sample_auth_client_id",
        DisplayName = "Mock OAuth2",
        AuthorizationUrl = "http://localhost:8080/default/authorize",
        TokenUrl = "http://localhost:8080/default/token",
        DeviceAuthorizationUrl = "http://localhost:8080/default/device/code",
        Scopes = ["openid", "somescope"],
        PreferDeviceFlow = false,
        AllowApiKey = true,
    };

    [SafeFunction]
    [Description("Test function that shows the authentication status. The token itself is not returned.")]
    [FunctionSignature("sample_auth_status(): string")]
    public static async ValueTask<VariantValue> TestAuthFunction(IExecutionThread thread, CancellationToken cancellationToken)
    {
        var token = await thread.AuthTokensManager.GetTokenOrDefaultAsync(ProviderId, cancellationToken: cancellationToken);
        if (token == null)
        {
            return new VariantValue("not_logged_in");
        }
        return new VariantValue($"logged_in, type: {token.TokenType}, length: {token.AccessToken.Length}");
    }
}
