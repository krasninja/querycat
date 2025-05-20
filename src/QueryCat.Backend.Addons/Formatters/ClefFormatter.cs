using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Addons.Formatters;

/// <summary>
/// Compact log event formatter (CLEF).
/// </summary>
/// <remarks>
/// URL: https://clef-json.org.
/// </remarks>
internal sealed class ClefFormatter : IRowsFormatter
{
    [SafeFunction]
    [Description("Compact log event formatter (CLEF).")]
    [FunctionSignature("clef(): object<IRowsFormatter>")]
    [FunctionFormatters(".clef", "application/vnd.serilog.clef")]
    public static VariantValue Clef(IExecutionThread thread)
    {
        return VariantValue.CreateFromObject(new ClefFormatter());
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null)
    {
        var stream = blob.GetStream();
        return new ClefInput(new StreamReader(stream), key: key);
    }

    /// <inheritdoc />
    public IRowsOutput OpenOutput(IBlobData blob) => throw new NotImplementedException();

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Clef);
    }
}
