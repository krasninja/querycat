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
    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(ShutdownAsync));
        return _client.ShutdownAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<Column>> RowsSet_GetColumnsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetColumnsAsync));
        return _client.RowsSet_GetColumnsAsync(token, object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_OpenAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_OpenAsync));
        return _client.RowsSet_OpenAsync(token, object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_CloseAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_CloseAsync));
        return _client.RowsSet_CloseAsync(token, object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_ResetAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_ResetAsync));
        return _client.RowsSet_ResetAsync(token, object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RowsSet_PositionAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_PositionAsync));
        return _client.RowsSet_PositionAsync(token, object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RowsSet_TotalRowsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_TotalRowsAsync));
        return _client.RowsSet_TotalRowsAsync(token, object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_SeekAsync(long token, int object_rows_set_handle, int offset, CursorSeekOrigin origin,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_SeekAsync));
        return _client.RowsSet_SeekAsync(token, object_rows_set_handle, offset, origin, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_SetContextAsync(long token, int object_rows_set_handle, ContextQueryInfo? context_query_info,
        ContextInfo? context_info, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_SetContextAsync));
        return _client.RowsSet_SetContextAsync(token, object_rows_set_handle, context_query_info, context_info, cancellationToken);
    }

    /// <inheritdoc />
    public Task<RowsList> RowsSet_GetRowsAsync(long token, int object_rows_set_handle, int count, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetRowsAsync));
        return _client.RowsSet_GetRowsAsync(token, object_rows_set_handle, count, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<string>> RowsSet_GetUniqueKeyAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetUniqueKeyAsync));
        return _client.RowsSet_GetUniqueKeyAsync(token, object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetKeyColumnsAsync));
        return _client.RowsSet_GetKeyColumnsAsync(token, object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_SetKeyColumnValueAsync(long token, int object_rows_set_handle, int column_index, string operation, VariantValue? value,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_SetKeyColumnValueAsync));
        return _client.RowsSet_SetKeyColumnValueAsync(token, object_rows_set_handle, column_index, operation, value, cancellationToken);
    }

    /// <inheritdoc />
    public Task RowsSet_UnsetKeyColumnValueAsync(long token, int object_rows_set_handle, int column_index, string operation,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_UnsetKeyColumnValueAsync));
        return _client.RowsSet_UnsetKeyColumnValueAsync(token, object_rows_set_handle, column_index, operation, cancellationToken);
    }

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_UpdateValueAsync(long token, int object_rows_set_handle, int column_index, VariantValue? value,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_UpdateValueAsync));
        return _client.RowsSet_UpdateValueAsync(token, object_rows_set_handle, column_index, value, cancellationToken);
    }

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_WriteValuesAsync(long token, int object_rows_set_handle, List<VariantValue>? values, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_WriteValuesAsync));
        return _client.RowsSet_WriteValuesAsync(token, object_rows_set_handle, values, cancellationToken);
    }

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_DeleteRowAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_DeleteRowAsync));
        return _client.RowsSet_DeleteRowAsync(token, object_rows_set_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ModelDescription> RowsSet_GetDescriptionAsync(long token, int object_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsSet_GetDescriptionAsync));
        return _client.RowsSet_GetDescriptionAsync(token, object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RowsFormatter_OpenInputAsync(long token, int object_rows_formatter_handle, int object_blob_handle, string? key, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsFormatter_OpenInputAsync));
        return _client.RowsFormatter_OpenInputAsync(token, object_rows_formatter_handle, object_blob_handle, key, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RowsFormatter_OpenOutputAsync(long token, int object_rows_formatter_handle, int object_blob_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(RowsFormatter_OpenOutputAsync));
        return _client.RowsFormatter_OpenOutputAsync(token, object_rows_formatter_handle, object_blob_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<VariantValue> CallFunctionAsync(long token, string function_name, FunctionCallArguments? call_args, int object_handle,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(CallFunctionAsync));
        return _client.CallFunctionAsync(token, function_name, call_args, object_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<byte[]> Blob_ReadAsync(long token, int object_blob_handle, int offset, int count, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_ReadAsync));
        return _client.Blob_ReadAsync(token, object_blob_handle, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> Blob_WriteAsync(long token, int object_blob_handle, byte[] bytes, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_WriteAsync));
        return _client.Blob_WriteAsync(token, object_blob_handle, bytes, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> Blob_GetLengthAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_GetLengthAsync));
        return _client.Blob_GetLengthAsync(token, object_blob_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> Blob_GetContentTypeAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_GetContentTypeAsync));
        return _client.Blob_GetContentTypeAsync(token,object_blob_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> Blob_GetNameAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Blob_GetNameAsync));
        return _client.Blob_GetNameAsync(token, object_blob_handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ServeAsync(CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(ServeAsync));
        return _client.ServeAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<QuestionResponse> AnswerAgent_AskAsync(long token, int object_answer_agent_handle, QuestionRequest? request,
        CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(AnswerAgent_AskAsync));
        return _client.AnswerAgent_AskAsync(token, object_answer_agent_handle, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task Thread_CloseHandleAsync(long token, int handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Thread_CloseHandleAsync));
        return _client.Thread_CloseHandleAsync(token, handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ObjectValue> Thread_GetHandleInfoAsync(long token, int handle, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Thread_GetHandleInfoAsync));
        return _client.Thread_GetHandleInfoAsync(token, handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ObjectValue> Thread_GetHandleFromVariableAsync(long token, string name, CancellationToken cancellationToken = default)
    {
        LogStartMethodCall(nameof(Thread_GetHandleFromVariableAsync));
        return _client.Thread_GetHandleFromVariableAsync(token, name, cancellationToken);
    }

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Trace, "Start call remote method '{MethodName}'.")]
    private partial void LogStartMethodCall(string methodName);

    [LoggerMessage(Microsoft.Extensions.Logging.LogLevel.Trace, "Finish call remote method '{MethodName}'.")]
    private partial void LogEndMethodCall(string methodName);
}
