using System.Xml;
using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Backend.PluginsManager;

internal sealed class S3PluginsStorage : IPluginsStorage, IDisposable
{
    private const string PluginsStorageUri = @"https://querycat.storage.yandexcloud.net/";

    private readonly HttpClient _httpClient = new();
    private readonly string _bucketUri;

    public S3PluginsStorage(string? bucketUri = null)
    {
        _bucketUri = !string.IsNullOrEmpty(bucketUri) ? bucketUri : PluginsStorageUri;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PluginInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = await _httpClient.GetStreamAsync(_bucketUri, cancellationToken)
            .ConfigureAwait(false);
        using var xmlReader = new XmlTextReader(stream);
        var xmlKeys = new List<string>();
        while (xmlReader.ReadToFollowing("Key"))
        {
            xmlKeys.Add(xmlReader.ReadString());
        }
        // Select only latest version.
        return xmlKeys
            .Select(k => CreatePluginInfoFromKey(k, _bucketUri))
            .GroupBy(k => Tuple.Create(k.Name, k.Platform, k.Architecture), v => v)
            .Select(v => v.MaxBy(q => q.Version))
            .ToList()!;
    }

    private PluginInfo CreatePluginInfoFromKey(string key, string baseUri, bool isInstalled = false)
    {
        var info = PluginInfo.CreateFromUniversalName(key);
        info.Uri = baseUri + key;
        info.IsInstalled = isInstalled;
        return info;
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string uri, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetStreamAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
