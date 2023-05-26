using System.ComponentModel;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

internal class XmlFormatter : IRowsFormatter
{
    [Description("XML formatter.")]
    [FunctionSignature("xml(xpath?: string): object<IRowsFormatter>")]
    public static VariantValue Xml(FunctionCallInfo args)
    {
        var rowsSource = new XmlFormatter(args.GetAtOrDefault(0).AsString);
        return VariantValue.CreateFromObject(rowsSource);
    }

    private readonly string? _xpath;

    public XmlFormatter(string? xpath = null)
    {
        _xpath = xpath;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input)
        => new XmlInput(new StreamReader(input), _xpath);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
        => new XmlOutput(output);

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Xml);
    }
}
