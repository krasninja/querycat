using System.Xml;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Abstractions.Plugins;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The class allows to search, install, remove and update plugins.
/// </summary>
public sealed class DefaultPluginsManager : PluginsManager, IDisposable
{
    private const string PluginsStorageUri = @"https://querycat.storage.yandexcloud.net/";

    private readonly IEnumerable<string> _pluginDirectories;
    private readonly PluginsLoader _pluginsLoader;
    private readonly string _bucketUri;
    private readonly HttpClient _httpClient = new();
    private List<PluginInfo>? _remotePluginsCache;

    public IEnumerable<string> PluginDirectories => _pluginDirectories;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<DefaultPluginsManager>();

    public DefaultPluginsManager(
        IEnumerable<string> pluginDirectories,
        PluginsLoader pluginsLoader,
        string? bucketUri = null)
    {
        _pluginDirectories = pluginDirectories;
        _pluginsLoader = pluginsLoader;
        _bucketUri = !string.IsNullOrEmpty(bucketUri) ? bucketUri : PluginsStorageUri;
    }

    /// <summary>
    /// Get all plugin files.
    /// </summary>
    /// <returns>Files.</returns>
    public IEnumerable<string> GetPluginFiles()
    {
        foreach (var file in _pluginsLoader.GetPluginFiles())
        {
            if (_pluginsLoader.IsCorrectPluginFile(file))
            {
                yield return file;
            }
        }
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<PluginInfo>> ListAsync(
        bool localOnly = false,
        CancellationToken cancellationToken = default)
    {
        var remote = !localOnly
            ? await GetRemotePluginsAsync(cancellationToken).ConfigureAwait(false)
            : Array.Empty<PluginInfo>();
        var local = GetLocalPlugins();

        return remote.Union(local);
    }

    /// <inheritdoc />
    public override async Task<int> InstallAsync(string name, CancellationToken cancellationToken = default)
    {
        var plugins = await GetRemotePluginsAsync(cancellationToken).ConfigureAwait(false);
        var plugin = plugins.FirstOrDefault(p => name.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
        if (plugin == null)
        {
            throw new PluginException($"Cannot find plugin '{name}' in repository.");
        }

        // Create X.downloading file, download and then remove.
        var mainPluginDirectory = GetMainPluginDirectory();
        var stream = await _httpClient.GetStreamAsync(plugin.Uri, cancellationToken).ConfigureAwait(false);
        var fullFileName = Path.Combine(mainPluginDirectory, Path.GetFileName(plugin.Uri));
        var fullFileNameDownloading = fullFileName + ".downloading";
        await using var outputFileStream = new FileStream(fullFileNameDownloading, FileMode.OpenOrCreate);
        await stream.CopyToAsync(outputFileStream, cancellationToken)
            .ConfigureAwait(false);
        stream.Close();
        outputFileStream.Close();
        var overwrite = File.Exists(fullFileName);
        File.Move(fullFileNameDownloading, fullFileName, overwrite);
        _logger.LogInformation("Save plugin file {FullFileName}.", fullFileName);
        return overwrite ? 1 : 0;
    }

    /// <inheritdoc />
    public override async Task UpdateAsync(string name, CancellationToken cancellationToken = default)
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
        await InstallAsync(name, cancellationToken).ConfigureAwait(false);
        twoPhaseRemove.Remove();
    }

    /// <inheritdoc />
    public override Task RemoveAsync(string name, CancellationToken cancellationToken = default)
    {
        foreach (var localPlugin in GetLocalPlugins())
        {
            if (name.Equals(localPlugin.Name, StringComparison.OrdinalIgnoreCase) && File.Exists(localPlugin.Uri))
            {
                _logger.LogInformation("Remove file {Uri}.", localPlugin.Uri);
                File.Delete(localPlugin.Uri);
            }
        }
        return Task.CompletedTask;
    }

    private async Task<IEnumerable<PluginInfo>> GetRemotePluginsAsync(CancellationToken cancellationToken = default)
    {
        if (_remotePluginsCache != null)
        {
            return _remotePluginsCache;
        }
        await using var stream = await _httpClient.GetStreamAsync(_bucketUri, cancellationToken)
            .ConfigureAwait(false);
        using var xmlReader = new XmlTextReader(stream);
        var xmlKeys = new List<string>();
        while (xmlReader.ReadToFollowing("Key"))
        {
            xmlKeys.Add(xmlReader.ReadString());
        }
        // Select only latest version.
        _remotePluginsCache = xmlKeys
            .Select(k => CreatePluginInfoFromKey(k, _bucketUri))
            .GroupBy(k => k.Name, v => v)
            .Select(v => v.MaxBy(q => q.Version))
            .ToList()!;
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
            throw new PluginException("Cannot find a directory for plugins.");
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
