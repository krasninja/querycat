using System.Reflection;
using QueryCat.Backend.AssemblyPlugins;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Client;
using QueryCat.Plugins.Sdk;

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
                GetPluginDataFromAssembly(assemblyLoader.LoadedAssemblies.FirstOrDefault()),
                ct);
            await client.WaitForServerExitAsync(ct);
        });
    }

    private static PluginData? GetPluginDataFromAssembly(Assembly? assembly)
    {
        if (assembly == null)
        {
            return null;
        }
        var assemblyName = assembly.GetName();
        return new PluginData
        {
            Name = assemblyName.Name ?? string.Empty,
            Version = assemblyName.Version?.ToString() ?? "0.0.0",
        };
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
