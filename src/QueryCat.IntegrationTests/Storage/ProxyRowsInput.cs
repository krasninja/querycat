using QueryCat.Backend;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.IntegrationTests.Storage;

public class ProxyRowsInput : IRowsInput, IRowsIteratorParent
{
    private IRowsInput _rowsInput;
    private QueryContext _queryContext = new EmptyQueryContext();

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _queryContext;
        set
        {
            _queryContext = value;
            _rowsInput.QueryContext = _queryContext;
        }
    }

    public ProxyRowsInput(IRowsInput rowsInput)
    {
        _rowsInput = rowsInput;
    }

    /// <inheritdoc />
    public void Open() => _rowsInput.Open();

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
        if (inputArguments.Any())
        {
            QueryContext.InputInfo.SetInputArguments(inputArguments);
        }
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsInput;
    }
}
