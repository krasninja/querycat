using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Storage;
using QueryCat.Plugins.Sdk;
using QueryCat.Backend.Utils;
using VariantValue = QueryCat.Backend.Types.VariantValue;

namespace QueryCat.Plugins.Client;

public sealed class ThriftInputConfigStorage : IInputConfigStorage
{
    private readonly PluginsManager.Client _client;
    private readonly Dictionary<string, VariantValue> _objectsStorage = new();
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<ThriftInputConfigStorage>();

    public ThriftInputConfigStorage(PluginsManager.Client client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public void Set(string key, VariantValue value)
    {
        _logger.LogDebug("Set {Key} with value {Value}.", key, value);
        Console.WriteLine("Set " + key + $" {value}");
        if (value.GetInternalType() == Backend.Types.DataType.Object)
        {
            _objectsStorage[key] = value;
        }
        else
        {
            AsyncUtils.RunSync(() => _client.SetConfigValueAsync(key, SdkConvert.Convert(value)));
        }
    }

    /// <inheritdoc />
    public void Unset(string key)
    {
        _objectsStorage.Remove(key);
        AsyncUtils.RunSync(() => _client.SetConfigValueAsync(key, SdkConvert.Convert(VariantValue.Null)));
    }

    /// <inheritdoc />
    public bool Has(string key) => Get(key).IsNull;

    /// <inheritdoc />
    public VariantValue Get(string key)
    {
        _logger.LogDebug("Get {Key}.", key);
        Console.WriteLine("Get " + key);
        if (_objectsStorage.TryGetValue(key, out var objectValue))
        {
            return objectValue;
        }

        var value = AsyncUtils.RunSync(() => _client.GetConfigValueAsync(key));
        if (value == null)
        {
            return VariantValue.Null;
        }
        return SdkConvert.Convert(value);
    }

    /// <inheritdoc />
    public Task SaveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task LoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
