# Advanced Plugin

The article describes other ways to create plugin.

## Plugin Based On IRowsIterator

Since `IRowsInput` is low-level interface, for some plugins it might be difficult to use. Instead, you can use `IRowsIterator`. It doesn't have open/close functionality, and requires developer to provide `Row` instance for `Current`.

```csharp
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
```

## Plugin Based On FetchRowsInput

Another way to define plugin. It is high-level wrapper for rows input. The main purpose is to provide convenience wrapper for inputs that do external calls (API clients, etc).

```csharp
/// <summary>
/// Example simple rows input plugin based on <see cref="FetchRowsInput{TClass}" />.
/// </summary>
public class SamplePluginInput : EnumerableRowsInput<TestClass>
{
    private const long MaxValue = 9;

    [Description("Sample input.")]
    [FunctionSignature("plugin(): object<IRowsInput>")]
    public static VariantValue SamplePlugin(IExecutionThread thread)
    {
        var rowsSource = new SamplePluginInput();
        return VariantValue.CreateFromObject(rowsSource);
    }

    private long _currentState;

    /// <inheritdoc />
    protected override void Initialize(ClassRowsFrameBuilder<TestClass> builder)
    {
        Trace.WriteLine(nameof(Initialize));
        builder
            .AddProperty(b => b.Key)
            .AddKeyColumn("key",
                isRequired: false,
                operation: VariantValue.Operation.Equals);
    }

    /// <inheritdoc />
    protected override IEnumerable<TestClass> GetData(Fetcher<TestClass> fetcher)
    {
        Trace.WriteLine(nameof(GetData));
        var key = GetKeyColumnValue("key");
        for (var i = 0; i < MaxValue; i++)
        {
            yield return new TestClass(++_currentState);
        }
    }
}
```
