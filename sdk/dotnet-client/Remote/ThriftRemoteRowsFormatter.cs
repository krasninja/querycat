using System;
using System.Threading.Tasks;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Plugins.Client.Remote;

public sealed class ThriftRemoteRowsFormatter : IRowsFormatter, IAsyncDisposable
{
    /*
     * Remote iteration scheme:
     *
     * | Serve                                          | Client
     * |       CallFunction('json_formatter') ->        |
     * |      <- object_rows_formatter_handle           |
     * |                                                |
     * |     RowsFormatter_OpenInput (                  |
     * |        object_rows_formatter_handle,           |
     * |        object_blob_handle) ->                  |
     * |            <- RowsInputHandle                  |
     */

    private readonly IThriftSessionProvider _sessionProvider;
    private readonly ObjectsStorage _objectsStorage;
    private readonly int _objectHandle;
    private readonly long _token;

    public ThriftRemoteRowsFormatter(
        IThriftSessionProvider sessionProvider,
        ObjectsStorage objectsStorage,
        int objectHandle,
        long token = 0)
    {
        _sessionProvider = sessionProvider;
        _objectsStorage = objectsStorage;
        _objectHandle = objectHandle;
        _token = token;
    }

    public ThriftRemoteRowsFormatter(
        ThriftPluginClient pluginClient,
        ObjectsStorage objectsStorage,
        int objectHandle)
        : this(new SimpleThriftSessionProvider(pluginClient.ThriftClient), objectsStorage, objectHandle, pluginClient.Token)
    {
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null)
    {
        var result = AsyncUtils.RunSync(async ct =>
        {
            var blobHandle = _objectsStorage.GetOrAdd(blob);
            using var session = await _sessionProvider.GetAsync(ct);
            var rowsInput = await session.Client.RowsFormatter_OpenInputAsync(_token, _objectHandle, blobHandle, key, ct);
            return new ThriftRemoteRowsInput(_sessionProvider, rowsInput);
        });
        if (result == null)
        {
            throw new InvalidOperationException(Resources.Errors.HandlerInternalError);
        }
        return result;
    }

    /// <inheritdoc />
    public IRowsOutput OpenOutput(IBlobData blob)
    {
        var result = AsyncUtils.RunSync(async ct =>
        {
            var blobHandle = _objectsStorage.GetOrAdd(blob);
            using var session = await _sessionProvider.GetAsync(ct);
            var rowsInput = await session.Client.RowsFormatter_OpenOutputAsync(_token, _objectHandle, blobHandle, ct);
            return new ThriftRemoteRowsOutput(_sessionProvider, rowsInput);
        });
        if (result == null)
        {
            throw new InvalidOperationException(Resources.Errors.HandlerInternalError);
        }
        return result;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        using var session = await _sessionProvider.GetAsync();
        await session.Client.Thread_CloseHandleAsync(_token, _objectHandle);
    }
}
