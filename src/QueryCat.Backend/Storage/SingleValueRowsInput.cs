using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Rows input with the single value.
/// </summary>
internal sealed class SingleValueRowsInput : IRowsInput
{
    private readonly int _id = IdGenerator.GetNext();

    private bool _wasRead;
    private readonly VariantValue _value = VariantValue.Null;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public string[] UniqueKey { get; } = [];

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    public SingleValueRowsInput()
    {
        Columns =
        [
            new(Column.ValueColumnTitle, DataType.Integer)
        ];
    }

    public SingleValueRowsInput(VariantValue value)
    {
        Columns =
        [
            new(Column.ValueColumnTitle, value.Type)
        ];
        _value = value;
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        value = _value;
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        if (_wasRead)
        {
            return ValueTask.FromResult(false);
        }
        _wasRead = true;
        return ValueTask.FromResult(true);
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _wasRead = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Single value input (value={_value}, id={_id})");
    }

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override string ToString() => $"{GetType().Name} (value={_value}, id={_id})";

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns() => [];

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
    }

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
    }
}
