using System.ComponentModel;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

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

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Null);
        functionsManager.RegisterFunction(NullOutput.Null);
    }
}
