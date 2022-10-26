using QueryCat.Backend.Functions;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Commands.Select.Iterators;

/// <summary>
/// The iterator evaluates the internal expression on every MoveNext call
/// and opens new output source if needed. For example, it is needed if there is an expression
/// in INTO clause (SELECT id, name FROM users INTO write_file(name || '.csv')).
/// </summary>
internal sealed class VaryingOutputRowsIterator : IRowsIterator, IDisposable
{
    private readonly IRowsIterator _rowsIterator;
    private readonly QueryContext _queryContext;
    private readonly FuncUnit _outputFactory;
    private readonly FunctionCallInfo _functionCallInfo;
    private readonly Dictionary<VariantValueArray, IRowsOutput> _outputs = new();

    /// <inheritdoc />
    public Column[] Columns => _outputFactory.Data.RowsIterator.Columns;

    /// <inheritdoc />
    public Row Current => _outputFactory.Data.RowsIterator.Current;

    public IRowsOutput CurrentOutput { get; private set; }

    public bool HasOutputDefined => _functionCallInfo != FunctionCallInfo.Empty;

    public VaryingOutputRowsIterator(
        IRowsIterator rowsIterator,
        FuncUnit func,
        FunctionCallInfo functionCallInfo,
        IRowsOutput defaultRowsOutput,
        QueryContext queryContext)
    {
        _rowsIterator = rowsIterator;
        _queryContext = queryContext;

        _outputFactory = func;
        _functionCallInfo = functionCallInfo;

        CurrentOutput = defaultRowsOutput;
    }

    public VaryingOutputRowsIterator(
        IRowsIterator rowsIterator,
        IRowsOutput defaultRowsOutput,
        QueryContext queryContext) : this(
            rowsIterator: rowsIterator,
            func: new FuncUnit(_ => VariantValue.CreateFromObject(defaultRowsOutput)),
            functionCallInfo: FunctionCallInfo.Empty,
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

        _functionCallInfo.InvokePushArgs(_outputFactory.Data);
        if (_outputs.TryGetValue(_functionCallInfo.Arguments, out IRowsOutput? output))
        {
            CurrentOutput = output;
        }
        else
        {
            var outputResult = _outputFactory.Invoke().AsObject;
            if (outputResult is IRowsOutput rowsOutput)
            {
                output = rowsOutput;
            }
            else
            {
                output = NullRowsOutput.Instance;
            }
            output.Open();
            output.SetContext(_queryContext);
            Logger.Instance.Debug($"Open for args {_functionCallInfo.Arguments}.", nameof(VaryingOutputRowsIterator));
            _outputs.Add(new VariantValueArray(_functionCallInfo.Arguments), output);
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
            Logger.Instance.Debug($"Close for args {outputKeyValue.Key}.", nameof(VaryingOutputRowsIterator));
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
        stringBuilder.AppendRowsIteratorsWithIndent("Vary Output", _rowsIterator)
            .AppendSubQueriesWithIndent(_outputFactory);
    }
}
