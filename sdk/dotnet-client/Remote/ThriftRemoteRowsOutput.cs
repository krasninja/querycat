using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Sdk;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Plugins.Client.Remote;

public sealed class ThriftRemoteRowsOutput : IRowsOutput
{
    private readonly IThriftSessionProvider _sessionProvider;
    private readonly int _objectHandle;
    private readonly long _token;
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

    public ThriftRemoteRowsOutput(IThriftSessionProvider sessionProvider, int objectHandle, string? id = null, long token = 0)
    {
        _sessionProvider = sessionProvider;
        _objectHandle = objectHandle;
        _token = token;
        _id = id ?? string.Empty;
    }

    public ThriftRemoteRowsOutput(ThriftPluginClient pluginClient, int objectHandle, string? id = null)
        : this(new SimpleThriftSessionProvider(pluginClient.ThriftClient), objectHandle, id, pluginClient.Token)
    {
    }

    private void SendContextToPlugin()
    {
        AsyncUtils.RunSync(async ct =>
        {
            using var session = await _sessionProvider.GetAsync(ct);
            await session.Client.RowsSet_SetContextAsync(
                _token,
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
        using var session = await _sessionProvider.GetAsync(cancellationToken);
        await session.Client.RowsSet_OpenAsync(_token, _objectHandle, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        using var session = await _sessionProvider.GetAsync(cancellationToken);
        await session.Client.RowsSet_CloseAsync(_token, _objectHandle, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        using var session = await _sessionProvider.GetAsync(cancellationToken);
        await session.Client.RowsSet_ResetAsync(_token, _objectHandle, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        var valuesArray = values.ToArray();
        using var session = await _sessionProvider.GetAsync(cancellationToken);
        var result = await session.Client.RowsSet_WriteValuesAsync(
            _token,
            _objectHandle,
            valuesArray.Select(SdkConvert.Convert).ToList(),
            cancellationToken);
        return SdkConvert.Convert(result);
    }

    /// <inheritdoc />
    public override string ToString() => $"Id = {_id}";
}
