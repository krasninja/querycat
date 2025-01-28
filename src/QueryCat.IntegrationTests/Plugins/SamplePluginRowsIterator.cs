using System.ComponentModel;
using System.Diagnostics;
using QueryCat.Backend;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Example simple rows input iterator.
/// </summary>
public class SamplePluginRowsIterator : IRowsIterator
{
    private const long MaxValue = 9;

    [Description("Sample iterator.")]
    [FunctionSignature("plugin(start: integer = 0): object<IRowsIterator>")]
    public static VariantValue SamplePlugin(IExecutionThread thread)
    {
        var startValue = thread.Stack.Pop().AsInteger;
        var rowsSource = new SamplePluginRowsIterator(startValue ?? 0);
        return VariantValue.CreateFromObject(rowsSource);
    }

    private long _currentState;

    /// <inheritdoc />
    public Column[] Columns { get; } =
    [
        new("id", DataType.Integer, "Key.")
    ];

    /// <inheritdoc />
    public Row Current { get; }

    public SamplePluginRowsIterator(long initialValue)
    {
        Current = new Row(Columns);
        _currentState = initialValue;
    }

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        Trace.WriteLine(nameof(MoveNextAsync));
        if (_currentState >= MaxValue)
        {
            return ValueTask.FromResult(false);
        }
        _currentState++;
        Current["id"] = new VariantValue(_currentState);
        return ValueTask.FromResult(true);
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        Trace.WriteLine(nameof(ResetAsync));
        _currentState = 0;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        Trace.WriteLine(nameof(Explain));
        stringBuilder.AppendRowsIteratorsWithIndent("Sample Plugin");
    }
}
