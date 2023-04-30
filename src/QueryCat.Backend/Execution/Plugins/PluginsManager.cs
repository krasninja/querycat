using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Execution.Plugins;

/// <summary>
/// The class allows to search, install, remove and update plugins.
/// </summary>
public sealed class PluginsManager : IDisposable
{
    private const string PluginsStorageUri = @"https://querycat.storage.yandexcloud.net/";
    private const string ApplicationPluginsDirectory = "plugins";

    private static readonly Regex KeyRegex = new(@"^(?<name>[a-zA-Z\.]+)\.(?<version>\d+\.\d+\.\d+)\.(dll|nupkg)$",
        RegexOptions.Compiled);

    private readonly IEnumerable<string> _pluginDirectories;
    private readonly string _bucketUri;
    private readonly HttpClient _httpClient = new();
    private List<PluginInfo>? _remotePluginsCache;

    public IEnumerable<string> PluginDirectories => _pluginDirectories;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<PluginsManager>();

    public PluginsManager(IEnumerable<string> pluginDirectories, string? bucketUri = null)
    {
        _pluginDirectories = pluginDirectories;
        _bucketUri = !string.IsNullOrEmpty(bucketUri) ? bucketUri : PluginsStorageUri;
    }

    /// <summary>
    /// Get all plugin directories.
    /// </summary>
    /// <param name="appDirectory">Application local directory.</param>
    /// <returns>Directories.</returns>
    public static IReadOnlyList<string> GetPluginDirectories(string appDirectory)
    {
        var exeDirectory = AppContext.BaseDirectory;
        return new[]
        {
            Path.Combine(appDirectory, ApplicationPluginsDirectory),
            exeDirectory,
            Path.Combine(exeDirectory, ApplicationPluginsDirectory)
        };
    }

    /// <summary>
    /// Get all plugin files.
    /// </summary>
    /// <param name="pluginDirectories">Plugin directories.</param>
    /// <returns>Files.</returns>
    public static IEnumerable<string> GetPluginFiles(IEnumerable<string> pluginDirectories)
    {
        foreach (var source in pluginDirectories)
        {
            if (File.Exists(source) && source.Contains("Plugin"))
            {
                var extension = Path.GetExtension(source);
                if (!extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)
                    && !extension.Equals(".nupkg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                yield return source;
            }
            if (!Directory.Exists(source))
            {
                continue;
            }

            var pluginFiles = Directory.GetFiles(source, "*Plugin*.dll");
            foreach (var pluginFile in pluginFiles)
            {
                yield return pluginFile;
            }

            pluginFiles = Directory.GetFiles(source, "*Plugin*.nupkg");
            foreach (var pluginFile in pluginFiles)
            {
                yield return pluginFile;
            }
        }
    }

    /// <summary>
    /// List all local and remote plugins.
    /// </summary>
    /// <param name="localOnly">List local plugins only.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of plugins info.</returns>
    public async Task<IEnumerable<PluginInfo>> ListAsync(
        bool localOnly = false,
        CancellationToken cancellationToken = default)
    {
        var remote = !localOnly
            ? await GetRemotePluginsAsync(cancellationToken).ConfigureAwait(false)
            : Array.Empty<PluginInfo>();
        var local = GetLocalPlugins();

        return remote.Union(local);
    }

    /// <summary>
    /// Install the plugin from remote repository.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of created files.</returns>
    public async Task<int> InstallAsync(string name, CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Update the plugin. Remove all current versions and install the new one.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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
        await InstallAsync(name, cancellationToken).ConfigureAwait(false);
        twoPhaseRemove.Remove();
    }

    /// <summary>
    /// Remove the plugin.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public Task RemoveAsync(string name, CancellationToken cancellationToken = default)
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
        return GetPluginFiles(_pluginDirectories)
            .Select(p => CreatePluginInfoFromKey(
                Path.GetFileName(p),
                Path.GetDirectoryName(p) + Path.DirectorySeparatorChar,
                isInstalled: true))
            .Where(p => name == "*" || p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private PluginInfo CreatePluginInfoFromKey(string key, string baseUri, bool isInstalled = false)
    {
        var name = GetNameFromKey(key);
        return new PluginInfo(name)
        {
            Version = GetVersionFromKey(key),
            Uri = baseUri + key,
            IsInstalled = isInstalled,
        };
    }

    internal static Version GetVersionFromKey(string key)
    {
        var match = KeyRegex.Match(key);
        var version = match.Groups["version"].Value;
        return !string.IsNullOrEmpty(version) && version.Length > 3
            ? Version.Parse(match.Groups["version"].Value)
            : new Version();
    }

    internal static string GetNameFromKey(string key)
    {
        var match = KeyRegex.Match(key);
        var name = match.Groups["name"].Value;
        if (string.IsNullOrEmpty(name))
        {
            name = Path.GetFileNameWithoutExtension(key);
        }
        return name;
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
