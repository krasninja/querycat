using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client;
using Column = QueryCat.Backend.Core.Data.Column;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class ThriftRemoteRowsIterator : IRowsInputKeys
{
    private const int LoadCount = 10;

    private readonly Plugins.Sdk.Plugin.Client _client;
    private readonly int _objectHandle;
    private readonly DynamicBuffer<VariantValue> _cache = new(chunkSize: 64);
    private bool _hasRecords = true;

    /// <inheritdoc />
    public string[] UniqueKey { get; private set; } = Array.Empty<string>();

    /// <inheritdoc />
    public Column[] Columns { get; private set; } = Array.Empty<Column>();

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    public ThriftRemoteRowsIterator(Plugins.Sdk.Plugin.Client client, int objectHandle)
    {
        _client = client;
        _objectHandle = objectHandle;
    }

    /// <inheritdoc />
    public void Open()
    {
        AsyncUtils.RunSync(async ct =>
        {
            await _client.RowsSet_OpenAsync(_objectHandle, ct);
            Columns = (await _client.RowsSet_GetColumnsAsync(_objectHandle, ct)).Select(SdkConvert.Convert).ToArray();
            UniqueKey = (await _client.RowsSet_GetUniqueKeyAsync(_objectHandle, ct)).ToArray();
        });
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
        _hasRecords = true;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (columnIndex > Columns.Length - 1)
        {
            value = VariantValue.Null;
            return ErrorCode.InvalidColumnIndex;
        }

        value = _cache.GetAt(columnIndex);
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public bool ReadNext()
    {
        if (!_cache.IsEmpty)
        {
            _cache.Advance(Columns.Length);
            if (!_cache.IsEmpty)
            {
                return true;
            }
        }

        if (!_hasRecords)
        {
            return false;
        }

        var result = AsyncUtils.RunSync(ct => _client.RowsSet_GetRowsAsync(_objectHandle, LoadCount * Columns.Length, ct));
        if (result == null || result.Values == null || result.Values.Count == 0)
        {
            _hasRecords = false;
            return false;
        }

        if (!result.HasMore)
        {
            _hasRecords = false;
        }

        var values = result.Values.Select(SdkConvert.Convert).ToArray();
        _cache.Write(values);
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns()
    {
        var result = AsyncUtils.RunSync(async ct =>
            {
                return (await _client.RowsSet_GetKeyColumnsAsync(_objectHandle, ct))
                    .Select(c => new KeyColumn(
                        c.ColumnIndex,
                        c.IsRequired,
                        (c.Operations ?? new List<string>()).Select(Enum.Parse<VariantValue.Operation>).ToArray()
                    ));
            }
        );
        return (result ?? Array.Empty<KeyColumn>()).ToList();
    }

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        AsyncUtils.RunSync(ct =>
            _client.RowsSet_SetKeyColumnValueAsync(_objectHandle, columnIndex, operation.ToString(), SdkConvert.Convert(value), ct));
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Remote");
    }
}
