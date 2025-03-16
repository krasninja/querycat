using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class ThriftRemoteRowsFormatter : IRowsFormatter
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

    private readonly ThriftPluginContext _context;
    private readonly int _objectHandle;

    public ThriftRemoteRowsFormatter(ThriftPluginContext context, int objectHandle)
    {
        _context = context;
        _objectHandle = objectHandle;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null)
    {
        var result = AsyncUtils.RunSync(async ct =>
        {
            using var session = await _context.GetSessionAsync(ct);
            var blobHandle = _context.ObjectsStorage.GetOrAdd(blob);
            var rowsInput = await session.ClientProxy.RowsFormatter_OpenInputAsync(_objectHandle, blobHandle, key, ct);
            return new ThriftRemoteRowsIterator(_context, rowsInput);
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
            using var session = await _context.GetSessionAsync(ct);
            var blobHandle = _context.ObjectsStorage.GetOrAdd(blob);
            var rowsInput = await session.ClientProxy.RowsFormatter_OpenOutputAsync(_objectHandle, blobHandle, ct);
            return new ThriftRemoteRowsOutput(_context, rowsInput);
        });
        if (result == null)
        {
            throw new InvalidOperationException(Resources.Errors.HandlerInternalError);
        }
        return result;
    }
}
