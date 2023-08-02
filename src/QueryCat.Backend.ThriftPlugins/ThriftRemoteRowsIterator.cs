using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;
using QueryCat.Plugins.Client;
using Column = QueryCat.Backend.Abstractions.Column;

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
    public QueryContext QueryContext { get; set; } = EmptyQueryContext.Empty;

    public ThriftRemoteRowsIterator(Plugins.Sdk.Plugin.Client client, int objectHandle)
    {
        _client = client;
        _objectHandle = objectHandle;
    }

    /// <inheritdoc />
    public void Open()
    {
        AsyncUtils.RunSync(async () =>
        {
            await _client.RowsSet_OpenAsync(_objectHandle);
            Columns = (await _client.RowsSet_GetColumnsAsync(_objectHandle)).Select(SdkConvert.Convert).ToArray();
            UniqueKey = (await _client.RowsSet_GetUniqueKeyAsync(_objectHandle)).ToArray();
        });
    }

    /// <inheritdoc />
    public void Close()
    {
        AsyncUtils.RunSync(() => _client.RowsSet_CloseAsync(_objectHandle));
    }

    /// <inheritdoc />
    public void Reset()
    {
        AsyncUtils.RunSync(() => _client.RowsSet_ResetAsync(_objectHandle));
        _hasRecords = true;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (!_hasRecords)
        {
            value = VariantValue.Null;
            return ErrorCode.OK;
        }
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
        if (!_hasRecords)
        {
            return false;
        }

        if (!_cache.IsEmpty)
        {
            _cache.Advance(Columns.Length);
            if (!_cache.IsEmpty)
            {
                return true;
            }
        }

        var result = AsyncUtils.RunSync(() => _client.RowsSet_GetRowsAsync(_objectHandle, LoadCount));
        if (result == null || result.Values == null || result.Values.Count == 0 || !result.HasMore)
        {
            _hasRecords = false;
            return false;
        }

        var values = result.Values.Select(SdkConvert.Convert).ToArray();
        _cache.Write(values);
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns()
    {
        var result = AsyncUtils.RunSync(async () =>
            {
                return (await _client.RowsSet_GetKeyColumnsAsync(_objectHandle))
                    .Select(c => new KeyColumn(
                        c.Name,
                        c.IsRequired,
                        (c.Operations ?? new List<string>()).Select(Enum.Parse<VariantValue.Operation>).ToArray()
                    ));
            }
        );
        return (result ?? Array.Empty<KeyColumn>()).ToList();
    }

    /// <inheritdoc />
    public void SetKeyColumnValue(string columnName, VariantValue value, VariantValue.Operation operation)
    {
        AsyncUtils.RunSync(() =>
            _client.RowsSet_SetKeyColumnValueAsync(_objectHandle, columnName, operation.ToString(), SdkConvert.Convert(value)));
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Remote");
    }
}
