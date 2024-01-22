namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// Plugin loading system with no implementation.
/// </summary>
public class NullPluginsLoader : PluginsLoader
{
    /// <summary>
    /// Instance of <see cref="NullPluginsLoader" />.
    /// </summary>
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
