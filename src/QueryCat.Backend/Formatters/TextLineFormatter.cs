using System.ComponentModel;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Formats the data as a single text line.
/// </summary>
public class TextLineFormatter : IRowsFormatter
{
    [Description("Text line formatter.")]
    [FunctionSignature("text_line(): object<IRowsFormatter>")]
    public static VariantValue TextLine(FunctionCallInfo args)
    {
        var rowsSource = new TextLineFormatter();
        return VariantValue.CreateFromObject(rowsSource);
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input, string? key = null) => new TextLineInput(input, key);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
    {
        throw new QueryCatException($"{nameof(TextLineFormatter)} does not support output.");
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(TextLine);
    }
}
