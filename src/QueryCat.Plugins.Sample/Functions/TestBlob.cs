using System.ComponentModel;
using System.Text;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Samples.Functions;

internal static class TestBlob
{
    [SafeFunction]
    [Description("Test blob function (simple).")]
    [FunctionSignature("sample_simple_3(): blob")]
    public static VariantValue TestBlobFunction(IExecutionThread thread)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < 1000; i++)
        {
            sb.Append("THIS IS THE TEST TEXT ");
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return VariantValue.CreateFromObject(new StreamBlobData(() => new MemoryStream(bytes)));
    }
}
