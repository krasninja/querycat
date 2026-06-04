using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Backend.PluginsManager;

/// <summary>
/// Use local file system as plugins storage.
/// </summary>
public sealed class LocalPluginsStorage : IPluginsStorage
{
    private readonly string _rootDirectory;

    public LocalPluginsStorage(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PluginInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        var pluginInfos = new List<PluginInfo>();

        if (!Directory.Exists(_rootDirectory))
        {
            return Task.FromResult<IReadOnlyList<PluginInfo>>(pluginInfos);
        }

        var pluginFiles = Directory.EnumerateFiles(_rootDirectory, "*.nupkg", SearchOption.AllDirectories);

        foreach (var pluginFile in pluginFiles)
        {
            var pluginInfo = PluginInfo.CreateFromUniversalName(pluginFile);
            pluginInfo.Size = new FileInfo(pluginFile).Length;
            pluginInfo.Uri = pluginFile;
            pluginInfos.Add(pluginInfo);
        }

        return Task.FromResult<IReadOnlyList<PluginInfo>>(
            PluginInfo.FilterOnlyLatest(pluginInfos).ToList()
        );
    }

    /// <inheritdoc />
    public Task<Stream> DownloadAsync(string uri, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(uri) || !File.Exists(uri))
        {
            return Task.FromResult(Stream.Null);
        }
        return Task.FromResult<Stream>(File.OpenRead(uri));
    }
}
