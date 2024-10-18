using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator evaluates the internal expression on every MoveNext call
/// and opens new output source if needed. For example, it is needed if there is an expression
/// in INTO clause (SELECT id, name FROM users INTO write_file(name || '.csv')).
/// </summary>
internal sealed class VaryingOutputRowsIterator : IRowsIterator, IRowsIteratorParent, IDisposable
{
    private readonly IExecutionThread _thread;
    private readonly IRowsIterator _rowsIterator;
    private readonly QueryContext _queryContext;
    private readonly IFuncUnit _outputFactory;
    private readonly FuncUnitCallInfo _functionCallInfo;
    private readonly Dictionary<VariantValueArray, IRowsOutput> _outputs = new();

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _rowsIterator.Current;

    public IRowsOutput CurrentOutput { get; private set; }

    public bool HasOutputDefined => _functionCallInfo != FuncUnitCallInfo.Empty;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(VaryingOutputRowsIterator));

    public VaryingOutputRowsIterator(
        IExecutionThread thread,
        IRowsIterator rowsIterator,
        IFuncUnit func,
        FuncUnitCallInfo functionCallInfo,
        IRowsOutput defaultRowsOutput,
        QueryContext queryContext)
    {
        _thread = thread;
        _rowsIterator = rowsIterator;
        _queryContext = queryContext;

        _outputFactory = func;
        _functionCallInfo = functionCallInfo;

        CurrentOutput = defaultRowsOutput;
    }

    public VaryingOutputRowsIterator(
        IExecutionThread thread,
        IRowsIterator rowsIterator,
        IRowsOutput defaultRowsOutput,
        QueryContext queryContext) : this(
            thread: thread,
            rowsIterator: rowsIterator,
            func: new FuncUnitDelegate(_ => VariantValue.CreateFromObject(defaultRowsOutput), DataType.Object),
            functionCallInfo: FuncUnitCallInfo.Empty,
            defaultRowsOutput,
            queryContext)
    {
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        var result = _rowsIterator.MoveNext();
        if (!result)
        {
            Close();
            return false;
        }

        var argValues = _functionCallInfo.InvokePushArgs(_thread)
            .Where(a => a.Type != DataType.Object)
            .ToArray();
        var args = new VariantValueArray(argValues);
        if (_outputs.TryGetValue(args, out IRowsOutput? output))
        {
            CurrentOutput = output;
        }
        else
        {
            var outputResult = _outputFactory.Invoke(_thread).AsObject;
            if (outputResult is IRowsOutput rowsOutput)
            {
                output = rowsOutput;
            }
            else
            {
                output = NullRowsOutput.Instance;
            }
            output.Open();
            output.QueryContext = _queryContext;
            _logger.LogDebug("Open for args {Arguments}.", _functionCallInfo);
            _outputs.Add(args, output);
            CurrentOutput = output;
        }

        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowsIterator.Reset();
        _outputs.Clear();
    }

    public void Close()
    {
        Close(dispose: false);
    }

    private void Close(bool dispose)
    {
        foreach (var outputKeyValue in _outputs)
        {
            _logger.LogDebug("Close for args {Key}.", outputKeyValue.Key);
            outputKeyValue.Value.Close();
            if (dispose)
            {
                (outputKeyValue.Value as IDisposable)?.Dispose();
            }
        }
        _outputs.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Close(dispose: true);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsIteratorsWithIndent("Vary Output", _rowsIterator);
        IndentedStringBuilderUtils.AppendSubQueriesWithIndent(stringBuilder, _outputFactory);
    }

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        yield return _rowsIterator;
    }
}
