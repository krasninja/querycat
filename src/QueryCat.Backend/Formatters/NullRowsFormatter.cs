using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Null formatters do nothing and can be used for testing only.
/// </summary>
internal sealed class NullRowsFormatter : IRowsFormatter
{
    [SafeFunction]
    [Description("NULL formatter.")]
    [FunctionSignature("null_fmt(): object<IRowsFormatter>")]
    public static VariantValue Null(IExecutionThread thread)
    {
        var rowsSource = new NullRowsFormatter();
        return VariantValue.CreateFromObject(rowsSource);
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null)
        => NullRowsInput.Instance;

    /// <inheritdoc />
    public IRowsOutput OpenOutput(IBlobData blob)
        => NullRowsOutput.Instance;

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Null);
        functionsManager.RegisterFunction(NullRowsOutput.Null);
    }
}
