using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Addons.Formatters;

/// <summary>
/// The formatter allows to use regular expression to split string to columns.
/// </summary>
internal sealed class RegexpFormatter : IRowsFormatter
{
    [SafeFunction]
    [Description("Regular expression formatter.")]
    [FunctionSignature("regex(pattern: string, flags?: string): object<IRowsFormatter>")]
    public static VariantValue Regex(FunctionCallInfo args)
    {
        var pattern = args.GetAt(0).AsString;
        var flags = args.GetAt(1).AsString;
        return VariantValue.CreateFromObject(new RegexpFormatter(pattern, flags));
    }

    private readonly string _pattern;
    private readonly string? _flags;

    public RegexpFormatter(string pattern, string? flags = null)
    {
        _pattern = pattern;
        _flags = flags;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input, string? key = null)
    {
        return new RegexpInput(input, _pattern, _flags, key);
    }

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output) => throw new NotImplementedException();

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Regex);
    }
}
