using System.ComponentModel;
using System.Diagnostics;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.IntegrationTests.Plugins;

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

    private long _currentState = 0;

    /// <inheritdoc />
    protected override void Initialize(ClassRowsFrameBuilder<TestClass> builder)
    {
        Trace.WriteLine(nameof(Initialize));
        builder.AddProperty(b => b.Key);
    }

    /// <inheritdoc />
    protected override IEnumerable<TestClass> GetData()
    {
        Trace.WriteLine(nameof(GetData));
        for (var i = 0; i < MaxValue; i++)
        {
            yield return new TestClass(++_currentState);
        }
    }
}
