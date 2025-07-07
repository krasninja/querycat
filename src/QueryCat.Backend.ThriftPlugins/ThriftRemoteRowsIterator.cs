using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;
using Column = QueryCat.Backend.Core.Data.Column;
using KeyColumn = QueryCat.Backend.Core.Data.KeyColumn;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class ThriftRemoteRowsIterator : IRowsInputUpdate, IRowsInputDelete
{
    private const int DefaultLoadCount = 1;

    private readonly ThriftPluginContext _context;
    private readonly int _objectHandle;
    private readonly int _loadCount;
    private readonly string _id;
    private readonly DynamicBuffer<VariantValue> _cache = new(chunkSize: 64);

    private IReadOnlyList<KeyColumn>? _cachedKeyColumns;

    /// <inheritdoc />
    public string[] UniqueKey { get; private set; } = [];

    /// <inheritdoc />
    public Column[] Columns { get; private set; } = [];

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

    public ThriftRemoteRowsIterator(ThriftPluginContext context, int objectHandle, string? id = null, int loadCount = DefaultLoadCount)
    {
        _context = context;
        _objectHandle = objectHandle;
        _loadCount = loadCount;
        _id = id ?? string.Empty;
    }

    private void SendContextToPlugin()
    {
        _cachedKeyColumns = null;
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
        Columns = (await session.ClientProxy.RowsSet_GetColumnsAsync(_objectHandle, cancellationToken))
            .Select(SdkConvert.Convert).ToArray();
        UniqueKey = (await session.ClientProxy.RowsSet_GetUniqueKeyAsync(_objectHandle, cancellationToken)).ToArray();
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _cachedKeyColumns = null;
        using var session = await _context.GetSessionAsync(cancellationToken);
        await session.ClientProxy.RowsSet_CloseAsync(_objectHandle, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _cachedKeyColumns = null;
        using var session = await _context.GetSessionAsync(cancellationToken);
        await session.ClientProxy.RowsSet_ResetAsync(_objectHandle, cancellationToken);
        _cache.Clear();
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (columnIndex > Columns.Length - 1)
        {
            value = VariantValue.Null;
            return ErrorCode.InvalidColumnIndex;
        }

        if (_cache.TryGetAt(columnIndex, out value))
        {
            return ErrorCode.OK;
        }
        return ErrorCode.NoData;
    }

    /// <inheritdoc />
    public async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        if (!_cache.IsEmpty)
        {
            _cache.Advance(Columns.Length);
            if (!_cache.IsEmpty)
            {
                return true;
            }
        }

        using var session = await _context.GetSessionAsync(cancellationToken);
        var result = await session.ClientProxy.RowsSet_GetRowsAsync(_objectHandle, _loadCount, cancellationToken);
        if (result.Values == null || result.Values.Count == 0)
        {
            return false;
        }

        var values = result.Values.Select(SdkConvert.Convert).ToArray();
        _cache.Write(values);
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns()
    {
        if (_cachedKeyColumns == null)
        {
            var result = AsyncUtils.RunSync(async ct =>
                {
                    using var session = await _context.GetSessionAsync(ct);
                    return (await session.ClientProxy.RowsSet_GetKeyColumnsAsync(_objectHandle, ct))
                        .Select(c => new KeyColumn(
                            c.ColumnIndex,
                            c.IsRequired,
                            (c.Operations ?? new List<string>()).Select(Enum.Parse<VariantValue.Operation>).ToArray()
                        ));
                }
            );
            _cachedKeyColumns = (result ?? Array.Empty<KeyColumn>()).ToList();
        }
        return _cachedKeyColumns;
    }

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        AsyncUtils.RunSync(async ct =>
        {
            using var session = await _context.GetSessionAsync(ct);
            await session.ClientProxy.RowsSet_SetKeyColumnValueAsync(_objectHandle, columnIndex, operation.ToString(),
                SdkConvert.Convert(value), ct);
        });
    }

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        AsyncUtils.RunSync(async ct =>
        {
            using var session = await _context.GetSessionAsync(ct);
            await session.ClientProxy.RowsSet_UnsetKeyColumnValueAsync(_objectHandle, columnIndex, operation.ToString(), ct);
        });
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> UpdateValueAsync(int columnIndex, VariantValue value, CancellationToken cancellationToken = default)
    {
        using var session = await _context.GetSessionAsync(cancellationToken);
        var result = await session.ClientProxy.RowsSet_UpdateValueAsync(_objectHandle, columnIndex, SdkConvert.Convert(value),
            cancellationToken);
        return SdkConvert.Convert(result);
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> DeleteAsync(CancellationToken cancellationToken = default)
    {
        using var session = await _context.GetSessionAsync(cancellationToken);
        var result = await session.ClientProxy.RowsSet_DeleteRowAsync(_objectHandle, cancellationToken);
        return SdkConvert.Convert(result);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Remote (handle={_objectHandle}, id={_id})");
    }

    /// <inheritdoc />
    public override string ToString() => $"Id = {_id}";
}
