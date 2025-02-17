using System.Reflection;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Tester;

public sealed class SimplePluginsAssemblyLoader : PluginsLoader
{
    private readonly IFunctionsManager _functionsManager;

    /// <inheritdoc />
    public SimplePluginsAssemblyLoader(IEnumerable<string> pluginDirectories, IFunctionsManager functionsManager) : base(pluginDirectories)
    {
        _functionsManager = functionsManager;
    }

    /// <inheritdoc />
    public override Task<string[]> LoadAsync(CancellationToken cancellationToken = default)
    {
        var loaded = new List<string>();
        foreach (var pluginFile in PluginDirectories)
        {
            if (!File.Exists(pluginFile) || !IsCorrectPluginFile(pluginFile))
            {
                continue;
            }
            var assembly = Assembly.LoadFrom(pluginFile);
            RegisterFunctions(assembly);
            loaded.Add(pluginFile);
        }
        return Task.FromResult(loaded.ToArray());
    }

    private void RegisterFunctions(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            _functionsManager.RegisterFunctions(_functionsManager.Factory.CreateFromType(type));
        }
    }
}
