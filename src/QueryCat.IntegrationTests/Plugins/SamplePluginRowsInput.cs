using System.ComponentModel;
using System.Diagnostics;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Example simple rows input plugin.
/// </summary>
public class SamplePluginRowsInput : IRowsInput
{
    private const long MaxValue = 9;

    [Description("Sample input.")]
    [FunctionSignature("plugin(start: integer = 0): object<IRowsInput>")]
    public static VariantValue SamplePlugin(IExecutionThread thread)
    {
        var startValue = thread.Stack.Pop().AsInteger;
        var rowsSource = new SamplePluginRowsInput(startValue ?? 0);
        return VariantValue.CreateFromObject(rowsSource);
    }

    private long _currentState;

    /// <inheritdoc />
    public Column[] Columns { get; } =
    {
        new("id", DataType.Integer, "Key.")
    };

    /// <inheritdoc />
    public string[] UniqueKey { get; } = [];

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => NullQueryContext.Instance;
        set => Trace.WriteLine($"Set {nameof(QueryContext)}");
    }

    public SamplePluginRowsInput(long initialValue)
    {
        _currentState = initialValue;
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        Trace.WriteLine(nameof(OpenAsync));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        Trace.WriteLine(nameof(CloseAsync));
        return Task.CompletedTask;
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
    public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        Trace.WriteLine(nameof(ReadNextAsync));
        if (_currentState >= MaxValue)
        {
            return ValueTask.FromResult(false);
        }
        _currentState++;
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
        stringBuilder.AppendLine("Sample");
    }
}
