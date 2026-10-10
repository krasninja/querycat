using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Backend.PluginsManager;

/// <summary>
/// The class allows to search, install, remove and update plugins.
/// </summary>
public sealed class DefaultPluginsManager : IPluginsManager
{
    private readonly IEnumerable<string> _pluginDirectories;
    private readonly PluginsLoader _pluginsLoader;
    private readonly IPluginsStorage _pluginsStorage;
    private readonly string? _platform;
    private IReadOnlyList<PluginInfo>? _remotePluginsCache;

    public IEnumerable<string> PluginDirectories => _pluginDirectories;

    private string Platform => _platform ?? Application.GetPlatform();

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

        return remote.Concat(local).OrderBy(p => p.Name);
    }

    private static readonly string[] _prefixes = [string.Empty, "QueryCat.Plugins.", "qcat-plugins-", "qcat.plugins.", "plugins-", "plugins."];

    private static bool IsNameMatch(string name, string targetName)
    {
        foreach (var prefix in _prefixes)
        {
            var newName = prefix + name;
            if (newName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private PluginInfo? FindPlugin(string name, IEnumerable<PluginInfo> plugins)
    {
        var architecture = Application.GetArchitecture();
        var candidates = plugins
            .Where(p => IsPlatformMatch(p, Platform, architecture))
            .OrderByDescending(p => p.Version)
            .ToList();
        foreach (var prefix in _prefixes)
        {
            var plugin = candidates.FirstOrDefault(p => (prefix + name).Equals(p.Name, StringComparison.OrdinalIgnoreCase));
            if (plugin != null)
            {
                return plugin;
            }
        }
        return null;
    }

    private static bool IsPlatformMatch(PluginInfo pluginInfo, string? platform, string? architecture)
    {
        var platformMatch =
            pluginInfo.Platform == platform
            || pluginInfo.Platform == Application.PlatformMulti
            || string.IsNullOrEmpty(platform);
        if (!platformMatch)
        {
            return false;
        }

        var archMatch =
            pluginInfo.Architecture == architecture
            || pluginInfo.Architecture == Application.ArchitectureMsil
            || pluginInfo.Architecture == Application.ArchitectureUnknown
            || string.IsNullOrEmpty(architecture);
        if (!archMatch)
        {
            return false;
        }

        return platformMatch && archMatch;
    }

    /// <inheritdoc />
    public async Task<int> InstallAsync(string name, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        var plugins = await GetRemotePluginsAsync(cancellationToken).ConfigureAwait(false);
        var plugin = FindPlugin(name, plugins);
        if (plugin == null)
        {
            throw new PluginException(string.Format(Resources.Errors.Plugins_CannotFind, name));
        }

        if (!overwrite)
        {
            var localPlugin = FindPlugin(name, GetLocalPlugins());
            if (localPlugin != null && localPlugin.Version >= plugin.Version)
            {
                _logger.LogInformation("Skip install because plugin '{Plugin}' already exists.", localPlugin);
                return 0;
            }
        }

        var mainPluginDirectory = GetMainPluginDirectory();
        var fullFileName = Path.Combine(mainPluginDirectory, Path.GetFileName(plugin.Uri));
        _logger.LogInformation("Start downloading plugin file {PluginUri}.", plugin.Uri);
        await FilesUtils.DownloadFileAsync(
                ct => _pluginsStorage.DownloadAsync(plugin.Uri, ct),
                fullFileName,
                cancellationToken)
            .ConfigureAwait(false);
        FilesUtils.MakeUnixExecutable(fullFileName);
        _logger.LogInformation("Saved plugin file {FullFileName}.", fullFileName);

        CleanupPlugins();
        return 1;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(string name, CancellationToken cancellationToken = default)
    {
        if (name == "*")
        {
            foreach (var localPlugin in GroupByName(GetLocalPlugins()))
            {
                try
                {
                    await UpdateAsyncInternal(localPlugin.Name, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (PluginException e)
                {
                    _logger.LogWarning(e, "Cannot update plugin '{Plugin}'.", localPlugin.Name);
                }
            }
        }
        else
        {
            await UpdateAsyncInternal(name, cancellationToken).ConfigureAwait(false);
        }

        CleanupPlugins();
    }

    private async Task UpdateAsyncInternal(string name, CancellationToken cancellationToken)
    {
        await InstallAsync(name, overwrite: false, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string name, CancellationToken cancellationToken = default)
    {
        var architecture = Application.GetArchitecture();
        var plugins = GetLocalPlugins()
            .Where(p => IsNameMatch(name, p.Name) && IsPlatformMatch(p, _platform, architecture))
            .ToArray();
        if (plugins.Length == 0)
        {
            throw new PluginException(string.Format(Resources.Errors.Plugins_CannotFind, name));
        }

        foreach (var pluginInfo in plugins)
        {
            if (File.Exists(pluginInfo.Uri))
            {
                _logger.LogInformation("Remove file {Uri}.", pluginInfo.Uri);
                File.Delete(pluginInfo.Uri);
            }
        }

        return Task.CompletedTask;
    }

    private void CleanupPlugins()
    {
        var mainDirectory = Path.GetFullPath(GetMainPluginDirectory());
        var pluginFiles = GetPluginFiles()
            .Where(pf => Path.GetDirectoryName(pf) == mainDirectory)
            .ToList();
        var actualPlugins =
            pluginFiles.Select(
                pf =>
                {
                    var plugin = CreatePluginInfoFromKey(
                        Path.GetFileName(pf),
                        Path.GetDirectoryName(pf) + Path.DirectorySeparatorChar,
                        isInstalled: true);
                    plugin.Uri = pf;
                    return plugin;
                })
                .ToArray();

        foreach (var pluginInfo in PluginInfo.FilterOnlyLatest(actualPlugins))
        {
            pluginFiles.RemoveAll(p => p == pluginInfo.Uri);
        }

        foreach (var file in pluginFiles)
        {
            try
            {
                _logger.LogInformation("Remove obsolete plugin file {PluginFile}.", file);
                File.Delete(file);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Cannot remove obsolete plugin file {PluginFile}.", file);
            }
        }
    }

    private static IEnumerable<PluginInfo> GroupByName(IEnumerable<PluginInfo> plugins)
    {
        return plugins
            .OrderByDescending(p => p.Version)
            .GroupBy(p => p.Name)
            .Select(g => g.First());
    }

    private async Task<IReadOnlyList<PluginInfo>> GetRemotePluginsAsync(CancellationToken cancellationToken = default)
    {
        if (_remotePluginsCache != null)
        {
            return _remotePluginsCache;
        }
        _remotePluginsCache = await _pluginsStorage.ListAsync(cancellationToken)
            .ConfigureAwait(false);
        return _remotePluginsCache;
    }

    private IEnumerable<PluginInfo> GetLocalPlugins(string name = "*")
    {
        return GetPluginFiles()
            .Select(p => CreatePluginInfoFromKey(
                Path.GetFileName(p),
                Path.GetDirectoryName(p) + Path.DirectorySeparatorChar,
                isInstalled: true))
            .Where(p => name == "*" || IsNameMatch(name, p.Name));
    }

    internal static PluginInfo CreatePluginInfoFromKey(string key, string baseUri, bool isInstalled = false)
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
}
