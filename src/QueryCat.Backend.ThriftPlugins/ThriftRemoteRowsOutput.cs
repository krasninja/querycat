using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class ThriftRemoteRowsOutput : IRowsOutput
{
    private readonly Plugins.Sdk.Plugin.Client _client;
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

    public ThriftRemoteRowsOutput(Plugins.Sdk.Plugin.Client client, int objectHandle, string? id = null)
    {
        _client = client;
        _objectHandle = objectHandle;
        _id = id ?? string.Empty;
    }

    private void SendContextToPlugin()
    {
        AsyncUtils.RunSync(async ct => await _client.RowsSet_SetContextAsync(_objectHandle, new ContextQueryInfo
        {
            Columns = QueryContext.QueryInfo.Columns.Select(SdkConvert.Convert).ToList(),
            Limit = QueryContext.QueryInfo.Limit ?? -1,
            Offset = QueryContext.QueryInfo.Offset,
        }, ct));
    }

    /// <inheritdoc />
    public void Open()
    {
        AsyncUtils.RunSync(async ct => await _client.RowsSet_OpenAsync(_objectHandle, ct));
    }

    /// <inheritdoc />
    public void Close()
    {
        AsyncUtils.RunSync(ct => _client.RowsSet_CloseAsync(_objectHandle, ct));
    }

    /// <inheritdoc />
    public void Reset()
    {
        AsyncUtils.RunSync(ct => _client.RowsSet_ResetAsync(_objectHandle, ct));
    }

    /// <inheritdoc />
    public void WriteValues(in VariantValue[] values)
    {
        var valuesArray = values.ToArray();
        AsyncUtils.RunSync(ct => _client.RowsSet_WriteValuesAsync(
            _objectHandle,
            valuesArray.Select(SdkConvert.Convert).ToList(),
            ct));
    }
}
