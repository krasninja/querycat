using System.ComponentModel;
using System.Diagnostics;
using QueryCat.Backend;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Example simple rows input iterator.
/// </summary>
public class SamplePluginRowsIterator : IRowsIterator
{
    private const long MaxValue = 9;

    [Description("Sample iterator.")]
    [FunctionSignature("plugin(start: integer = 0): object<IRowsIterator>")]
    public static VariantValue SamplePlugin(FunctionCallInfo args)
    {
        var startValue = args.GetAt(0).AsInteger;
        var rowsSource = new SamplePluginRowsIterator(startValue);
        return VariantValue.CreateFromObject(rowsSource);
    }

    private long _currentState;

    /// <inheritdoc />
    public Column[] Columns { get; } =
    {
        new("id", DataType.Integer, "Key.")
    };

    /// <inheritdoc />
    public Row Current { get; }

    public SamplePluginRowsIterator(long initialValue)
    {
        Current = new Row(Columns);
        _currentState = initialValue;
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        Trace.WriteLine(nameof(MoveNext));
        if (_currentState >= MaxValue)
        {
            return false;
        }
        _currentState++;
        Current["id"] = new VariantValue(_currentState);
        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Trace.WriteLine(nameof(Reset));
        _currentState = 0;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        Trace.WriteLine(nameof(Explain));
        stringBuilder.AppendRowsIteratorsWithIndent("Sample Plugin");
    }
}
