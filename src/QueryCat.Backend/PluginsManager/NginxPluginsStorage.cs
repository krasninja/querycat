using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Backend.PluginsManager;

/// <summary>
/// Parse files from Nginx directory listing as plugins storage. Use "autoindex on; autoindex_format json;" options.
/// </summary>
public sealed class NginxPluginsStorage : IPluginsStorage, IDisposable
{
    private const string TypeFile = "file";
    private const string TypeDirectory = "directory";

    private readonly Uri _uri;
    private readonly HttpClient _httpClient = new();

    internal sealed class NginxObjectDto
    {
        /// <summary>
        /// File or directory name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Object type: "file" or "directory".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Modification time.
        /// </summary>
        [JsonPropertyName("mtime")]
        [JsonConverter(typeof(Rfc1123DateTimeConverter))]
        public DateTime ModificationTime { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    internal sealed class Rfc1123DateTimeConverter : JsonConverter<DateTime>
    {
        /// <inheritdoc />
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();

            return DateTime.ParseExact(
                str!,
                "R",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
            );
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture));
        }
    }

    public NginxPluginsStorage(Uri uri)
    {
        _uri = uri;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PluginInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        var dtos = await _httpClient.GetFromJsonAsync(_uri, SourceGenerationContext.Default.IListNginxObjectDto,
            cancellationToken: cancellationToken);
        if (dtos == null)
        {
            return [];
        }
        var list = new List<PluginInfo>();
        foreach (var dto in dtos)
        {
            if (dto.Type != TypeFile)
            {
                continue;
            }
            var plugin = PluginInfo.CreateFromUniversalName(dto.Name);
            plugin.Size = dto.Size;
            plugin.Uri = _uri.AbsoluteUri + dto.Name;
            list.Add(plugin);
        }
        return PluginInfo.FilterOnlyLatest(list).ToList();
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
