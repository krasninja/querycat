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
    public override Task<int> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    /// <inheritdoc />
    public override bool IsCorrectPluginFile(string file) => false;
}
