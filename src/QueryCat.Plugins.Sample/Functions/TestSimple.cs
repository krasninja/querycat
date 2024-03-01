using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Samples.Functions;

internal static class TestSimple
{
    [SafeFunction]
    [Description("Test function (simple).")]
    [FunctionSignature("test_simple(): timestamp")]
    public static VariantValue TestSimpleFunction(FunctionCallInfo args)
    {
        return new VariantValue(DateTime.Now);
    }
}
