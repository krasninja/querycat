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
    [UnmanagedCallersOnly(EntryPoint = ThriftPluginClient.PluginMainFunctionName)]
    public static void DllMain(QueryCatPluginArguments args)
    {
        ThriftPluginClient.SetupApplicationLogging();
        AsyncUtils.RunSync(async () =>
        {
            using var client = new ThriftPluginClient(args.ConvertToPluginClientArguments());
            client.FunctionsManager.RegisterFromType(typeof(AddressIterator));
            client.FunctionsManager.RegisterFromType(typeof(AddressRowsInput));
            client.FunctionsManager.RegisterFunction(TestFunctions.TestCombineFunction);
            client.FunctionsManager.RegisterFunction(TestFunctions.TestSimpleNonStandardFunction);
            client.FunctionsManager.RegisterFunction(TestFunctions.TestSimpleFunction);
            await client.Start();
            await client.WaitForServerExitAsync();
        });
    }

    public static async Task Main(string[] args)
    {
        ThriftPluginClient.SetupApplicationLogging();
        using var client = new ThriftPluginClient(args);
        client.FunctionsManager.RegisterFromType(typeof(AddressIterator));
        client.FunctionsManager.RegisterFromType(typeof(AddressRowsInput));
        client.FunctionsManager.RegisterFunction(TestFunctions.TestCombineFunction);
        client.FunctionsManager.RegisterFunction(TestFunctions.TestSimpleNonStandardFunction);
        client.FunctionsManager.RegisterFunction(TestFunctions.TestSimpleFunction);
        await client.Start();
        await client.WaitForServerExitAsync();
    }
}
