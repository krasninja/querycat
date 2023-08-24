using System.ComponentModel;
using System.Diagnostics;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Example simple rows input plugin based on <see cref="ClassRowsInput{TClass}" />.
/// </summary>
public class SamplePluginClassRowsInput : ClassRowsInput<TestClass>
{
    private const long MaxValue = 9;

    [Description("Sample input.")]
    [FunctionSignature("plugin(): object<IRowsInput>")]
    public static VariantValue SamplePlugin(FunctionCallInfo args)
    {
        var rowsSource = new SamplePluginClassRowsInput();
        return VariantValue.CreateFromObject(rowsSource);
    }

    /// <inheritdoc />
    protected override void Initialize(ClassRowsFrameBuilder<TestClass> builder)
    {
        Trace.WriteLine(nameof(Initialize));
        builder.AddProperty(b => b.Key, "Key.");
    }

    /// <inheritdoc />
    protected override void Load()
    {
        Trace.WriteLine(nameof(Load));
        for (var i = 0; i < MaxValue; i++)
        {
            Frame.AddRow(new TestClass(i + 1));
        }
    }
}
