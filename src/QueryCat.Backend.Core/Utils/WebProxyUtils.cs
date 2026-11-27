using System.Net;

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// Web proxy utilities.
/// </summary>
public static class WebProxyUtils
{
    /// <summary>
    /// Create a web proxy from the specified URI.
    /// </summary>
    /// <param name="proxyUri">Proxy URI.</param>
    /// <returns>Web proxy instance.</returns>
    public static IWebProxy CreateProxy(string proxyUri)
    {
        ArgumentException.ThrowIfNullOrEmpty(proxyUri);

        var uri = new Uri(proxyUri, UriKind.Absolute);
        if (uri.Scheme.Equals("socks", StringComparison.OrdinalIgnoreCase))
        {
            uri = new UriBuilder(uri)
            {
                Scheme = "socks5",
            }.Uri;
        }

        var proxy = new WebProxy(uri);
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var separatorIndex = uri.UserInfo.IndexOf(':', StringComparison.Ordinal);
            var userName = separatorIndex >= 0 ? uri.UserInfo[..separatorIndex] : uri.UserInfo;
            var password = separatorIndex >= 0 ? uri.UserInfo[(separatorIndex + 1)..] : string.Empty;
            proxy.Credentials = new NetworkCredential(
                Uri.UnescapeDataString(userName),
                Uri.UnescapeDataString(password));
        }
        return proxy;
    }

    /// <summary>
    /// Set the default proxy for HTTP clients.
    /// </summary>
    /// <param name="proxyUri">Proxy URI. No changes are made if it is empty.</param>
    public static void SetDefaultProxy(string? proxyUri)
    {
        if (!string.IsNullOrEmpty(proxyUri))
        {
            HttpClient.DefaultProxy = CreateProxy(proxyUri);
        }
    }
}
