using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Inputs;

internal sealed class GenerateSeriesInput : IRowsInput
{
    [Description("Generates a series of values from start to stop, with a step size of step.")]
    [FunctionSignature("generate_series(start: integer, stop: integer, step: integer = 1): object<IRowsInput>")]
    [FunctionSignature("generate_series(start: float, stop: float, step: float = 1): object<IRowsInput>")]
    [FunctionSignature("generate_series(start: numeric, stop: numeric, step: numeric = 1): object<IRowsInput>")]
    [FunctionSignature("generate_series(start: timestamp, stop: timestamp, step: timestamp): object<IRowsInput>")]
    public static VariantValue GenerateSeries(FunctionCallInfo args)
    {
        return VariantValue.CreateFromObject(
            new GenerateSeriesInput(args.GetAt(0), args.GetAt(1), args.GetAt(2)));
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
    public string[] UniqueKey => new[]
    {
        _start.ToString(),
        _end.ToString(),
        _step.ToString(),
    };

    public GenerateSeriesInput(VariantValue start, VariantValue end, VariantValue step)
    {
        _start = start;
        _current = start;
        _end = end;
        _step = step;

        _addFunction = VariantValue.GetAddDelegate(_current.GetInternalType(), _step.GetInternalType());
        Columns = new[]
        {
            new Column("value", _current.GetInternalType(), "The series value."),
        };
    }

    /// <inheritdoc />
    public void Open()
    {
    }

    /// <inheritdoc />
    public void Close()
    {
    }

    /// <inheritdoc />
    public void Reset()
    {
        _current = _start;
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
    public bool ReadNext()
    {
        var next = _addFunction.Invoke(_current, _step);
        if (next <= _end)
        {
            _current = next;
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Series");
    }
}
