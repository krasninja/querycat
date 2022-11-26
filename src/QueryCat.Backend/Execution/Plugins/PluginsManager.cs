using System.Text.RegularExpressions;
using System.Xml;
using QueryCat.Backend.Logging;

namespace QueryCat.Backend.Execution.Plugins;

/// <summary>
/// The class allows to search, install, remove and update plugins.
/// </summary>
public sealed class PluginsManager : IDisposable
{
    private const string PluginsStorageUri = @"https://querycat.storage.yandexcloud.net/";
    internal const string ApplicationPluginsDirectory = "plugins";

    private readonly IEnumerable<string> _pluginDirectories;
    private readonly string _bucketUri;
    private readonly HttpClient _httpClient = new();
    private static readonly Regex KeyRegex = new(@"^(?<name>[a-zA-Z\.]+)\.(?<version>\d+\.\d+\.\d+)\.(dll|nupkg)$", RegexOptions.Compiled);

    public IEnumerable<string> PluginDirectories => _pluginDirectories;

    public PluginsManager(IEnumerable<string> pluginDirectories, string? bucketUri = null)
    {
        _pluginDirectories = pluginDirectories;
        _bucketUri = bucketUri ?? PluginsStorageUri;
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of plugins info.</returns>
    public async Task<IEnumerable<PluginInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        var remote = await GetRemotePluginsAsync(cancellationToken);
        var local = GetLocalPlugins();

        return remote.Union(local);
    }

    /// <summary>
    /// Install the plugin from remote repository.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InstallAsync(string name, CancellationToken cancellationToken = default)
    {
        var plugins = await GetRemotePluginsAsync(cancellationToken);
        var plugin = plugins.FirstOrDefault(p => p.Name == name);
        if (plugin == null)
        {
            throw new InvalidOperationException($"Cannot find plugin {name} in repository.");
        }

        var mainPluginDirectory = GetMainPluginDirectory();
        var stream = await _httpClient.GetStreamAsync(plugin.Uri, cancellationToken);
        var fullFileName = Path.Combine(mainPluginDirectory, Path.GetFileName(plugin.Uri));
        await using var outputFileStream = new FileStream(fullFileName, FileMode.CreateNew);
        await stream.CopyToAsync(outputFileStream, cancellationToken);
        Logger.Instance.Info($"Save plugin file {fullFileName}.", nameof(PluginsManager));
    }

    /// <summary>
    /// Update the plugin. Remove all current versions and install the new one.
    /// </summary>
    /// <param name="name">Plugin name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UpdateAsync(string name, CancellationToken cancellationToken = default)
    {
        await RemoveAsync(name, cancellationToken);
        await InstallAsync(name, cancellationToken);
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
            if (name == localPlugin.Name && File.Exists(localPlugin.Uri))
            {
                Logger.Instance.Info($"Remove file {localPlugin.Uri}.", nameof(PluginsManager));
                File.Delete(localPlugin.Uri);
            }
        }
        return Task.CompletedTask;
    }

    private async Task<IEnumerable<PluginInfo>> GetRemotePluginsAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = await _httpClient.GetStreamAsync(_bucketUri, cancellationToken);
        using var xmlReader = new XmlTextReader(stream);
        var xmlKeys = new List<string>();
        while (xmlReader.ReadToFollowing("Key"))
        {
            xmlKeys.Add(xmlReader.ReadString());
        }
        return xmlKeys.Select(k => CreatePluginInfoFromKey(k, _bucketUri)).ToList();
    }

    private IEnumerable<PluginInfo> GetLocalPlugins()
    {
        return GetPluginFiles(_pluginDirectories)
            .Select(p => CreatePluginInfoFromKey(Path.GetFileName(p),
                Path.GetDirectoryName(p) + Path.DirectorySeparatorChar));
    }

    private PluginInfo CreatePluginInfoFromKey(string key, string baseUri)
    {
        var match = KeyRegex.Match(key);
        var name = match.Groups["name"].Value;
        if (string.IsNullOrEmpty(name))
        {
            name = Path.GetFileNameWithoutExtension(key);
        }
        var version = match.Groups["version"].Value;
        return new PluginInfo(name)
        {
            Version = !string.IsNullOrEmpty(version) && version.Length > 3
                ? Version.Parse(match.Groups["version"].Value)
                : new Version(),
            Uri = baseUri + key,
        };
    }

    private string GetMainPluginDirectory()
    {
        var mainPluginDirectory = _pluginDirectories.FirstOrDefault();
        if (mainPluginDirectory == null)
        {
            throw new InvalidOperationException("Cannot find a directory for plugins.");
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
