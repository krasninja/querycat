using System.ComponentModel;
using System.Diagnostics;
using QueryCat.Backend;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Example simple rows input plugin.
/// </summary>
public class SamplePluginRowsInput : IRowsInput
{
    private const long MaxValue = 9;

    [Description("Sample input.")]
    [FunctionSignature("plugin(start: integer = 0): object<IRowsInput>")]
    public static VariantValue SamplePlugin(FunctionCallInfo args)
    {
        var startValue = args.GetAt(0).AsInteger;
        var rowsSource = new SamplePluginRowsInput(startValue);
        return VariantValue.CreateFromObject(rowsSource);
    }

    private long _currentState;

    /// <inheritdoc />
    public Column[] Columns { get; } =
    {
        new("id", DataType.Integer, "Key.")
    };

    public SamplePluginRowsInput(long initialValue)
    {
        _currentState = initialValue;
    }

    /// <inheritdoc />
    public void Open()
    {
        Trace.WriteLine(nameof(Open));
    }

    /// <inheritdoc />
    public void SetContext(QueryContext queryContext)
    {
        Trace.WriteLine(nameof(SetContext));
    }

    /// <inheritdoc />
    public void Close()
    {
        Trace.WriteLine(nameof(Close));
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        Trace.WriteLine(nameof(ReadValue));
        if (columnIndex == 0)
        {
            value = new VariantValue(_currentState);
            return ErrorCode.OK;
        }

        value = default;
        return ErrorCode.InvalidColumnIndex;
    }

    /// <inheritdoc />
    public bool ReadNext()
    {
        Trace.WriteLine(nameof(ReadNext));
        if (_currentState >= MaxValue)
        {
            return false;
        }
        _currentState++;
        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Trace.WriteLine(nameof(Reset));
        _currentState = 0;
    }
}
