using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Formats the data as a single text line.
/// </summary>
public class TextLineFormatter : IRowsFormatter
{
    [SafeFunction]
    [Description("Text line formatter.")]
    [FunctionSignature("text_line(): object<IRowsFormatter>")]
    public static VariantValue TextLine(IExecutionThread thread)
    {
        var rowsSource = new TextLineFormatter();
        return VariantValue.CreateFromObject(rowsSource);
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null) => new TextLineInput(blob.GetStream(), key);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(IBlobData blob)
    {
        throw new QueryCatException(string.Format(Resources.Errors.FormatterNoOutput, nameof(TextLineFormatter)));
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(TextLine);
    }
}
