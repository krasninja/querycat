using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using DataType = QueryCat.Backend.Core.Types.DataType;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Config storage that calls Thrift host to store variables. Object variables are saved locally.
/// </summary>
public sealed class ThriftInputConfigStorage : IInputConfigStorage
{
    private readonly ThriftPluginClient _client;
    private readonly Dictionary<string, VariantValue> _objectsStorage = new();
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ThriftInputConfigStorage));

    public ThriftInputConfigStorage(ThriftPluginClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async ValueTask SetAsync(string key, VariantValue value, CancellationToken cancellationToken = default)
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
            await _client.ThriftClient.SetConfigValueAsync(_client.Token, key, SdkConvert.Convert(value), cancellationToken);
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasAsync(string key, CancellationToken cancellationToken = default)
        => !(await GetAsync(key, cancellationToken)).IsNull;

    /// <inheritdoc />
    public async ValueTask<VariantValue> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Get '{Key}'.", key);
        if (_objectsStorage.TryGetValue(key, out var objectValue))
        {
            return objectValue;
        }

        var value = await _client.ThriftClient.GetConfigValueAsync(_client.Token, key, cancellationToken);
        return SdkConvert.Convert(value);
    }

    /// <inheritdoc />
    public Task SaveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task LoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
