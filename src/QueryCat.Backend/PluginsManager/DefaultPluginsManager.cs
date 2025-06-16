using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.PluginsManager;

/// <summary>
/// The class allows to search, install, remove and update plugins.
/// </summary>
public sealed class DefaultPluginsManager : IPluginsManager, IDisposable
{
    private readonly IEnumerable<string> _pluginDirectories;
    private readonly PluginsLoader _pluginsLoader;
    private readonly IPluginsStorage _pluginsStorage;
    private readonly string? _platform;
    private readonly HttpClient _httpClient = new();
    private IReadOnlyList<PluginInfo>? _remotePluginsCache;

    public IEnumerable<string> PluginDirectories => _pluginDirectories;

    /// <inheritdoc />
    public IPluginsLoader PluginsLoader => _pluginsLoader;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(DefaultPluginsManager));

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="pluginDirectories">Directories to search plugins for.</param>
    /// <param name="pluginsLoader">Instance of <see cref="PluginsLoader" />.</param>
    /// <param name="pluginsStorage">Instance of <see cref="IPluginsStorage" />.</param>
    /// <param name="platform">Target platform.</param>
    public DefaultPluginsManager(
        IEnumerable<string> pluginDirectories,
        PluginsLoader pluginsLoader,
        IPluginsStorage pluginsStorage,
        string? platform = null)
    {
        _pluginDirectories = pluginDirectories;
        _pluginsLoader = pluginsLoader;
        _pluginsStorage = pluginsStorage;
        _platform = platform;
    }

    /// <summary>
    /// Get all plugin files.
    /// </summary>
    /// <returns>Files.</returns>
    public IEnumerable<string> GetPluginFiles()
    {
        foreach (var file in _pluginsLoader.GetPluginFiles(new PluginsLoadingOptions
                 {
                     SkipDuplicates = false,
                 }))
        {
            if (_pluginsLoader.IsCorrectPluginFile(file))
            {
                yield return file;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PluginInfo>> ListAsync(
        bool localOnly = false,
        CancellationToken cancellationToken = default)
    {
        var remote = !localOnly
            ? await GetRemotePluginsAsync(cancellationToken).ConfigureAwait(false)
            : [];
        var local = GetLocalPlugins();

        return remote.Union(local).OrderBy(p => p.Name);
    }

    private static readonly string[] _prefixes = [string.Empty, "QueryCat.Plugins.", "qcat-plugins-", "qcat.plugins.", "plugins-", "plugins."];

    private static PluginInfo? TryFindPlugin(string name, string? platform, IReadOnlyCollection<PluginInfo> allPlugins)
    {
        foreach (var prefix in _prefixes)
        {
            var newName = prefix + name;
            var plugin = allPlugins
                .OrderByDescending(p => p.Version)
                .FirstOrDefault(p => newName.Equals(p.Name, StringComparison.OrdinalIgnoreCase)
                                     && (p.Platform == platform || p.Platform == Application.PlatformMulti || string.IsNullOrEmpty(platform)));
            if (plugin != null)
            {
                return plugin;
            }
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<int> InstallAsync(string name, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        var plugins = await GetRemotePluginsAsync(cancellationToken).ConfigureAwait(false);
        var plugin = TryFindPlugin(name, Application.GetPlatform(), plugins);
        if (plugin == null)
        {
            throw new PluginException(string.Format(Resources.Errors.Plugins_CannotFind, name));
        }

        if (!overwrite)
        {
            var localPlugin = TryFindPlugin(name, Application.GetPlatform(), GetLocalPlugins().ToList());
            if (localPlugin != null && localPlugin.Version >= plugin.Version)
            {
                _logger.LogInformation("Skip install because plugin '{Plugin}' already exists.", localPlugin);
                return 0;
            }
        }

        // Create X.downloading file, download and then remove.
        var mainPluginDirectory = GetMainPluginDirectory();
        var fullFileName = Path.Combine(mainPluginDirectory, Path.GetFileName(plugin.Uri));
        _logger.LogInformation("Start downloading plugin file {PluginUri}.", plugin.Uri);
        await FilesUtils.DownloadFileAsync(
                ct => _pluginsStorage.DownloadAsync(plugin.Uri, ct),
                fullFileName,
                cancellationToken)
            .ConfigureAwait(false);
        FilesUtils.MakeUnixExecutable(fullFileName);
        var exists = File.Exists(fullFileName);
        _logger.LogInformation("Saved plugin file {FullFileName}.", fullFileName);
        return exists ? 1 : 0;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(string name, CancellationToken cancellationToken = default)
    {
        if (name == "*")
        {
            foreach (var localPlugin in GetLocalPlugins())
            {
                await UpdateAsyncInternal(localPlugin.Name, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        else
        {
            await UpdateAsyncInternal(name, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UpdateAsyncInternal(string name, CancellationToken cancellationToken)
    {
        using var twoPhaseRemove = new TwoPhaseRemove(renameBeforeRemove: true);
        var pluginsToRemove = GetLocalPlugins(name);
        twoPhaseRemove.AddRange(pluginsToRemove.Select(p => p.Uri));
        await InstallAsync(name, cancellationToken: cancellationToken).ConfigureAwait(false);
        twoPhaseRemove.Remove();
    }

    /// <inheritdoc />
    public Task RemoveAsync(string name, CancellationToken cancellationToken = default)
    {
        var plugins = GetLocalPlugins();
        var plugin = TryFindPlugin(name, _platform, plugins.ToList());
        if (plugin == null)
        {
            throw new PluginException(string.Format(Resources.Errors.Plugins_CannotFind, name));
        }
        if (File.Exists(plugin.Uri))
        {
            _logger.LogInformation("Remove file {Uri}.", plugin.Uri);
            File.Delete(plugin.Uri);
        }

        return Task.CompletedTask;
    }

    private async Task<IReadOnlyList<PluginInfo>> GetRemotePluginsAsync(CancellationToken cancellationToken = default)
    {
        if (_remotePluginsCache != null)
        {
            return _remotePluginsCache;
        }
        _remotePluginsCache = await _pluginsStorage.ListAsync(cancellationToken);
        return _remotePluginsCache;
    }

    private IEnumerable<PluginInfo> GetLocalPlugins(string name = "*")
    {
        return GetPluginFiles()
            .Select(p => CreatePluginInfoFromKey(
                Path.GetFileName(p),
                Path.GetDirectoryName(p) + Path.DirectorySeparatorChar,
                isInstalled: true))
            .Where(p => name == "*" || p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private PluginInfo CreatePluginInfoFromKey(string key, string baseUri, bool isInstalled = false)
    {
        var info = PluginInfo.CreateFromUniversalName(key);
        info.Uri = baseUri + key;
        info.IsInstalled = isInstalled;
        return info;
    }

    private string GetMainPluginDirectory()
    {
        var mainPluginDirectory = _pluginDirectories.FirstOrDefault();
        if (mainPluginDirectory == null)
        {
            throw new PluginException(Resources.Errors.Plugins_NoDirectory);
        }
        if (!Directory.Exists(mainPluginDirectory))
        {
            Directory.CreateDirectory(mainPluginDirectory);
        }
        return mainPluginDirectory;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
