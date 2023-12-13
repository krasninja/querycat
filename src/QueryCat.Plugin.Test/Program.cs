using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client;

namespace QueryCat.Plugin.Test;

/// <summary>
/// Program entry point.
/// </summary>
public class Program
{
    public static void Main(ThriftPluginClientArguments args)
    {
        ThriftPluginClient.SetupApplicationLogging(logLevel: LogLevel.Debug);
        AsyncUtils.RunSync(async ct =>
        {
            using var client = new ThriftPluginClient(args);
            client.FunctionsManager.RegisterFromType(typeof(AddressIterator));
            client.FunctionsManager.RegisterFromType(typeof(AddressRowsInput));
            client.FunctionsManager.RegisterFunction(TestFunctions.TestCombineFunction);
            client.FunctionsManager.RegisterFunction(TestFunctions.TestSimpleNonStandardFunction);
            client.FunctionsManager.RegisterFunction(TestFunctions.TestSimpleFunction);
            client.FunctionsManager.RegisterFunction(TestFunctions.TestBlobFunction);
            await client.StartAsync(ct);
            await client.WaitForServerExitAsync(ct);
        });
    }

    [UnmanagedCallersOnly(EntryPoint = ThriftPluginClient.PluginMainFunctionName)]
    public static void DllMain(QueryCatPluginArguments args) => Main(args.ConvertToPluginClientArguments());

    public static void Main(string[] args) => Main(ThriftPluginClient.ConvertCommandLineArguments(args));
}
