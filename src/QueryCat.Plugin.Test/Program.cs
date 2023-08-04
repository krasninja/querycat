using QueryCat.Plugins.Client;

namespace QueryCat.Plugin.Test;

/// <summary>
/// Program entry point.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        ThriftPluginClient.SetupApplicationLogging();
        using var client = new ThriftPluginClient(args);
        client.FunctionsManager.RegisterFromType(typeof(AddressIterator));
        client.FunctionsManager.RegisterFromType(typeof(AddressRowsInput));
        client.FunctionsManager.RegisterFunction(TestFunctions.TestCombineFunction);
        await client.Start();
        await client.WaitForParentProcessExitAsync();
    }
}
