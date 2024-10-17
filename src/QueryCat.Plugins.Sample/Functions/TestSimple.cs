using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Samples.Functions;

internal static class TestSimple
{
    [SafeFunction]
    [Description("Test function (simple).")]
    [FunctionSignature("sample_simple(): timestamp")]
    public static VariantValue TestSimpleFunction(IExecutionThread thread)
    {
        return new VariantValue(DateTime.Now);
    }
}
