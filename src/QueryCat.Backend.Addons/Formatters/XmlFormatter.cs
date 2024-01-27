using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Addons.Formatters;

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
    public IRowsInput OpenInput(Stream input, string? key = null)
        => new XmlInput(new StreamReader(input), _xpath, key ?? string.Empty);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
        => new XmlOutput(output);

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Xml);
    }
}
