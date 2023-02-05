using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

internal record WindowFunctionInfo(
    IFuncUnit[] PartitionFormatters);

internal sealed class WindowFunctionsRowsIterator : IRowsIterator
{
    private readonly IRowsIterator _rowsIterator;
    private readonly IEnumerable<WindowFunctionInfo> _windowFunctionInfos;
    private readonly RowsFrame _rowsFrame;
    private readonly RowsFrameIterator _rowsFrameIterator;
    private bool _isInitialized;

    private readonly List<VariantValueArray> _partitions = new();

    /// <inheritdoc />
    public Column[] Columns => _rowsFrame.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public WindowFunctionsRowsIterator(IRowsIterator rowsIterator,
        IEnumerable<WindowFunctionInfo> windowFunctionInfos)
    {
        _rowsIterator = rowsIterator;
        _windowFunctionInfos = windowFunctionInfos;

        _rowsFrame = new RowsFrame(_rowsIterator.Columns);
        _rowsFrameIterator = _rowsFrame.GetIterator();
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (!_isInitialized)
        {
            FillRows();
            _isInitialized = true;
        }

        var hasData = _rowsIterator.MoveNext();
        if (hasData)
        {
            foreach (var windowFunctionInfo in _windowFunctionInfos)
            {
                var arrKey = new VariantValueArray(
                    windowFunctionInfo.PartitionFormatters.Select(f => f.Invoke()));
                _partitions.Add(arrKey);
            }
        }
        return hasData;
    }

    private void FillRows()
    {
        while (_rowsIterator.MoveNext())
        {
            _rowsFrame.AddRow(_rowsIterator.Current);
        }
        CreatePartitions();
    }

    private void CreatePartitions()
    {
        var rowsIterator = _rowsFrame.GetIterator();
        while (rowsIterator.MoveNext())
        {
            foreach (var windowFunctionInfo in _windowFunctionInfos)
            {
                windowFunctionInfo.PartitionFormatters.Select(f => f.Invoke());
            }
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        _isInitialized = false;
        _rowsFrame.Clear();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Window", _rowsIterator);
    }
}
