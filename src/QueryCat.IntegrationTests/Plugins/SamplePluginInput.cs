using System.ComponentModel;
using System.Diagnostics;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.IntegrationTests.Plugins;

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

    public SamplePluginInput() : base(Initialize)
    {
    }

    private static void Initialize(ClassRowsFrameBuilder<TestClass> builder)
    {
        Trace.WriteLine(nameof(Initialize));
        builder
            .AddProperty(b => b.Key)
            .AddKeyColumn("key",
                isRequired: false,
                operation: VariantValue.Operation.Equals);
    }

    /// <inheritdoc />
    protected override IEnumerable<TestClass> GetData()
    {
        Trace.WriteLine(nameof(GetData));
        var key = GetKeyColumnValue("key");
        for (var i = 0; i < MaxValue; i++)
        {
            yield return new TestClass(++_currentState);
        }
    }
}
