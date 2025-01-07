using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Addons.Formatters;

/// <summary>
/// Formatter for SRT (SubRip) format.
/// </summary>
internal sealed class SubRipFormatter : IRowsFormatter
{
    [SafeFunction]
    [Description("SubRip (SRT) formatter.")]
    [FunctionSignature("srt(path: string): object<IRowsFormatter>")]
    [FunctionFormatters(".srt", "application/x-subrip")]
    public static VariantValue Srt(IExecutionThread thread)
    {
        var rowsSource = new SubRipFormatter();
        return VariantValue.CreateFromObject(rowsSource);
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input, string? key = null)
        => new SubRipInput(new StreamReader(input), keys: key ?? string.Empty);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
    {
        throw new QueryCatException($"{nameof(SubRipInput)} does not support output.");
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Srt);
    }
}
