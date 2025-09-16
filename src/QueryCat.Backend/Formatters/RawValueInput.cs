using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Input as binary data.
/// </summary>
internal sealed class RawValueInput : RowsInput
{
    private readonly IBlobData _blobInput;
    private bool _isRead;

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } =
    [
        new(Column.ValueColumnTitle, DataType.Blob)
    ];

    /// <inheritdoc />
    public RawValueInput(IBlobData blobInput, string? key = null)
    {
        _blobInput = blobInput;
    }

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public override Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (columnIndex == 0)
        {
            value = VariantValue.CreateFromObject(_blobInput);
            return ErrorCode.OK;
        }

        value = VariantValue.Null;
        return ErrorCode.NoData;
    }

    /// <inheritdoc />
    public override ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        if (_isRead)
        {
            return ValueTask.FromResult(false);
        }
        _isRead = true;
        return ValueTask.FromResult(true);
    }
}
