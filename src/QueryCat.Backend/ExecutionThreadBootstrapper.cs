using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Functions.Aggregate;
using QueryCat.Backend.Functions.UriResolvers;
using QueryCat.Backend.FunctionsManager;
using QueryCat.Backend.Parser;

namespace QueryCat.Backend;

/// <summary>
/// The facade class that contains workflow to run query from string.
/// </summary>
public sealed class ExecutionThreadBootstrapper(ExecutionOptions? options = null)
{
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ExecutionThreadBootstrapper));

    private readonly ExecutionOptions _executionOptions = options ?? new ExecutionOptions();

    private IInputConfigStorage _inputConfigStorage = NullInputConfigStorage.Instance;

    private IFunctionsManager? _functionsManager;

    private IObjectSelector? _objectSelector;

    private bool _registerStandardLibrary;

    private int _cacheSize;
    private int _maxQueryLength;

    private readonly List<Action<IFunctionsManager>> _registrations = new();

    private Func<IExecutionThread, PluginsLoader> _pluginsLoaderFactory = _ => new NullPluginsLoader(Array.Empty<string>());

    private Func<PluginsLoader, IPluginsManager> _pluginsManagerFactory = _ => new NullPluginsManager();

    private readonly List<IUriResolver> _uriResolvers = new();

    private readonly List<ICompletionSource> _completionSources = new();

    private object? _tag;

    private bool _addStandardCompletions;

    /// <summary>
    /// Use the custom config storage.
    /// </summary>
    /// <param name="configStorage">Instance of <see cref="IInputConfigStorage" />.</param>
    /// <returns>The instance of <see cref="ExecutionThreadBootstrapper" />.</returns>
    public ExecutionThreadBootstrapper WithConfigStorage(IInputConfigStorage configStorage)
    {
        _inputConfigStorage = configStorage;
        return this;
    }

    /// <summary>
    /// Use the specific functions' manager.
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
    /// Use custom objects selector to resolve object expressions (like "user.property[ind]").
    /// </summary>
    /// <param name="objectSelector">Custom object selector.</param>
    /// <returns>The instance of <see cref="ExecutionThreadBootstrapper" />.</returns>
    public ExecutionThreadBootstrapper WithObjectsSelector(IObjectSelector objectSelector)
    {
        _objectSelector = objectSelector;
        return this;
    }

    /// <summary>
    /// Use CURL and directory resolvers for "SELECT FROM" clause.
    /// </summary>
    /// <returns>The instance of <see cref="ExecutionThreadBootstrapper" />.</returns>
    public ExecutionThreadBootstrapper WithStandardUriResolvers()
    {
        _uriResolvers.Add(new CurlUriResolver());
        _uriResolvers.Add(new DirectoryUriResolver());
        _uriResolvers.Add(new FileUriResolver());
        return this;
    }

    /// <summary>
    /// Add functions registrations.
    /// </summary>
    /// <param name="registrations">Registration delegates.</param>
    /// <returns>The instance of <see cref="ExecutionThreadBootstrapper" />.</returns>
    public ExecutionThreadBootstrapper WithRegistrations(params IEnumerable<Action<IFunctionsManager>> registrations)
    {
        _registrations.AddRange(registrations);
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
    /// With tag (custom user information).
    /// </summary>
    /// <param name="tag">Custom user information object.</param>
    /// <returns>Instance of <see cref="ExecutionThread" />.</returns>
    public ExecutionThreadBootstrapper WithTag(object? tag)
    {
        _tag = tag;
        return this;
    }

    /// <summary>
    /// Use AST cache. Can be useful if the same query is executed several times. Experimental feature.
    /// </summary>
    /// <param name="cacheSize">Max cache size.</param>
    /// <param name="maxQueryLength">Max query length.</param>
    /// <returns>Instance of <see cref="ExecutionThread" />.</returns>
    public ExecutionThreadBootstrapper WithAstCache(int cacheSize = 21, int maxQueryLength = 150)
    {
        _cacheSize = cacheSize;
        _maxQueryLength = maxQueryLength;
        return this;
    }

    /// <summary>
    /// Use standard internal completion sources.
    /// </summary>
    /// <returns>Instance of <see cref="ExecutionThread" />.</returns>
    public ExecutionThreadBootstrapper WithStandardCompletionSources()
    {
        _addStandardCompletions = true;
        return this;
    }

    /// <summary>
    /// Use completion source.
    /// </summary>
    /// <param name="completionSource">Completion source instance.</param>
    /// <returns>Instance of <see cref="ExecutionThread" />.</returns>
    public ExecutionThreadBootstrapper WithCompletionSource(ICompletionSource completionSource)
    {
        _completionSources.Add(completionSource);
        return this;
    }

    /// <summary>
    /// Create the instance of execution thread.
    /// </summary>
    /// <returns>Instance of <see cref="ExecutionThread" />.</returns>
    public ExecutionThread Create()
    {
#if DEBUG
        var timer = new System.Diagnostics.Stopwatch();
        timer.Start();
#endif

        // Create functions manager.
        if (_functionsManager == null)
        {
            _functionsManager = new DefaultFunctionsManager(new AstBuilder(), _uriResolvers);
        }

        // Add completions.
        if (_addStandardCompletions)
        {
            _completionSources.Add(new KeywordsCompletionSource());
            _completionSources.Add(new VariablesCompletionSource());
            _completionSources.Add(new ObjectPropertiesCompletionSource());
            _completionSources.Add(new FunctionsCompletionSource(_functionsManager));
        }

        // Create objects selector.
        if (_objectSelector == null)
        {
            _objectSelector = new DefaultObjectSelector();
        }

        // Create thread.
        var astBuilder = _cacheSize > 0
            ? new AstBuilder(
                new SimpleLruDictionary<string, IAstNode>(_cacheSize),
                _maxQueryLength > 0 ? _maxQueryLength : 150)
            : new AstBuilder();
        // Keep only one combined completion source.
        var completionSource = _completionSources.Count == 1 && _completionSources[0] is CombineCompletionSource
            ? _completionSources[0]
            : new CombineCompletionSource(_completionSources, _executionOptions.CompletionsCount);
        var thread = new ExecutionThread(
            options: _executionOptions,
            functionsManager: _functionsManager,
            objectSelector: _objectSelector,
            configStorage: _inputConfigStorage,
            astBuilder: astBuilder,
            completionSource: completionSource
        );
        thread.Tag = _tag;

        // Register functions.
        if (_registerStandardLibrary)
        {
            StringFunctions.RegisterFunctions(_functionsManager);
            MathFunctions.RegisterFunctions(_functionsManager);
            DateTimeFunctions.RegisterFunctions(_functionsManager);
            IOFunctions.RegisterFunctions(_functionsManager);
            AggregatesRegistration.RegisterFunctions(_functionsManager);
            Formatters.Registration.Register(_functionsManager);
            Inputs.Registration.RegisterFunctions(_functionsManager);
            CryptoFunctions.RegisterFunctions(_functionsManager);
            MiscFunctions.RegisterFunctions(_functionsManager);
            InfoFunctions.RegisterFunctions(_functionsManager);
        }
        foreach (var registration in _registrations)
        {
            registration.Invoke(_functionsManager);
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
