using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core;

/// <summary>
/// Execution thread that does nothing.
/// </summary>
public sealed class NullExecutionThread : IExecutionThread
{
    public static NullExecutionThread Instance { get; } = new();

    /// <inheritdoc />
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    /// <inheritdoc />
    public FunctionsManager FunctionsManager { get; } = NullFunctionsManager.Instance;

    /// <inheritdoc />
    public PluginsManager PluginsManager { get; } = NullPluginsManager.Instance;

    /// <inheritdoc />
    public IInputConfigStorage ConfigStorage { get; } = NullInputConfigStorage.Instance;

    /// <inheritdoc />
    public VariantValue Run(string query) => VariantValue.Null;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}