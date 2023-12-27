using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Functions.Aggregate;
using QueryCat.Backend.FunctionsManager;

namespace QueryCat.Backend;

/// <summary>
/// The facade class that contains workflow to run query from string.
/// </summary>
public sealed class ExecutionThreadBootstrapper(ExecutionOptions? options = null)
{
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ExecutionThreadBootstrapper));

    private readonly ExecutionOptions _executionOptions = options ?? new ExecutionOptions();

    private IInputConfigStorage _inputConfigStorage = new NullInputConfigStorage();

    private IFunctionsManager _functionsManager = new DefaultFunctionsManager();

    private bool _registerStandardLibrary;

    private Action<IFunctionsManager>[] _registrations = Array.Empty<Action<IFunctionsManager>>();

    private Func<IExecutionThread, PluginsLoader> _pluginsLoaderFactory = _ => new NullPluginsLoader(Array.Empty<string>());

    private Func<PluginsLoader, IPluginsManager> _pluginsManagerFactory = _ => new NullPluginsManager();

    public ExecutionThreadBootstrapper WithConfigStorage(IInputConfigStorage configStorage)
    {
        _inputConfigStorage = configStorage;
        return this;
    }

    /// <summary>
    /// Use the specific functions manager.
    /// </summary>
    /// <param name="functionsManager">Functions manager.</param>
    /// <returns>The instance of <see cref="ExecutionThreadBootstrapper" />.</returns>
    public ExecutionThreadBootstrapper WithFunctionsManager(IFunctionsManager functionsManager)
    {
        _functionsManager = functionsManager;
        return this;
    }

    /// <summary>
    /// Add standard functions.
    /// </summary>
    /// <returns>The instance of <see cref="ExecutionThreadBootstrapper" />.</returns>
    public ExecutionThreadBootstrapper WithStandardFunctions()
    {
        _registerStandardLibrary = true;
        return this;
    }

    /// <summary>
    /// Add functions registrations.
    /// </summary>
    /// <param name="registrations">Registration delegates.</param>
    /// <returns>The instance of <see cref="ExecutionThreadBootstrapper" />.</returns>
    public ExecutionThreadBootstrapper WithRegistrations(params Action<IFunctionsManager>[] registrations)
    {
        _registrations = registrations;
        return this;
    }

    /// <summary>
    /// Add plugin loader.
    /// </summary>
    /// <param name="pluginsLoaderFactory">Plugin loader factory.</param>
    /// <returns>The instance of <see cref="ExecutionThreadBootstrapper" />.</returns>
    public ExecutionThreadBootstrapper WithPluginsLoader(Func<IExecutionThread, PluginsLoader> pluginsLoaderFactory)
    {
        _pluginsLoaderFactory = pluginsLoaderFactory;
        return this;
    }

    /// <summary>
    /// Add the specific plugin manager.
    /// </summary>
    /// <param name="pluginsManagerFactory">Plugin manager factory.</param>
    /// <returns>The instance of <see cref="ExecutionThreadBootstrapper" />.</returns>
    public ExecutionThreadBootstrapper WithPluginsManager(Func<PluginsLoader, IPluginsManager> pluginsManagerFactory)
    {
        _pluginsManagerFactory = pluginsManagerFactory;
        return this;
    }

    /// <summary>
    /// Create the instance of execution thread.
    /// </summary>
    /// <returns>Instance of <see cref="ExecutionThread" />.</returns>
    public ExecutionThread Create()
    {
#if DEBUG
        var timer = new Stopwatch();
        timer.Start();
#endif

        // Create thread.
        var thread = new ExecutionThread(
            _executionOptions,
            functionsManager: _functionsManager,
            configStorage: _inputConfigStorage
        );
        thread.Statistic.CountErrorRows = thread.Options.ShowDetailedStatistic;

        // Register functions.
        if (_registerStandardLibrary)
        {
            thread.FunctionsManager.RegisterFactory(StringFunctions.RegisterFunctions);
            thread.FunctionsManager.RegisterFactory(CryptoFunctions.RegisterFunctions);
            thread.FunctionsManager.RegisterFactory(DateTimeFunctions.RegisterFunctions);
            thread.FunctionsManager.RegisterFactory(InfoFunctions.RegisterFunctions);
            thread.FunctionsManager.RegisterFactory(MathFunctions.RegisterFunctions);
            thread.FunctionsManager.RegisterFactory(MiscFunctions.RegisterFunctions);
            thread.FunctionsManager.RegisterFactory(JsonFunctions.RegisterFunctions);
            thread.FunctionsManager.RegisterFactory(ObjectFunctions.RegisterFunctions);
            thread.FunctionsManager.RegisterFactory(AggregatesRegistration.RegisterFunctions);
            thread.FunctionsManager.RegisterFactory(Inputs.Registration.RegisterFunctions);
            thread.FunctionsManager.RegisterFactory(IOFunctions.RegisterFunctions);
            thread.FunctionsManager.RegisterFactory(Formatters.Registration.Register, postpone: false);
        }
        foreach (var registration in _registrations)
        {
            thread.FunctionsManager.RegisterFactory(registration, postpone: false);
        }

        // Load plugins.
        var pluginLoader = _pluginsLoaderFactory.Invoke(thread);
        AsyncUtils.RunSync(pluginLoader.LoadAsync);
        thread.PluginsManager = _pluginsManagerFactory.Invoke(pluginLoader);

#if DEBUG
        timer.Stop();
        _logger.LogTrace("Bootstrap time: {Time}.", timer.Elapsed);
#endif

        return thread;
    }
}
