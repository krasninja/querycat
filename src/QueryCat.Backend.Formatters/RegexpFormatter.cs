using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// The formatter allows to use regular expression to split string to columns.
/// </summary>
internal sealed class RegexpFormatter : IRowsFormatter
{
    [Description("Regual expression formatter.")]
    [FunctionSignature("regex(pattern: string): object<IRowsFormatter>")]
    public static VariantValue Regex(FunctionCallInfo args)
    {
        var pattern = args.GetAt(0).AsString;
        return VariantValue.CreateFromObject(new RegexpFormatter(pattern));
    }

    private readonly string _pattern;

    public RegexpFormatter(string pattern)
    {
        _pattern = pattern;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input, string? key = null)
    {
        return new RegexpInput(input, _pattern, key);
    }

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output) => throw new NotImplementedException();

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Regex);
    }
}
