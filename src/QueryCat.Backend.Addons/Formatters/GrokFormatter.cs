using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Addons.Formatters;

internal sealed class GrokFormatter : IRowsFormatter
{
    [SafeFunction]
    [Description("Grok expression formatter.")]
    [FunctionSignature("grok(pattern: string): object<IRowsFormatter>")]
    public static VariantValue Grok(IExecutionThread thread)
    {
        var pattern = thread.Stack.Pop().AsString;
        return VariantValue.CreateFromObject(new GrokFormatter(pattern));
    }

    private readonly string _pattern;

    public GrokFormatter(string pattern)
    {
        _pattern = pattern;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null)
    {
        return new GrokInput(blob.GetStream(), _pattern, key);
    }

    /// <inheritdoc />
    public IRowsOutput OpenOutput(IBlobData blob) => throw new NotImplementedException();

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Grok);
    }
}
