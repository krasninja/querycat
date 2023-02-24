using System.ComponentModel;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

internal class XmlFormatter : IRowsFormatter
{
    [Description("XML formatter.")]
    [FunctionSignature("xml(): object<IRowsFormatter>")]
    public static VariantValue Xml(FunctionCallInfo args)
    {
        var rowsSource = new XmlFormatter();
        return VariantValue.CreateFromObject(rowsSource);
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input)
        => new XmlInput(new StreamReader(input));

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
        => new XmlOutput(output);

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Xml);
    }
}
