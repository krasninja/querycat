using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// The formatter for raw input and output.
/// </summary>
internal sealed class RawValueFormatter : IRowsFormatter
{
    [SafeFunction]
    [Description("Raw formatter.")]
    [FunctionFormatters(".bin", ".raw", MimeTypesProvider.ContentTypeOctetStream)]
    [FunctionSignature("raw_fmt(): object<IRowsFormatter>")]
    public static VariantValue Raw(IExecutionThread thread)
    {
        var rowsSource = new RawValueFormatter();
        return VariantValue.CreateFromObject(rowsSource);
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null) => new RawValueInput(blob);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(IBlobData blob) => new RawValueOutput(blob);

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Raw);
    }
}
