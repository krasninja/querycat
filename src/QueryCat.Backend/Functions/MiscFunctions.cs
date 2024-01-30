using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Miscellaneous functions.
/// </summary>
internal static class MiscFunctions
{
    [SafeFunction]
    [Description("The function returns a null value if value1 equals value2; otherwise it returns value1.")]
    [FunctionSignature("[nullif](value1: any, value2: any): any")]
    public static VariantValue NullIf(FunctionCallInfo args)
    {
        var value1 = args.GetAt(0);
        var value2 = args.GetAt(1);
        if (VariantValue.Equals(in value1, in value2, out _))
        {
            return VariantValue.Null;
        }
        return value1;
    }

    [SafeFunction]
    [Description("Not operation. The function can be used to suppress output.")]
    [FunctionSignature("nop(...args: any[]): void")]
    public static VariantValue Nop(FunctionCallInfo args)
    {
        return VariantValue.Null;
    }

    [SafeFunction]
    [Description("The function returns a version 4 (random) UUID.")]
    [FunctionSignature("uuid(): string")]
    public static VariantValue GetRandomGuid(FunctionCallInfo args)
    {
        return new VariantValue(Guid.NewGuid().ToString("D"));
    }

    [SafeFunction]
    [Description("Converts a size in bytes into a more easily human-readable format with size units.")]
    [FunctionSignature("size_pretty(size: integer, base: integer = 1024): string")]
    public static VariantValue SizePretty(FunctionCallInfo args)
    {
        if (args.GetAt(0).IsNull)
        {
            return VariantValue.Null;
        }

        var byteCount = args.GetAt(0).AsInteger;
        var @base = args.GetAt(1).AsInteger;

        // For reference: https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net.
        string[] suffix = ["B", "K", "M", "G", "T", "P", "E"];
        if (byteCount == 0)
        {
            return new VariantValue("0 " + suffix[0]);
        }
        var bytes = Math.Abs(byteCount);
        var place = Convert.ToInt64(Math.Floor(Math.Log(bytes, @base)));
        var num = Math.Round(bytes / Math.Pow(@base, place), 1);
        var size = string.Concat(Math.Sign(byteCount) * num, ' ', suffix[place]);

        return new VariantValue(size);
    }

    [SafeFunction]
    [Description("Returns the object itself. Needed when you need to pass variable as function call.")]
    [FunctionSignature("self(target: any): any")]
    public static VariantValue Self(FunctionCallInfo args)
    {
        return args.GetAt(0);
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(NullIf);
        functionsManager.RegisterFunction(Nop);
        functionsManager.RegisterFunction(GetRandomGuid);
        functionsManager.RegisterFunction(SizePretty);
        functionsManager.RegisterFunction(Self);
    }
}
