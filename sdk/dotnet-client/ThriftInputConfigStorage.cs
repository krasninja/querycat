using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Sdk;
using DataType = QueryCat.Backend.Core.Types.DataType;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Config storage that calls Thrift host to store variables. Object variables are saved locally.
/// </summary>
public sealed class ThriftInputConfigStorage : IInputConfigStorage
{
    private readonly PluginsManager.Client _client;
    private readonly Dictionary<string, VariantValue> _objectsStorage = new();
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftInputConfigStorage));

    public ThriftInputConfigStorage(PluginsManager.Client client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public void Set(string key, VariantValue value)
    {
        _logger.LogDebug("Set '{Key}' with value '{Value}'.", key, value);
        if (value.Type == DataType.Object)
        {
            _objectsStorage[key] = value;
        }
        else
        {
            if (value.IsNull)
            {
                _objectsStorage.Remove(key);
            }
            AsyncUtils.RunSync(ct => _client.SetConfigValueAsync(key, SdkConvert.Convert(value), ct));
        }
    }

    /// <inheritdoc />
    public bool Has(string key) => !Get(key).IsNull;

    /// <inheritdoc />
    public VariantValue Get(string key)
    {
        _logger.LogDebug("Get '{Key}'.", key);
        if (_objectsStorage.TryGetValue(key, out var objectValue))
        {
            return objectValue;
        }

        var value = AsyncUtils.RunSync(ct => _client.GetConfigValueAsync(key, ct));
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
