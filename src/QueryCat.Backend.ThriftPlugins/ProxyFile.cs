using QueryCat.Backend.Core;

namespace QueryCat.Backend.ThriftPlugins;

/// <summary>
/// General routines for plugin proxy file.
/// </summary>
public static class ProxyFile
{
    public const int ProxyLatestVersion = 8;
    private const string ProxyExecutable = "qcat-plugins-proxy";

    /// <summary>
    /// Get plugins proxy file name.
    /// </summary>
    /// <param name="includeVersion">Append version postfix.</param>
    /// <returns>Plugins proxy file name.</returns>
    public static string GetProxyFileName(bool includeVersion = false)
        => GetProxyFileNameWithVersion(includeVersion, ProxyLatestVersion);

    /// <summary>
    /// Get plugins proxy file name.
    /// </summary>
    /// <param name="includeVersion">Append version postfix.</param>
    /// <param name="version">Version.</param>
    /// <returns>Plugins proxy file name.</returns>
    public static string GetProxyFileNameWithVersion(bool includeVersion = false, int version = -1)
    {
        version = version == -1 ? ProxyLatestVersion : version;
        var proxyExecutable = includeVersion ? ProxyExecutable + version : ProxyExecutable;
        return Application.GetPlatform() == Application.PlatformWindows
            ? proxyExecutable + ".exe"
            : proxyExecutable;
    }

    internal static string ResolveProxyFileName(string? applicationDirectory, int version = -1)
    {
        var proxyExecutable = PathUtils.ResolveExecutableFullPath(
            GetProxyFileNameWithVersion(includeVersion: true, version: version),
            applicationDirectory);
        if (string.IsNullOrEmpty(proxyExecutable))
        {
            proxyExecutable = PathUtils.ResolveExecutableFullPath(
                GetProxyFileNameWithVersion(includeVersion: false, version: version),
                applicationDirectory);
        }
        return proxyExecutable;
    }

    /// <summary>
    /// Remove old versions of proxy.
    /// </summary>
    public static void CleanUpPreviousVersions(string? applicationDirectory)
    {
        for (var i = ProxyLatestVersion - 1; i > 0; i--)
        {
            var resolvedExecutable = ResolveProxyFileName(applicationDirectory, version: i);
            if (!string.IsNullOrEmpty(resolvedExecutable))
            {
                File.Delete(resolvedExecutable);
            }
        }
    }
}
