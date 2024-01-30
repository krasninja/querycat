using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Addons.Formatters;

internal sealed class GrokFormatter : IRowsFormatter
{
    [Description("Grok expression formatter.")]
    [FunctionSignature("grok(pattern: string): object<IRowsFormatter>")]
    public static VariantValue Grok(FunctionCallInfo args)
    {
        var pattern = args.GetAt(0).AsString;
        return VariantValue.CreateFromObject(new GrokFormatter(pattern));
    }

    private readonly string _pattern;

    public GrokFormatter(string pattern)
    {
        _pattern = pattern;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input, string? key = null)
    {
        return new GrokInput(input, _pattern, key);
    }

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output) => throw new NotImplementedException();

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Grok);
    }
}
