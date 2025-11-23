using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using QueryCat.Plugins.Sdk;
using CompletionResult = QueryCat.Plugins.Sdk.CompletionResult;
using QuestionRequest = QueryCat.Plugins.Sdk.QuestionRequest;
using QuestionResponse = QueryCat.Plugins.Sdk.QuestionResponse;

namespace QueryCat.Plugins.Client;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal sealed class ThreadSafePluginsManagerClient : PluginsManager.IAsync
{
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly PluginsManager.IAsync _client;

    public ThreadSafePluginsManagerClient(PluginsManager.IAsync client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<RegistrationResult> RegisterPluginAsync(string registration_token, string callback_uri, PluginData? plugin_data,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RegisterPluginAsync(registration_token, callback_uri, plugin_data, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task PluginReadyAsync(long token, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.PluginReadyAsync(token, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<VariantValue> CallFunctionAsync(long token, string function_name, List<VariantValue>? args, int object_handle,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.CallFunctionAsync(token, function_name, args, object_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<VariantValue> RunQueryAsync(long token, string query, Dictionary<string, VariantValue>? parameters, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RunQueryAsync(token, query, parameters, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetConfigValueAsync(long token, string key, VariantValue? value, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.SetConfigValueAsync(token, key, value, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<VariantValue> GetConfigValueAsync(long token, string key, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.GetConfigValueAsync(token, key, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<VariantValue> GetVariableAsync(long token, string name, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.GetVariableAsync(token, name, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<VariantValue> SetVariableAsync(long token, string name, VariantValue? value, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.SetVariableAsync(token, name, value, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<List<ScopeVariable>> GetVariablesAsync(long token, int scope_id, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.GetVariablesAsync(token, scope_id, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ExecutionScope> PushScopeAsync(long token, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.PushScopeAsync(token, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ExecutionScope> PopScopeAsync(long token, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.PopScopeAsync(token, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ExecutionScope> PeekTopScopeAsync(long token, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.PeekTopScopeAsync(token, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<List<CompletionResult>> GetCompletionsAsync(long token, string text, int position, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.GetCompletionsAsync(token, text, position, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> Blob_ReadAsync(long token, int object_blob_handle, int offset, int count,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.Blob_ReadAsync(token, object_blob_handle, offset, count, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<long> Blob_WriteAsync(long token, int object_blob_handle, byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.Blob_WriteAsync(token, object_blob_handle, bytes, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<long> Blob_GetLengthAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.Blob_GetLengthAsync(token, object_blob_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> Blob_GetContentTypeAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.Blob_GetContentTypeAsync(token, object_blob_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> Blob_GetNameAsync(long token, int object_blob_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.Blob_GetNameAsync(token, object_blob_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<List<Column>> RowsSet_GetColumnsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsSet_GetColumnsAsync(token, object_rows_set_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RowsSet_OpenAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.RowsSet_OpenAsync(token, object_rows_set_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RowsSet_CloseAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.RowsSet_CloseAsync(token, object_rows_set_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RowsSet_ResetAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.RowsSet_ResetAsync(token, object_rows_set_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RowsSet_SetContextAsync(long token, int object_rows_set_handle, ContextQueryInfo? context_query_info,
        ContextInfo? context_info, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.RowsSet_SetContextAsync(token, object_rows_set_handle,
                context_query_info, context_info, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> RowsSet_PositionAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsSet_PositionAsync(token, object_rows_set_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> RowsSet_TotalRowsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsSet_TotalRowsAsync(token, object_rows_set_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RowsSet_SeekAsync(long token, int object_rows_set_handle, int offset, CursorSeekOrigin origin,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.RowsSet_SeekAsync(token, object_rows_set_handle, offset, origin, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RowsList> RowsSet_GetRowsAsync(long token, int object_rows_set_handle, int count,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsSet_GetRowsAsync(token, object_rows_set_handle, count, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> RowsSet_GetUniqueKeyAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsSet_GetUniqueKeyAsync(token, object_rows_set_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<List<KeyColumn>> RowsSet_GetKeyColumnsAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsSet_GetKeyColumnsAsync(token, object_rows_set_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RowsSet_SetKeyColumnValueAsync(long token, int object_rows_set_handle, int column_index, string operation,
        VariantValue? value, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.RowsSet_SetKeyColumnValueAsync(
                token, object_rows_set_handle, column_index, operation, value, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RowsSet_UnsetKeyColumnValueAsync(long token, int object_rows_set_handle, int column_index, string operation,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.RowsSet_UnsetKeyColumnValueAsync(
                token, object_rows_set_handle, column_index, operation, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<QueryCatErrorCode> RowsSet_UpdateValueAsync(long token, int object_rows_set_handle, int column_index, VariantValue? value,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsSet_UpdateValueAsync(
                token, object_rows_set_handle, column_index, value, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<QueryCatErrorCode> RowsSet_WriteValuesAsync(long token, int object_rows_set_handle, List<VariantValue>? values,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsSet_WriteValuesAsync(token, object_rows_set_handle, values, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<QueryCatErrorCode> RowsSet_DeleteRowAsync(long token, int object_rows_set_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsSet_DeleteRowAsync(token, object_rows_set_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ModelDescription> RowsSet_GetDescriptionAsync(long token, int object_handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsSet_GetDescriptionAsync(token, object_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> RowsFormatter_OpenInputAsync(long token, int object_rows_formatter_handle, int object_blob_handle, string? key,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsFormatter_OpenInputAsync(token, object_rows_formatter_handle, object_blob_handle, key, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> RowsFormatter_OpenOutputAsync(long token, int object_rows_formatter_handle, int object_blob_handle,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.RowsFormatter_OpenOutputAsync(token, object_rows_formatter_handle, object_blob_handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<QuestionResponse> AnswerAgent_AskAsync(long token, int object_answer_agent_handle, QuestionRequest? request,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.AnswerAgent_AskAsync(token, object_answer_agent_handle, request, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task Thread_CloseHandleAsync(long token, int handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.Thread_CloseHandleAsync(token, handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ObjectValue> Thread_GetHandleInfoAsync(long token, int handle, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.Thread_GetHandleInfoAsync(token, handle, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ObjectValue> Thread_GetHandleFromVariableAsync(long token, string name, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.Thread_GetHandleFromVariableAsync(token, name, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task LogAsync(long token, LogLevel level, string message, List<string>? arguments,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await _client.LogAsync(token, level, message, arguments, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Statistic> GetStatisticAsync(long token, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _client.GetStatisticAsync(token, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
