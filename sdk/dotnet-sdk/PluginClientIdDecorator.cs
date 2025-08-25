using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace QueryCat.Plugins.Sdk;

/// <summary>
/// Decorator for <see cref="Plugin.Client" /> with identifier.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed class PluginClientIdDecorator : Plugin.IAsync
{
    private static int _nextId;

    private readonly Plugin.IAsync _client;
    private readonly int _id;

    public int Id => _id;

    public PluginClientIdDecorator(Plugin.IAsync client)
    {
        _client = client;
        _id = Interlocked.Increment(ref _nextId);
    }

    /// <inheritdoc />
    public Task<VariantValue> CallFunctionAsync(string function_name, List<VariantValue>? args, int object_handle,
        CancellationToken cancellationToken = default)
        => _client.CallFunctionAsync(function_name, args, object_handle, cancellationToken);

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken cancellationToken = default)
        => _client.ShutdownAsync(cancellationToken);

    /// <inheritdoc />
    public Task<List<Column>> RowsSet_GetColumnsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        => _client.RowsSet_GetColumnsAsync(object_rows_set_handle, cancellationToken);

    /// <inheritdoc />
    public Task RowsSet_OpenAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        => _client.RowsSet_OpenAsync(object_rows_set_handle, cancellationToken);

    /// <inheritdoc />
    public Task RowsSet_CloseAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        => _client.RowsSet_CloseAsync(object_rows_set_handle, cancellationToken);

    /// <inheritdoc />
    public Task RowsSet_ResetAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        => _client.RowsSet_ResetAsync(object_rows_set_handle, cancellationToken);

    /// <inheritdoc />
    public Task<int> RowsSet_PositionAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        => _client.RowsSet_PositionAsync(object_rows_set_handle, cancellationToken);

    /// <inheritdoc />
    public Task<int> RowsSet_TotalRowsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        => _client.RowsSet_TotalRowsAsync(object_rows_set_handle, cancellationToken);

    /// <inheritdoc />
    public Task RowsSet_SeekAsync(int object_rows_set_handle, int offset, CursorSeekOrigin origin,
        CancellationToken cancellationToken = default)
        => _client.RowsSet_SeekAsync(object_rows_set_handle, offset, origin, cancellationToken);

    /// <inheritdoc />
    public Task RowsSet_SetContextAsync(
        int object_rows_set_handle,
        ContextQueryInfo? context_query_info,
        ContextInfo? context_info,
        CancellationToken cancellationToken = default)
        => _client.RowsSet_SetContextAsync(object_rows_set_handle, context_query_info, context_info, cancellationToken);

    /// <inheritdoc />
    public Task<RowsList> RowsSet_GetRowsAsync(int object_rows_set_handle, int count, CancellationToken cancellationToken = default)
        => _client.RowsSet_GetRowsAsync(object_rows_set_handle, count, cancellationToken);

    /// <inheritdoc />
    public Task<List<string>> RowsSet_GetUniqueKeyAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        => _client.RowsSet_GetUniqueKeyAsync(object_rows_set_handle, cancellationToken);

    /// <inheritdoc />
    public Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        => _client.RowsSet_GetKeyColumnsAsync(object_rows_set_handle, cancellationToken);

    /// <inheritdoc />
    public Task RowsSet_SetKeyColumnValueAsync(int object_rows_set_handle, int column_index, string operation, VariantValue? value,
        CancellationToken cancellationToken = default)
        => _client.RowsSet_SetKeyColumnValueAsync(object_rows_set_handle, column_index, operation, value, cancellationToken);

    /// <inheritdoc />
    public Task RowsSet_UnsetKeyColumnValueAsync(int object_rows_set_handle, int column_index, string operation,
        CancellationToken cancellationToken = default)
        => _client.RowsSet_UnsetKeyColumnValueAsync(object_rows_set_handle, column_index, operation, cancellationToken);

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_UpdateValueAsync(int object_rows_set_handle, int column_index, VariantValue? value,
        CancellationToken cancellationToken = default)
        => _client.RowsSet_UpdateValueAsync(object_rows_set_handle, column_index, value, cancellationToken);

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_WriteValuesAsync(int object_rows_set_handle, List<VariantValue>? values, CancellationToken cancellationToken = default)
        => _client.RowsSet_WriteValuesAsync(object_rows_set_handle, values, cancellationToken);

    /// <inheritdoc />
    public Task<QueryCatErrorCode> RowsSet_DeleteRowAsync(int object_rows_set_handle, CancellationToken cancellationToken = default)
        => _client.RowsSet_DeleteRowAsync(object_rows_set_handle, cancellationToken);

    /// <inheritdoc />
    public Task<ModelDescription> RowsSet_GetDescriptionAsync(int object_handle, CancellationToken cancellationToken = default)
        => _client.RowsSet_GetDescriptionAsync(object_handle, cancellationToken);

    /// <inheritdoc />
    public Task<int> RowsFormatter_OpenInputAsync(int object_rows_formatter_handle, int object_blob_handle, string? key,
        CancellationToken cancellationToken = default)
        => _client.RowsFormatter_OpenInputAsync(object_rows_formatter_handle, object_blob_handle, key, cancellationToken);

    /// <inheritdoc />
    public Task<int> RowsFormatter_OpenOutputAsync(int object_rows_formatter_handle, int object_blob_handle,
        CancellationToken cancellationToken = default)
        => _client.RowsFormatter_OpenOutputAsync(object_rows_formatter_handle, object_blob_handle, cancellationToken);

    /// <inheritdoc />
    public Task<byte[]> Blob_ReadAsync(int object_blob_handle, int offset, int count, CancellationToken cancellationToken = default)
        => _client.Blob_ReadAsync(object_blob_handle, offset, count, cancellationToken);

    /// <inheritdoc />
    public Task<long> Blob_WriteAsync(int object_blob_handle, byte[] bytes, CancellationToken cancellationToken = default)
        => _client.Blob_WriteAsync(object_blob_handle, bytes, cancellationToken);

    /// <inheritdoc />
    public Task<long> Blob_GetLengthAsync(int object_blob_handle, CancellationToken cancellationToken = default)
        => _client.Blob_GetLengthAsync(object_blob_handle, cancellationToken);

    /// <inheritdoc />
    public Task<string> Blob_GetContentTypeAsync(int object_blob_handle, CancellationToken cancellationToken = default)
        => _client.Blob_GetContentTypeAsync(object_blob_handle, cancellationToken);

    /// <inheritdoc />
    public Task<string> Blob_GetNameAsync(int object_blob_handle, CancellationToken cancellationToken = default)
        => _client.Blob_GetNameAsync(object_blob_handle, cancellationToken);

    /// <inheritdoc />
    public Task<string> ServeAsync(CancellationToken cancellationToken = default)
        => _client.ServeAsync(cancellationToken);

    /// <inheritdoc />
    public Task<QuestionResponse> AnswerAgent_AskAsync(int object_answer_agent_handle, QuestionRequest? request,
        CancellationToken cancellationToken = default)
        => _client.AnswerAgent_AskAsync(object_answer_agent_handle, request, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => $"ClientId = {_id}";
}
