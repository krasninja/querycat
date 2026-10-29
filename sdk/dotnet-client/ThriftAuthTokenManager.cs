using System.Threading;
using System.Threading.Tasks;
using QueryCat.Backend.Core.Plugins.Auth;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Gets authentication tokens from QueryCat host. The host is responsible for login and refresh.
/// </summary>
public sealed class ThriftAuthTokenManager : IAuthTokenManager
{
    private readonly ThriftPluginClient _client;

    /// <summary>
    /// Providers declared by the plugin. They are sent to the host on plugin registration.
    /// </summary>
    public IAuthProvidersRegistry Providers { get; } = new AuthProvidersRegistry();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="client">Plugin client.</param>
    public ThriftAuthTokenManager(ThriftPluginClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async ValueTask<AuthToken?> GetTokenOrDefaultAsync(
        string providerId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var token = await _client.ThriftClient.GetAuthTokenAsync(_client.Token, providerId, forceRefresh, cancellationToken);
        return SdkConvert.Convert(token);
    }
}
