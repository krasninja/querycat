using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Addons.Formatters;

internal class XmlFormatter : IRowsFormatter
{
    [SafeFunction]
    [Description("XML formatter.")]
    [FunctionSignature("xml(xpath?: string): object<IRowsFormatter>")]
    [FunctionFormatters(".xml", ".xsd", "application/xml", "application/xhtml+xml", "application/soap+xml")]
    public static VariantValue Xml(IExecutionThread thread)
    {
        var rowsSource = new XmlFormatter(thread.Stack.GetAtOrDefault(0).AsString);
        return VariantValue.CreateFromObject(rowsSource);
    }

    private readonly string? _xpath;

    public XmlFormatter(string? xpath = null)
    {
        _xpath = xpath;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null)
    {
        var stream = blob.GetStream();
        return new XmlInput(stream, _xpath, key ?? string.Empty);
    }

    /// <inheritdoc />
    public IRowsOutput OpenOutput(IBlobData blob)
        => new XmlOutput(blob.GetStream());

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Xml);
    }
}
