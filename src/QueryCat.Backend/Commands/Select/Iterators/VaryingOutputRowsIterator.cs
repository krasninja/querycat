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
    private readonly VariantValue[] _functionCallInfoResults;
    private readonly VariantValue[] _functionCallInfoResultsForCompare;
    private bool _firstCall = true;
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
        _functionCallInfoResults = new VariantValueArray(size: functionCallInfo.Arguments.Length);
        _functionCallInfoResultsForCompare = new VariantValueArray(size: functionCallInfo.Arguments.Length);

        CurrentOutput = defaultRowsOutput;
    }

    public VaryingOutputRowsIterator(
        IExecutionThread thread,
        IRowsIterator rowsIterator,
        IRowsOutput defaultRowsOutput,
        QueryContext queryContext) : this(
            thread: thread,
            rowsIterator: rowsIterator,
            func: new FuncUnitDelegate((_, _) => ValueTask.FromResult(VariantValue.CreateFromObject(defaultRowsOutput)), DataType.Object),
            functionCallInfo: FuncUnitCallInfo.Empty,
            defaultRowsOutput,
            queryContext)
    {
    }

    /// <inheritdoc />
    public async ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        var result = await _rowsIterator.MoveNextAsync(cancellationToken);
        if (!result)
        {
            await CloseAsync(cancellationToken);
            return false;
        }

        var values = await InvokeArgumentsDelegatesAsync(cancellationToken);
        var args = new VariantValueArray(values);
        if (_outputs.TryGetValue(args, out IRowsOutput? output))
        {
            CurrentOutput = output;
        }
        else
        {
            var outputResult = (await _outputFactory.InvokeAsync(_thread, cancellationToken)).AsObject;
            if (outputResult is IRowsOutput rowsOutput)
            {
                output = rowsOutput;
            }
            else
            {
                output = NullRowsOutput.Instance;
            }
            output.QueryContext = _queryContext;
            await output.OpenAsync(cancellationToken);
            _logger.LogDebug("Open for args {Arguments}.", _functionCallInfo);
            _outputs.Add(args, output);
            CurrentOutput = output;
        }

        return true;
    }

    private async ValueTask<VariantValue[]> InvokeArgumentsDelegatesAsync(CancellationToken cancellationToken)
    {
        var args = _functionCallInfo.Arguments;
        for (var i = 0; i < _functionCallInfoResults.Length; i++)
        {
            if (!_firstCall
                && (_functionCallInfoResults[i].Type == DataType.Object || _functionCallInfoResults[i].Type == DataType.Dynamic))
            {
                continue;
            }
            _functionCallInfoResults[i] = await args[i].InvokeAsync(_thread, cancellationToken);
            if (_functionCallInfoResults[i].Type != DataType.Object
                || _functionCallInfoResults[i].Type != DataType.Dynamic)
            {
                _functionCallInfoResultsForCompare[i] = _functionCallInfoResults[i];
            }
        }
        _firstCall = false;
        return _functionCallInfoResultsForCompare;
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _rowsIterator.ResetAsync(cancellationToken);
        _outputs.Clear();
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        foreach (var outputKeyValue in _outputs)
        {
            _logger.LogDebug("Close for args {Key}.", outputKeyValue.Key);
            await outputKeyValue.Value.CloseAsync(cancellationToken);
        }
        Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var outputKeyValue in _outputs)
        {
            (outputKeyValue.Value as IDisposable)?.Dispose();
        }
        _outputs.Clear();
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
