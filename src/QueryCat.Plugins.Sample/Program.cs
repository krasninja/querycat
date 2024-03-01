using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Samples.Functions;
using QueryCat.Plugins.Samples.Inputs;

namespace QueryCat.Plugins.Samples;

/// <summary>
/// Program entry point.
/// </summary>
public class Program
{
    public static void QueryCatMain(ThriftPluginClientArguments args)
    {
        ThriftPluginClient.SetupApplicationLogging(logLevel: LogLevel.Debug);
        AsyncUtils.RunSync(async ct =>
        {
            using var client = new ThriftPluginClient(args);
            client.FunctionsManager.RegisterFunction(AddressIterator.AddressIteratorFunction);
            client.FunctionsManager.RegisterFunction(AddressRowsInput.AddressRowsInputFunction);
            client.FunctionsManager.RegisterFunction(TestCombine.TestCombineFunction);
            client.FunctionsManager.RegisterFunction(TestSimpleNonStandard.TestSimpleNonStandardFunction);
            client.FunctionsManager.RegisterFunction(TestSimple.TestSimpleFunction);
            client.FunctionsManager.RegisterFunction(TestBlob.TestBlobFunction);
            await client.StartAsync(ct);
            await client.WaitForServerExitAsync(ct);
        });
    }

    [UnmanagedCallersOnly(EntryPoint = ThriftPluginClient.PluginMainFunctionName)]
    public static void DllMain(QueryCatPluginArguments args) => QueryCatMain(args.ConvertToPluginClientArguments());

    public static void Main(string[] args) => QueryCatMain(ThriftPluginClient.ConvertCommandLineArguments(args));
}
