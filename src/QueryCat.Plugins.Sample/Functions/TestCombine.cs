using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Samples.Functions;

internal static class TestCombine
{
    [SafeFunction]
    [Description("Test function (combine).")]
    [FunctionSignature("sample_combine([int]: integer, str: string, dec: numeric, fl: float, bl: boolean, tim: timestamp, inter: interval): string")]
    public static VariantValue TestCombineFunction(IExecutionThread thread)
    {
        // Call: test_combine(1, 'str', 2.5::numeric, 5.2, True, '2023-08-02'::timestamp, interval '3d');
        var integer = thread.Stack[0];
        var str = thread.Stack[1];
        var dec = thread.Stack[2];
        var fl = thread.Stack[3];
        var bl = thread.Stack[4];
        var tim = thread.Stack[5];
        var inter = thread.Stack[6];
        var result = string.Join(" ", integer, str, dec, fl, bl, tim, inter.ToString());
        return new VariantValue(result);
    }
}
