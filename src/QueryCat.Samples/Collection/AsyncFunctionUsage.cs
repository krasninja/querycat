using System.ComponentModel;
using System.Text;
using QueryCat.Backend;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Samples.Collection;

internal class AsyncFunctionUsage : BaseUsage
{
    [Description("Async demo function.")]
    [FunctionSignature("async_demo(): string")]
    public static async Task<VariantValue> AsyncDemo(FunctionCallInfo args)
    {
        const string target = "Hello World!";
        var ms = new MemoryStream();
        await ms.WriteAsync(Encoding.UTF8.GetBytes(target));
        await ms.FlushAsync();
        ms.Seek(0, SeekOrigin.Begin);

        var str = Encoding.UTF8.GetString(ms.ToArray());
        return new VariantValue(str);
    }

    /// <inheritdoc />
    public override void Run()
    {
        var executionThread = new ExecutionThreadBootstrapper().Create();
        executionThread.FunctionsManager.RegisterFunction(AsyncDemo);

        var result = executionThread.Run("async_demo()");
        Console.WriteLine(result.ToString()); // Hello World!
    }
}
