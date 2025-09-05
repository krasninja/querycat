using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QueryCat.Backend.Core.Data;

namespace QueryCat.Plugins.Client.Remote;

public sealed class ThriftRemoteRowsIterator : IRowsIterator
{
    private readonly IThriftSessionProvider _sessionProvider;
    private readonly int _objectHandle;
    private readonly long _token;

    /// <inheritdoc />
    public Column[] Columns { get; private set; } = [];

    /// <inheritdoc />
    public Row Current { get; private set; } = Row.Empty;

    public ThriftRemoteRowsIterator(
        IThriftSessionProvider sessionProvider,
        int objectHandle,
        long token = 0)
    {
        _sessionProvider = sessionProvider;
        _objectHandle = objectHandle;
        _token = token;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var session = await _sessionProvider.GetAsync(cancellationToken);
        Columns = (await session.Client.RowsSet_GetColumnsAsync(_token, _objectHandle, cancellationToken))
            .Select(SdkConvert.Convert).ToArray();
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        using var session = await _sessionProvider.GetAsync(cancellationToken);
        var result = await session.Client.RowsSet_GetRowsAsync(_token, _objectHandle, 1, cancellationToken);
        if (result.Values == null || result.Values.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < result.Values.Count && i < Current.Length; i++)
        {
            Current[i] = SdkConvert.Convert(result.Values[i]);
        }
        return true;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        using var session = await _sessionProvider.GetAsync(cancellationToken);
        await session.Client.RowsSet_ResetAsync(_token, _objectHandle, cancellationToken);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Remote iterator (handle={_objectHandle})");
    }
}
