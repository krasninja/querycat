using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Samples.Functions;

internal static class TestCombine
{
    [SafeFunction]
    [Description("Test function (combine).")]
    [FunctionSignature("sample_combine([int]: integer, str: string, dec: numeric, fl: float, bl: boolean, tim: timestamp, inter: interval): string")]
    public static VariantValue TestCombineFunction(FunctionCallInfo args)
    {
        // Call: test_combine(1, 'str', 2.5::numeric, 5.2, True, '2023-08-02'::timestamp, interval '3d');
        var integer = args.GetAt(0);
        var str = args.GetAt(1);
        var dec = args.GetAt(2);
        var fl = args.GetAt(3);
        var bl = args.GetAt(4);
        var tim = args.GetAt(5);
        var inter = args.GetAt(6);
        var result = string.Join(" ", integer, str, dec, fl, bl, tim, inter.ToString());
        return new VariantValue(result);
    }
}
