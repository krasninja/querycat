using System.ComponentModel;
using System.Globalization;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Inputs;

internal sealed class GenerateSeriesInput : IRowsInput
{
    [Description("Generates a series of values from start to stop, with a step size of step.")]
    [FunctionSignature("generate_series(start: integer, stop: integer, step: integer = 1): object<IRowsInput>")]
    [FunctionSignature("generate_series(start: float, stop: float, step: float = 1): object<IRowsInput>")]
    [FunctionSignature("generate_series(start: numeric, stop: numeric, step: numeric = 1): object<IRowsInput>")]
    [FunctionSignature("generate_series(start: timestamp, stop: timestamp, step: timestamp): object<IRowsInput>")]
    public static VariantValue GenerateSeries(IExecutionThread thread)
    {
        return VariantValue.CreateFromObject(
            new GenerateSeriesInput(thread.Stack[0], thread.Stack[1], thread.Stack[2]));
    }

    private VariantValue _current;
    private readonly VariantValue _start;
    private readonly VariantValue _end;
    private readonly VariantValue _step;
    private readonly VariantValue.BinaryFunction _addFunction;

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public string[] UniqueKey =>
    [
        _start.ToString(CultureInfo.InvariantCulture),
        _end.ToString(CultureInfo.InvariantCulture),
        _step.ToString(CultureInfo.InvariantCulture)
    ];

    public GenerateSeriesInput(VariantValue start, VariantValue end, VariantValue step)
    {
        _start = start;
        _current = start;
        _end = end;
        _step = step;

        _addFunction = VariantValue.GetAddDelegate(_current.Type, _step.Type);
        Columns =
        [
            new Column("value", _current.Type, "The series value.")
        ];
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _current = _start;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (columnIndex == 0)
        {
            value = _current;
            return ErrorCode.OK;
        }

        value = VariantValue.Null;
        return ErrorCode.InvalidColumnIndex;
    }

    /// <inheritdoc />
    public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        var next = _addFunction.Invoke(_current, _step);
        if (next <= _end)
        {
            _current = next;
            return ValueTask.FromResult(true);
        }
        return ValueTask.FromResult(false);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Series");
    }
}
