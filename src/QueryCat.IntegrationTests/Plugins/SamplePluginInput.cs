using System.ComponentModel;
using System.Diagnostics;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Example simple rows input plugin based on <see cref="FetchInput{TClass}" />.
/// </summary>
public class SamplePluginInput : FetchInput<TestClass>
{
    private const long MaxValue = 9;

    [Description("Sample input.")]
    [FunctionSignature("plugin(): object<IRowsInput>")]
    public static VariantValue SamplePlugin(FunctionCallInfo args)
    {
        var rowsSource = new SamplePluginInput();
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
        AddKeyColumn("Key",
            isRequired: false,
            operation: VariantValue.Operation.Equals,
            set: value => _key = value.AsString);
    }

    /// <inheritdoc />
    protected override IEnumerable<TestClass> GetData(Fetcher<TestClass> fetcher)
    {
        Trace.WriteLine(nameof(GetData));
        for (var i = 0; i < MaxValue; i++)
        {
            yield return new TestClass(++_currentState);
        }
    }
}
