using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Providers;

/// <summary>
/// The class combines multiple rows inputs (of the same schema) into a single rows input.
/// </summary>
internal sealed class CombineRowsInput : RowsInput, IDisposable
{
    private readonly IReadOnlyList<IRowsInput> _rowsInputs;
    private int _currentInputIndex = -1;
    private IRowsInput? _currentRowsInput;

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } = Array.Empty<Column>();

    public CombineRowsInput(IReadOnlyList<IRowsInput> rowsInputs)
    {
        if (!rowsInputs.Any())
        {
            throw new ArgumentException("No inputs.", nameof(rowsInputs));
        }
        _rowsInputs = rowsInputs;
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
        return ErrorCode.Error;
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
        base.Reset();
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
