using QueryCat.Backend.AssemblyPlugins;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client;

namespace QueryCat.PluginsProxy;

/// <summary>
/// Program entry point.
/// </summary>
public class Program
{
    private const string AssemblyPrefix = "--assembly=";

    public static void QueryCatMain(ThriftPluginClientArguments args, string[] assemblyFiles)
    {
        ThriftPluginClient.SetupApplicationLogging(logLevel: args.LogLevel);
        AsyncUtils.RunSync(async ct =>
        {
            using var client = new ThriftPluginClient(args);
            var assemblyLoader = new DotNetAssemblyPluginsLoader(client.FunctionsManager, assemblyFiles);
            await assemblyLoader.LoadAsync(ct);
            if (!assemblyLoader.LoadedAssemblies.Any())
            {
                throw new QueryCatException("No plugins loaded.");
            }
            await client.StartAsync(
                SdkConvert.Convert(assemblyLoader.LoadedAssemblies.FirstOrDefault()),
                ct);
            await client.WaitForServerExitAsync(ct);
        });
    }

    public static void Main(string[] args) => QueryCatMain(
        ThriftPluginClient.ConvertCommandLineArguments(args),
        ParseAssemblyFiles(args));

    public static string[] ParseAssemblyFiles(string[] args)
    {
        var assemblies = new List<string>();
        foreach (var arg in args)
        {
            if (!arg.StartsWith(AssemblyPrefix))
            {
                continue;
            }
            assemblies.Add(arg.Substring(AssemblyPrefix.Length));
        }
        return assemblies.ToArray();
    }
}
