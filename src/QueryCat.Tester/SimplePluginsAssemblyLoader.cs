using System.Reflection;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Plugins.Auth;

namespace QueryCat.Tester;

public sealed class SimplePluginsAssemblyLoader : PluginsLoader
{
    private const string RegistrationClassName = "Registration";
    private const string RegisterAuthProvidersMethodName = "RegisterAuthProviders";

    private readonly IFunctionsManager _functionsManager;
    private readonly IAuthProvidersRegistry _authProvidersRegistry;

    /// <inheritdoc />
    public SimplePluginsAssemblyLoader(
        IEnumerable<string> pluginDirectories,
        IFunctionsManager functionsManager,
        IAuthProvidersRegistry authProvidersRegistry) : base(pluginDirectories)
    {
        _functionsManager = functionsManager;
        _authProvidersRegistry = authProvidersRegistry;
    }

    /// <inheritdoc />
    public override Task<int> LoadAsync(PluginsLoadingOptions options, CancellationToken cancellationToken = default)
    {
        var loaded = new List<string>(capacity: PluginDirectories.Count());
        foreach (var pluginFile in PluginDirectories)
        {
            if (!File.Exists(pluginFile) || !IsCorrectPluginFile(pluginFile))
            {
                continue;
            }
            var assembly = Assembly.LoadFrom(pluginFile);
            RegisterFunctions(assembly);
            RegisterAuthProviders(assembly);
            loaded.Add(pluginFile);
        }
        return Task.FromResult(loaded.Count);
    }

    private void RegisterFunctions(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            _functionsManager.RegisterFunctions(_functionsManager.Factory.CreateFromType(type));
        }
    }

    private void RegisterAuthProviders(Assembly assembly)
    {
        // static void RegisterAuthProviders(IAuthProvidersRegistry registry).
        var registerType = assembly.GetType(assembly.GetName().Name + $".{RegistrationClassName}");
        var registerAuthProvidersMethod = registerType?.GetMethod(
            RegisterAuthProvidersMethodName,
            BindingFlags.Public | BindingFlags.Static,
            [typeof(IAuthProvidersRegistry)]);
        if (registerAuthProvidersMethod != null)
        {
            registerAuthProvidersMethod.Invoke(null, [_authProvidersRegistry]);
        }
    }
}
