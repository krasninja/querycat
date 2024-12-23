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
            if (!rowsInput.IsSchemaEqual(firstRowsInput))
            {
                throw new QueryCatException(Resources.Errors.SchemasNotEqual);
            }
        }
    }

    /// <inheritdoc />
    public override void Open()
    {
        if (FetchNextInput())
        {
            _currentRowsInput!.Open();
            Columns = _currentRowsInput.Columns;
        }
    }

    /// <inheritdoc />
    public override void Close()
    {
        _currentRowsInput?.Close();
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
    public override bool ReadNext()
    {
        base.ReadNext();

        if (_currentRowsInput != null)
        {
            if (_currentRowsInput.ReadNext())
            {
                return true;
            }
            _currentRowsInput.Close();
        }

        if (FetchNextInput())
        {
            _currentRowsInput!.Open();
            return ReadNext();
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
    public override void Reset()
    {
        _currentRowsInput = null;
        foreach (var rowsInput in _rowsInputs)
        {
            rowsInput.Reset();
        }
        _currentInputIndex = -1;
        base.Reset();
        FetchNextInput();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Close();
    }

    /// <inheritdoc />
    public override string ToString()
        => string.Join(Environment.NewLine, _rowsInputs.Select(ri => $"{ri.GetType().Name}: {ri}"));
}
