using System.ComponentModel;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Formatter for SRT (SubRip) format.
/// </summary>
internal sealed class SubRipFormatter : IRowsFormatter
{
    [Description("SubRip (SRT) formatter.")]
    [FunctionSignature("srt(path: string): object<IRowsFormatter>")]
    public static VariantValue Srt(FunctionCallInfo args)
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

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Srt);
    }
}
