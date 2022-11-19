using QueryCat.Backend;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.IntegrationTests.Storage;

public class ProxyRowsInput : IRowsInput
{
    private IRowsInput _rowsInput;
    private QueryContext? _queryContext;

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    public ProxyRowsInput(IRowsInput rowsInput)
    {
        _rowsInput = rowsInput;
    }

    /// <inheritdoc />
    public void Open() => _rowsInput.Open();

    /// <inheritdoc />
    public void SetContext(QueryContext queryContext)
    {
        _queryContext = queryContext;
        _rowsInput.SetContext(queryContext);
    }

    /// <inheritdoc />
    public void Close() => _rowsInput.Close();

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value) => _rowsInput.ReadValue(columnIndex, out value);

    /// <inheritdoc />
    public bool ReadNext() => _rowsInput.ReadNext();

    /// <inheritdoc />
    public void Reset() => _rowsInput.Reset();

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent("Proxy", _rowsInput);
    }

    public void SetInput(IRowsInput rowsInput, params string[] inputArguments)
    {
        _rowsInput = rowsInput;
        if (_queryContext != null && inputArguments.Any())
        {
            _queryContext.InputInfo.SetInputArguments(inputArguments);
        }
    }
}
