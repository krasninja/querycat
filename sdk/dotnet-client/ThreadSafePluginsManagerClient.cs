using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using QueryCat.Plugins.Sdk;

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
