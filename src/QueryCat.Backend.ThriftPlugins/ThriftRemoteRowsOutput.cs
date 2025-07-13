using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class ThriftRemoteRowsOutput : IRowsOutput
{
    private readonly ThriftPluginContext _context;
    private readonly int _objectHandle;
    private readonly string _id;

    private QueryContext _queryContext = NullQueryContext.Instance;

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _queryContext;
        set
        {
            _queryContext = value;
            SendContextToPlugin();
        }
    }

    /// <inheritdoc />
    public RowsOutputOptions Options { get; } = new();

    public ThriftRemoteRowsOutput(ThriftPluginContext context, int objectHandle, string? id = null)
    {
        _context = context;
        _objectHandle = objectHandle;
        _id = id ?? string.Empty;
    }

    private void SendContextToPlugin()
    {
        AsyncUtils.RunSync(async ct =>
        {
            using var session = await _context.GetSessionAsync(ct);
            await session.ClientProxy.RowsSet_SetContextAsync(
                _objectHandle,
                new ContextQueryInfo
                {
                    Columns = QueryContext.QueryInfo.Columns.Select(SdkConvert.Convert).ToList(),
                    Limit = QueryContext.QueryInfo.Limit ?? -1,
                    Offset = QueryContext.QueryInfo.Offset,
                },
                new ContextInfo
                {
                    PrereadRowsCount = QueryContext.PrereadRowsCount,
                    SkipIfNoColumns = QueryContext.SkipIfNoColumns,
                },
                ct
            );
        });
    }

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        using var session = await _context.GetSessionAsync(cancellationToken);
        await session.ClientProxy.RowsSet_OpenAsync(_objectHandle, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        using var session = await _context.GetSessionAsync(cancellationToken);
        await session.ClientProxy.RowsSet_CloseAsync(_objectHandle, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        using var session = await _context.GetSessionAsync(cancellationToken);
        await session.ClientProxy.RowsSet_ResetAsync(_objectHandle, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        var valuesArray = values.ToArray();
        using var session = await _context.GetSessionAsync(cancellationToken);
        var result = await session.ClientProxy.RowsSet_WriteValuesAsync(
            _objectHandle,
            valuesArray.Select(SdkConvert.Convert).ToList(),
            cancellationToken);
        return SdkConvert.Convert(result);
    }

    /// <inheritdoc />
    public override string ToString() => $"Id = {_id}";
}
