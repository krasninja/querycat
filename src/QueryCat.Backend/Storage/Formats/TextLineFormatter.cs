using System.ComponentModel;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage.Formats;

/// <summary>
/// Formats the data as a single text line.
/// </summary>
public class TextLineFormatter : IRowsFormatter
{
    [Description("Text line formatter.")]
    [FunctionSignature("textline(): object<IRowsFormatter>")]
    public static VariantValue TextLine(FunctionCallInfo args)
    {
        var rowsSource = new TextLineFormatter();
        return VariantValue.CreateFromObject(rowsSource);
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input) => new TextLineInput(input);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
    {
        throw new NotImplementedException();
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(TextLine);
    }
}
