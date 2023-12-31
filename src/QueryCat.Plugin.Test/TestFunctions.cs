using System.ComponentModel;
using System.Text;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Types.Blob;

namespace QueryCat.Plugin.Test;

internal static class TestFunctions
{
    [Description("Test function (combine).")]
    [FunctionSignature("test_combine([int]: integer, str: string, dec: numeric, fl: float, bl: boolean, tim: timestamp, inter: interval): string")]
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

    [Description("Test function (simple).")]
    [FunctionSignature("test_simple(): timestamp")]
    public static VariantValue TestSimpleFunction(FunctionCallInfo args)
    {
        return new VariantValue(DateTime.Now);
    }

    [Description("Test non standard function (simple).")]
    [FunctionSignature("test_simple_2(a: int, b: int): int")]
    public static int TestSimpleNonStandardFunction(int a, int b)
    {
        return new VariantValue(a + b);
    }

    [Description("Test blob function (simple).")]
    [FunctionSignature("test_simple_3(): blob")]
    public static VariantValue TestBlobFunction(FunctionCallInfo args)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < 1000; i++)
        {
            sb.Append("THIS IS THE TEST TEXT ");
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return VariantValue.CreateFromObject(new BytesBlobData(bytes));
    }
}
