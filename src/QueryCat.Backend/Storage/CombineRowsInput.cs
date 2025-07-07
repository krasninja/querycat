using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The class combines multiple rows inputs (of the same schema) into a single rows input.
/// </summary>
internal sealed class CombineRowsInput : RowsInput, IDisposable
{
    private readonly IReadOnlyList<IRowsInput> _rowsInputs;
    private int _currentInputIndex = -1;
    private IRowsInput? _currentRowsInput;
    private QueryContext _queryContext = NullQueryContext.Instance;

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } = [];

    /// <inheritdoc />
    public override QueryContext QueryContext
    {
        get => _queryContext;
        set
        {
            _queryContext = value;
            foreach (var rowsInput in _rowsInputs)
            {
                rowsInput.QueryContext = _queryContext;
            }
        }
    }

    public CombineRowsInput(IReadOnlyList<IRowsInput> rowsInputs)
    {
        if (!rowsInputs.Any())
        {
            throw new ArgumentException(Resources.Errors.NoInputs, nameof(rowsInputs));
        }
        _rowsInputs = rowsInputs;

        ValidateRowsInputsColumns();
    }

    private void ValidateRowsInputsColumns()
    {
        var firstRowsInput = _rowsInputs.First();
        foreach (var rowsInput in _rowsInputs.Skip(1))
        {
            if (!rowsInput.IsSchemaEqual(firstRowsInput.Columns))
            {
                throw new QueryCatException(Resources.Errors.SchemasNotEqual);
            }
        }
    }

    /// <inheritdoc />
    public override async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (FetchNextInput())
        {
            await _currentRowsInput!.OpenAsync(cancellationToken);
            Columns = _currentRowsInput.Columns;
        }
    }

    /// <inheritdoc />
    public override async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_currentRowsInput == null)
        {
            return;
        }
        await _currentRowsInput.CloseAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (_currentRowsInput != null)
        {
            return _currentRowsInput.ReadValue(columnIndex, out value);
        }
        value = VariantValue.Null;
        return ErrorCode.NoData;
    }

    /// <inheritdoc />
    public override async ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        await base.ReadNextAsync(cancellationToken);

        if (_currentRowsInput != null)
        {
            if (await _currentRowsInput.ReadNextAsync(cancellationToken))
            {
                return true;
            }
            await _currentRowsInput.CloseAsync(cancellationToken);
        }

        if (FetchNextInput() && _currentRowsInput != null)
        {
            await _currentRowsInput.OpenAsync(cancellationToken);
            return await ReadNextAsync(cancellationToken);
        }

        return false;
    }

    private bool FetchNextInput()
    {
        _currentInputIndex++;
        if (_currentInputIndex > -1 && _currentInputIndex < _rowsInputs.Count)
        {
            _currentRowsInput = _rowsInputs[_currentInputIndex];
            return true;
        }
        _currentRowsInput = null;
        return false;
    }

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _currentRowsInput = null;
        foreach (var rowsInput in _rowsInputs)
        {
            await rowsInput.ResetAsync(cancellationToken);
        }
        _currentInputIndex = -1;
        await base.ResetAsync(cancellationToken);
        FetchNextInput();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        (_currentRowsInput as IDisposable)?.Dispose();
        _currentRowsInput = null;
    }

    /// <inheritdoc />
    public override string ToString()
        => string.Join(Environment.NewLine, _rowsInputs.Select(ri => $"{ri.GetType().Name}: {ri}"));
}
