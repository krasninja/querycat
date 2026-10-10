using QueryCat.Backend.AssemblyPlugins;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Plugins.Client;

namespace QueryCat.PluginsProxy;

/// <summary>
/// Program entry point.
/// </summary>
public class Program
{
    private const string AssemblyPrefix = "--assembly=";

    public static async Task QueryCatMainAsync(ThriftPluginClientArguments args, string[] assemblyFiles)
    {
        ThriftPluginClient.SetupApplicationLogging(logLevel: args.LogLevel);

        using var client = new ThriftPluginClient(args);
        using var assemblyLoader = new DotNetAssemblyPluginsLoader(client.FunctionsManager, client.ExecutionThread, assemblyFiles);
        await assemblyLoader.LoadAsync(new PluginsLoadingOptions
        {
            SkipLoadingCallbackCall = true,
        });
        if (assemblyLoader.LoadedAssemblies.Count < 1)
        {
            throw new QueryCatException("No plugins loaded.");
        }
        await client.StartAsync(
            SdkConvert.Convert(assemblyLoader.LoadedAssemblies.First()));
        await assemblyLoader.CallOnLoadAsync(CancellationToken.None);
        await client.ReadyAsync(CancellationToken.None);
        await client.WaitForServerExitAsync();
    }

    public static Task Main(string[] args) => QueryCatMainAsync(
        ThriftPluginClient.ConvertCommandLineArguments(args),
        ParseAssemblyFiles(args));

    private static string[] ParseAssemblyFiles(string[] args)
    {
        var assemblies = new List<string>();
        foreach (var arg in args)
        {
            if (!arg.StartsWith(AssemblyPrefix, StringComparison.Ordinal))
            {
                continue;
            }
            assemblies.Add(arg.Substring(AssemblyPrefix.Length));
        }
        return assemblies.ToArray();
    }
}
