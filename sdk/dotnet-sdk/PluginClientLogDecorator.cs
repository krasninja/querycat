using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QueryCat.Plugins.Sdk;

/// <summary>
/// Decorator for <see cref="Plugin.Client" /> with methods call logging.
/// </summary>
public sealed class PluginClientLogDecorator : Plugin.IAsync
{
    private readonly Plugin.Client _client;
    private readonly ILogger _logger;

    public PluginClientLogDecorator(Plugin.Client client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public Task OpenTransportAsync(CancellationToken cancellationToken = default) => _client.OpenTransportAsync(cancellationToken);

    /// <inheritdoc />
    public Task<VariantValue> CallFunctionAsync(string function_name, List<VariantValue>? args, int object_handle,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(CallFunctionAsync));
        return _client.CallFunctionAsync(function_name, args, object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(ShutdownAsync));
        return _client.ShutdownAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<Column>> RowsSet_GetColumnsAsync(int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetColumnsAsync));
        return _client.RowsSet_GetColumnsAsync(object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_OpenAsync(int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_OpenAsync));
        return _client.RowsSet_OpenAsync(object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_CloseAsync(int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_CloseAsync));
        return _client.RowsSet_CloseAsync(object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_ResetAsync(int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_ResetAsync));
        return _client.RowsSet_ResetAsync(object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RowsSet_PositionAsync(int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_PositionAsync));
        return _client.RowsSet_PositionAsync(object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RowsSet_TotalRowsAsync(int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_TotalRowsAsync));
        return _client.RowsSet_TotalRowsAsync(object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_SeekAsync(int object_handle, int offset, CursorSeekOrigin origin,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_SeekAsync));
        return _client.RowsSet_SeekAsync(object_handle, offset, origin, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_SetContextAsync(int object_handle, ContextQueryInfo? context_query_info,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_SetContextAsync));
        return _client.RowsSet_SetContextAsync(object_handle, context_query_info, cancellationToken);
    }

    /// <inheritdoc />
    public Task<RowsList> RowsSet_GetRowsAsync(int object_handle, int count, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetRowsAsync));
        return _client.RowsSet_GetRowsAsync(object_handle, count, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<string>> RowsSet_GetUniqueKeyAsync(int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetUniqueKeyAsync));
        return _client.RowsSet_GetUniqueKeyAsync(object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetKeyColumnsAsync));
        return _client.RowsSet_GetKeyColumnsAsync(object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_SetKeyColumnValueAsync(int object_handle, int column_index, string operation, VariantValue? value,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_SetKeyColumnValueAsync));
        return _client.RowsSet_SetKeyColumnValueAsync(object_handle, column_index, operation, value, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_UnsetKeyColumnValueAsync(int object_handle, int column_index, string operation,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_UnsetKeyColumnValueAsync));
        return _client.RowsSet_UnsetKeyColumnValueAsync(object_handle, column_index, operation, cancellationToken);
    }

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_UpdateValueAsync(int object_handle, int column_index, VariantValue? value,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_UpdateValueAsync));
        return _client.RowsSet_UpdateValueAsync(object_handle, column_index, value, cancellationToken);
    }

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_WriteValuesAsync(int object_handle, List<VariantValue>? values, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_WriteValuesAsync));
        return _client.RowsSet_WriteValuesAsync(object_handle, values, cancellationToken);
    }

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_DeleteRowAsync(int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_DeleteRowAsync));
        return _client.RowsSet_DeleteRowAsync(object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<byte[]> Blob_ReadAsync(int object_handle, int offset, int count, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_ReadAsync));
        return _client.Blob_ReadAsync(object_handle, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> Blob_GetLengthAsync(int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_GetLengthAsync));
        return _client.Blob_GetLengthAsync(object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> OfferConnectionAsync(string uri, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(OfferConnectionAsync));
        return _client.OfferConnectionAsync(uri, cancellationToken);
    }

    private void LogStartMethodCall(string methodName) => _logger.LogTrace("Call remote method '{MethodName}'.", methodName);

    private void LogEndMethodCall(string methodName) => _logger.LogTrace("Call end remote method '{MethodName}'.", methodName);
}