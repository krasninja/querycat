using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Persistent config storage. The content is saved between program runs.
/// </summary>
public class PersistentConfigStorage : MemoryConfigStorage
{
    private readonly string _configFile;
    private int _writesCount;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(PersistentConfigStorage));

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="configFile">Configuration file.</param>
    public PersistentConfigStorage(string configFile)
    {
        _configFile = configFile;
    }

    /// <inheritdoc />
    public override ValueTask SetAsync(string key, VariantValue value, CancellationToken cancellationToken = default)
    {
        _writesCount++;
        return base.SetAsync(key, value, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (_writesCount < 1)
        {
            return;
        }

        var directory = Path.GetDirectoryName(_configFile);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var dict = new ConfigDictionary();
        foreach (var variantValueWithKey in Map)
        {
            if (!DataTypeUtils.IsSimple(variantValueWithKey.Value.Type))
            {
                _logger.LogTrace("Skip saving config with key '{Key}'. Only simple types save is supported.",
                    variantValueWithKey.Key);
            }
            var type = variantValueWithKey.Value.Type;
            if (!DataTypeUtils.IsSimple(type))
            {
                continue;
            }

            dict[variantValueWithKey.Key] = DataTypeUtils.SerializeVariantValue(variantValueWithKey.Value);
        }

        var json = JsonSerializer.Serialize(dict, SourceGenerationContext.Default.ConfigDictionary);
        await File.WriteAllTextAsync(_configFile, json, CancellationToken.None);

        _writesCount = 0;
    }

    /// <inheritdoc />
    public override async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configFile))
        {
            return;
        }

        var dict = JsonSerializer.Deserialize(
            await File.ReadAllTextAsync(_configFile, cancellationToken),
            SourceGenerationContext.Default.ConfigDictionary);
        if (dict == null)
        {
            return;
        }
        foreach (var keyValue in dict)
        {
            Map[keyValue.Key] = DataTypeUtils.DeserializeVariantValue(keyValue.Value);
        }
    }
}
