using System.Runtime.InteropServices;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client;

namespace QueryCat.Plugins.Sample;

/// <summary>
/// Program entry point.
/// </summary>
public class Program
{
    public static void QueryCatMain(ThriftPluginClientArguments args)
    {
        ThriftPluginClient.SetupApplicationLogging(logLevel: args.LogLevel);
        AsyncUtils.RunSync(async ct =>
        {
            using var client = new ThriftPluginClient(args);
            Registration.RegisterFunctions(client.FunctionsManager);
            await Registration.OnLoadAsync(client.ExecutionThread, ct);
            await client.StartAsync(cancellationToken: ct);
            await client.ReadyAsync(cancellationToken: ct);
            await client.WaitForServerExitAsync(ct);
        });
    }

    [UnmanagedCallersOnly(EntryPoint = ThriftPluginClient.PluginMainFunctionName)]
    public static void DllMain(QueryCatPluginArguments args) => QueryCatMain(args.ConvertToPluginClientArguments());

    public static void Main(string[] args) => QueryCatMain(ThriftPluginClient.ConvertCommandLineArguments(args));
}
