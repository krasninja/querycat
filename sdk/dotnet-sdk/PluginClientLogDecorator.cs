using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QueryCat.Plugins.Sdk;

/// <summary>
/// Decorator for <see cref="Plugin.Client" /> with methods call logging.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class PluginClientLogDecorator : Plugin.IAsync
{
    private readonly Plugin.Client _client;
    private readonly ILogger _logger;

    public PluginClientLogDecorator(Plugin.Client client, ILoggerFactory loggerFactory)
    {
        _client = client;
        _logger = loggerFactory.CreateLogger(nameof(PluginClientLogDecorator));
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
    public Task<List<Column>> RowsSet_GetColumnsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetColumnsAsync));
        return _client.RowsSet_GetColumnsAsync(object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_OpenAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_OpenAsync));
        return _client.RowsSet_OpenAsync(object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_CloseAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_CloseAsync));
        return _client.RowsSet_CloseAsync(object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_ResetAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_ResetAsync));
        return _client.RowsSet_ResetAsync(object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RowsSet_PositionAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_PositionAsync));
        return _client.RowsSet_PositionAsync(object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RowsSet_TotalRowsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_TotalRowsAsync));
        return _client.RowsSet_TotalRowsAsync(object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_SeekAsync(int object_rows_set_handle, int offset, CursorSeekOrigin origin,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_SeekAsync));
        return _client.RowsSet_SeekAsync(object_rows_set_handle, offset, origin, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_SetContextAsync(int object_rows_set_handle, ContextQueryInfo? context_query_info,
        ContextInfo? context_info, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_SetContextAsync));
        return _client.RowsSet_SetContextAsync(object_rows_set_handle, context_query_info, context_info, cancellationToken);
    }

    /// <inheritdoc />
    public Task<RowsList> RowsSet_GetRowsAsync(int object_rows_set_handle, int count, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetRowsAsync));
        return _client.RowsSet_GetRowsAsync(object_rows_set_handle, count, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<string>> RowsSet_GetUniqueKeyAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetUniqueKeyAsync));
        return _client.RowsSet_GetUniqueKeyAsync(object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetKeyColumnsAsync));
        return _client.RowsSet_GetKeyColumnsAsync(object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_SetKeyColumnValueAsync(int object_rows_set_handle, int column_index, string operation, VariantValue? value,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_SetKeyColumnValueAsync));
        return _client.RowsSet_SetKeyColumnValueAsync(object_rows_set_handle, column_index, operation, value, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_UnsetKeyColumnValueAsync(int object_rows_set_handle, int column_index, string operation,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_UnsetKeyColumnValueAsync));
        return _client.RowsSet_UnsetKeyColumnValueAsync(object_rows_set_handle, column_index, operation, cancellationToken);
    }

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_UpdateValueAsync(int object_rows_set_handle, int column_index, VariantValue? value,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_UpdateValueAsync));
        return _client.RowsSet_UpdateValueAsync(object_rows_set_handle, column_index, value, cancellationToken);
    }

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_WriteValuesAsync(int object_rows_set_handle, List<VariantValue>? values, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_WriteValuesAsync));
        return _client.RowsSet_WriteValuesAsync(object_rows_set_handle, values, cancellationToken);
    }

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_DeleteRowAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_DeleteRowAsync));
        return _client.RowsSet_DeleteRowAsync(object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ModelDescription> RowsSet_GetDescriptionAsync(int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetDescriptionAsync));
        return _client.RowsSet_GetDescriptionAsync(object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RowsFormatter_OpenInputAsync(int object_rows_formatter_handle, int object_blob_handle, string? key, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsFormatter_OpenInputAsync));
        return _client.RowsFormatter_OpenInputAsync(object_rows_formatter_handle, object_blob_handle, key, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RowsFormatter_OpenOutputAsync(int object_rows_formatter_handle, int object_blob_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsFormatter_OpenOutputAsync));
        return _client.RowsFormatter_OpenOutputAsync(object_rows_formatter_handle, object_blob_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<byte[]> Blob_ReadAsync(int object_blob_handle, int offset, int count, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_ReadAsync));
        return _client.Blob_ReadAsync(object_blob_handle, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> Blob_WriteAsync(int object_blob_handle, byte[] bytes, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_WriteAsync));
        return _client.Blob_WriteAsync(object_blob_handle, bytes, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> Blob_GetLengthAsync(int object_blob_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_GetLengthAsync));
        return _client.Blob_GetLengthAsync(object_blob_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> Blob_GetContentTypeAsync(int object_blob_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_GetContentTypeAsync));
        return _client.Blob_GetContentTypeAsync(object_blob_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> Blob_GetNameAsync(int object_blob_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_GetNameAsync));
        return _client.Blob_GetNameAsync(object_blob_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ServeAsync(CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(ServeAsync));
        return _client.ServeAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<QuestionResponse> AnswerAgent_AskAsync(int object_answer_agent_handle, QuestionRequest? request,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(AnswerAgent_AskAsync));
        return _client.AnswerAgent_AskAsync(object_answer_agent_handle, request, cancellationToken);
    }

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Trace, "Start call remote method '{MethodName}'.")]
    private partial void LogStartMethodCall(string methodName);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Trace, "Finish call remote method '{MethodName}'.")]
    private partial void LogEndMethodCall(string methodName);
}
