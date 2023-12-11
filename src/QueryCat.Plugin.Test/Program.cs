using System.Runtime.InteropServices;
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
        ThriftPluginClient.SetupApplicationLogging();
        AsyncUtils.RunSync(async () =>
        {
            using var client = new ThriftPluginClient(args);
            client.FunctionsManager.RegisterFromType(typeof(AddressIterator));
            client.FunctionsManager.RegisterFromType(typeof(AddressRowsInput));
            client.FunctionsManager.RegisterFunction(TestFunctions.TestCombineFunction);
            client.FunctionsManager.RegisterFunction(TestFunctions.TestSimpleNonStandardFunction);
            client.FunctionsManager.RegisterFunction(TestFunctions.TestSimpleFunction);
            await client.StartAsync();
            await client.WaitForServerExitAsync();
        });
    }

    [UnmanagedCallersOnly(EntryPoint = ThriftPluginClient.PluginMainFunctionName)]
    public static void DllMain(QueryCatPluginArguments args) => Main(args.ConvertToPluginClientArguments());

    public static void Main(string[] args) => Main(ThriftPluginClient.ConvertCommandLineArguments(args));
}
