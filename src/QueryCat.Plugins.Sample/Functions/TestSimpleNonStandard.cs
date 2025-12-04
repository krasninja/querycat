using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Sample.Functions;

internal static class TestSimpleNonStandard
{
    [SafeFunction]
    [Description("Test non standard function (simple).")]
    [FunctionSignature("sample_simple_2(a: int, b: int): int")]
    public static int? TestSimpleNonStandardFunction(int a, int b)
    {
        return new VariantValue(a + b);
    }
}
