using Microsoft.Extensions.Logging;
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

    private static readonly Lazy<ILogger> _logger = new(() => Application.LoggerFactory.CreateLogger(nameof(Program)));

    public static async Task QueryCatMainAsync(ThriftPluginClientArguments args, string[] assemblyFiles)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        ThriftPluginClient.SetupApplicationLogging(logLevel: args.LogLevel);

        using var client = new ThriftPluginClient(args);
        var assemblyLoader = new DotNetAssemblyPluginsLoader(client.FunctionsManager, client.ExecutionThread, assemblyFiles);
        await assemblyLoader.LoadAsync(new PluginsLoadingOptions
        {
            SkipLoadingCallbackCall = true,
        });
        if (!assemblyLoader.LoadedAssemblies.Any())
        {
            throw new QueryCatException("No plugins loaded.");
        }
        await client.StartAsync(
            SdkConvert.Convert(assemblyLoader.LoadedAssemblies.FirstOrDefault()));
        await assemblyLoader.CallOnLoadAsync(CancellationToken.None);
        await client.ReadyAsync(CancellationToken.None);
        await client.WaitForServerExitAsync();
    }

    public static Task Main(string[] args) => QueryCatMainAsync(
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

    private static void CurrentDomainOnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            _logger.Value.LogCritical(exception, "Unhandled exception.");
        }
        Environment.Exit(1);
    }
}
