using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class ThriftRemoteRowsFormatter : IRowsFormatter
{
    private readonly ThriftPluginContext _context;
    private readonly int _objectHandle;

    public ThriftRemoteRowsFormatter(ThriftPluginContext context, int objectHandle)
    {
        _context = context;
        _objectHandle = objectHandle;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input, string? key = null)
    {
        var result = AsyncUtils.RunSync(async ct =>
        {
            using var session = await _context.GetSessionAsync(ct);
            // TODO:
            var rowsInput = await session.ClientProxy.RowsFormatter_OpenInputAsync(_objectHandle, -1, key, ct);
            return new ThriftRemoteRowsIterator(_context, rowsInput);
        });
        if (result == null)
        {
            throw new InvalidOperationException(Resources.Errors.HandlerInternalError);
        }
        return result;
    }

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
    {
        var result = AsyncUtils.RunSync(async ct =>
        {
            using var session = await _context.GetSessionAsync(ct);
            // TODO:
            var rowsInput = await session.ClientProxy.RowsFormatter_OpenOutputAsync(_objectHandle, -1, ct);
            return new ThriftRemoteRowsOutput(_context, rowsInput);
        });
        if (result == null)
        {
            throw new InvalidOperationException(Resources.Errors.HandlerInternalError);
        }
        return result;
    }
}
