using System.Xml;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Backend.PluginsManager;

/// <summary>
/// Query S3 compatible storage (Yandex Cloud by default) to fetch plugins.
/// </summary>
internal sealed class S3PluginsStorage : IPluginsStorage, IDisposable
{
    private const string PluginsStorageUri = @"https://querycat.storage.yandexcloud.net/";

    private readonly HttpClient _httpClient = new();
    private readonly string _bucketUri;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(S3PluginsStorage));

    public S3PluginsStorage(string? bucketUri = null)
    {
        _bucketUri = !string.IsNullOrEmpty(bucketUri) ? bucketUri : PluginsStorageUri;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PluginInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = await _httpClient.GetStreamAsync(_bucketUri, cancellationToken)
            .ConfigureAwait(false);
        using var xmlReader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            Async = true,
        });
        var xmlKeys = new List<string>();
        var isTruncated = false;
        while (await xmlReader.ReadAsync().ConfigureAwait(false))
        {
            if (xmlReader.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            if (xmlReader.LocalName == "Key")
            {
                xmlKeys.Add(await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false));
            }
            else if (xmlReader.LocalName == "IsTruncated")
            {
                isTruncated = bool.Parse(await xmlReader.ReadElementContentAsStringAsync().ConfigureAwait(false));
            }
        }
        if (isTruncated)
        {
            _logger.LogWarning("Plugins listing is truncated, not all plugins are shown.");
        }
        // Select only latest version.
        var plugins = xmlKeys
            .Where(k => !k.EndsWith('/'))
            .Select(k => DefaultPluginsManager.CreatePluginInfoFromKey(k, _bucketUri));
        return PluginInfo.FilterOnlyLatest(plugins).ToList();
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
