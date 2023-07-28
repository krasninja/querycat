using QueryCat.Backend.Abstractions.Plugins;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Plugin loading system with no implementation.
/// </summary>
public class NullPluginsLoader : PluginsLoader
{
    public static NullPluginsLoader Instance { get; } = new(Array.Empty<string>());

    /// <inheritdoc />
    public NullPluginsLoader(IEnumerable<string> pluginDirectories) : base(pluginDirectories)
    {
    }

    /// <inheritdoc />
    public override Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public override bool IsCorrectPluginFile(string file) => false;
}
