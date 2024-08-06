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
    public override Task<string[]> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<string>());

    /// <inheritdoc />
    public override bool IsCorrectPluginFile(string file) => false;
}
