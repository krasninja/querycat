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
```

## Plugin Based On ClassEnumerableInput

Another way to define plugin. It is high-level wrapper for rows input.

```csharp
/// <summary>
/// Example simple rows input plugin based on <see cref="ClassEnumerableInput{TClass}" />.
/// </summary>
public class SamplePluginEnumerableInput : ClassEnumerableInput<TestClass>
{
    private const long MaxValue = 9;

    [Description("Sample input.")]
    [FunctionSignature("plugin(): object<IRowsInput>")]
    public static VariantValue SamplePlugin(FunctionCallInfo args)
    {
        var rowsSource = new SamplePluginEnumerableInput();
        return VariantValue.CreateFromObject(rowsSource);
    }

    private long _currentState;
    private string? _key;

    /// <inheritdoc />
    protected override void Initialize(ClassRowsFrameBuilder<TestClass> builder)
    {
        Trace.WriteLine(nameof(Initialize));
        builder.AddProperty(b => b.Key);
    }

    /// <inheritdoc />
    protected override void InitializeInputInfo(QueryContextInputInfo inputInfo)
    {
        Trace.WriteLine(nameof(InitializeInputInfo));
        inputInfo.AddKeyColumn("Key",
            isRequired: true,
            operation: VariantValue.Operation.Equals,
            action: value => _key = value.AsString);
    }

    /// <inheritdoc />
    protected override IEnumerable<TestClass> GetData(ClassEnumerableInputFetch<TestClass> fetch)
    {
        Trace.WriteLine(nameof(GetData));
        for (var i = 0; i < MaxValue; i++)
        {
            yield return new TestClass(++_currentState);
        }
    }
}
```
