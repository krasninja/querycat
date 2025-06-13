using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.Inputs;

/// <summary>
/// Pre-reads the first row.
/// </summary>
internal sealed class PrefetchRowsInput : IRowsInput
{
    private readonly IRowsInput _rowsInput;
    private bool _firstRead;

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    /// <inheritdoc />
    public string[] UniqueKey => _rowsInput.UniqueKey;

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _rowsInput.QueryContext;
        set
        {
            _rowsInput.QueryContext = value;
        }
    }

    private PrefetchRowsInput(IRowsInput rowsInput)
    {
        _rowsInput = rowsInput;
    }

    public static async Task<PrefetchRowsInput> CreateAsync(IRowsInput rowsInput, CancellationToken cancellationToken)
    {
        var prefetchRowsInput = new PrefetchRowsInput(rowsInput);
        var hasData = await rowsInput.ReadNextAsync(cancellationToken);
        if (hasData)
        {
            prefetchRowsInput._firstRead = true;
        }
        return prefetchRowsInput;
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) => _rowsInput.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) => _rowsInput.CloseAsync(cancellationToken);

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) => _rowsInput.ResetAsync(cancellationToken);

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        return _rowsInput.ReadValue(columnIndex, out value);
    }

    /// <inheritdoc />
    public async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        if (_firstRead)
        {
            _firstRead = false;
            return true;
        }
        return await _rowsInput.ReadNextAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent("Prefetch", _rowsInput);
    }
}
