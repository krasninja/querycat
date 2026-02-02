using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Labeled Tab-separated Values (LTSV) format.
/// </summary>
/// <remarks>
/// For reference: http://ltsv.org.
/// </remarks>
internal class LtsvFormatter : IRowsFormatter
{
    [Description("LTSV formatter.")]
    [FunctionSignature("ltsv(): object<IRowsFormatter>")]
    [FunctionFormatters(".ltsv")]
    public static VariantValue Ltsv(IExecutionThread thread)
    {
        var rowsFormatter = new LtsvFormatter();
        return VariantValue.CreateFromObject(rowsFormatter);
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null) => new LtsvInput(blob.GetStream(), key: key);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(IBlobData blob)
    {
        throw new QueryCatException(string.Format(Resources.Errors.FormatterNoOutput, nameof(LtsvFormatter)));
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Ltsv);
    }
}
