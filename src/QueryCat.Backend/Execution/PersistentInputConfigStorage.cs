using System.Text.Json;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Persistent input config storage. The content is saved between program runs.
/// </summary>
public class PersistentInputConfigStorage : MemoryInputConfigStorage
{
    private readonly string _configFile;
    private int _writesCount;

    public PersistentInputConfigStorage(string configFile)
    {
        _configFile = configFile;
    }

    /// <inheritdoc />
    public override void Unset(string key)
    {
        _writesCount++;
        base.Unset(key);
    }

    /// <inheritdoc />
    public override void Set(string key, VariantValue value)
    {
        _writesCount++;
        base.Set(key, value);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
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

        var dict = new Dictionary<string, string>();
        foreach (var variantValueWithKey in Map)
        {
            if (!DataTypeUtils.IsSimple(variantValueWithKey.Value.GetInternalType()))
            {
                Logger.Instance.Trace($"Skip saving config with key '{variantValueWithKey.Key}'. Only simple types save is supported.",
                    nameof(PersistentInputConfigStorage));
            }
            var type = variantValueWithKey.Value.GetInternalType();
            if (!DataTypeUtils.IsSimple(type))
            {
                continue;
            }

            dict[variantValueWithKey.Key] = DataTypeUtils.SerializeVariantValue(variantValueWithKey.Value);
        }

        var json = JsonSerializer.Serialize(dict);
        await File.WriteAllTextAsync(_configFile, json, cancellationToken);

        _writesCount = 0;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configFile))
        {
            return;
        }

        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(
            await File.ReadAllTextAsync(_configFile, cancellationToken));
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
