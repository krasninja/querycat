using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Null formatters do nothing and can be used for testing only.
/// </summary>
internal sealed class NullFormatter : IRowsFormatter
{
    [Description("NULL formatter.")]
    [FunctionSignature("null_fmt(): object<IRowsFormatter>")]
    public static VariantValue Null(FunctionCallInfo args)
    {
        var rowsSource = new NullFormatter();
        return VariantValue.CreateFromObject(rowsSource);
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input, string? key = null)
        => new NullRowsInput();

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
        => new NullOutput();

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Null);
        functionsManager.RegisterFunction(NullOutput.Null);
    }
}
