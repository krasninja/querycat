using System.Text.RegularExpressions;

namespace QueryCat.Backend.Abstractions.Plugins;

/// <summary>
/// Plugin information.
/// </summary>
public class PluginInfo
{
    private static readonly Regex KeyRegex = new(@"^(?<name>[a-zA-Z\.]+)\.(?<version>\d+\.\d+\.\d+)\.(dll|nupkg)$",
        RegexOptions.Compiled);

    private static readonly Regex NewKeyRegex = new(@"^(?<name>.+)-(?<version>\d+\.\d+\.\d+)-(?<platform>[a-z0-9]+)-(?<arch>[a-z0-9]+)(\.exe)?$",
        RegexOptions.Compiled);

    /// <summary>
    /// Plugin name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Full URI to plugin file.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Plugin version. It will be "0.0" if not defined.
    /// </summary>
    public Version Version { get; set; } = new();

    /// <summary>
    /// Plugin platform.
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Plugin architecture.
    /// </summary>
    public string Architecture { get; set; } = string.Empty;

    /// <summary>
    /// Is plugin installed and ready to use.
    /// </summary>
    public bool IsInstalled { get; set; }

    public PluginInfo(string name)
    {
        Name = name;
    }

    public static PluginInfo CreateFromUniversalName(string name)
    {
        var match = NewKeyRegex.Match(name);
        if (match.Success)
        {
            return new PluginInfo(match.Groups["name"].Value)
            {
                Version = new Version(match.Groups["version"].Value),
                Platform = match.Groups["platform"].Value,
                Architecture = match.Groups["arch"].Value,
            };
        }
        match = KeyRegex.Match(name);
        if (match.Success)
        {
            return new PluginInfo(match.Groups["name"].Value)
            {
                Version = new Version(match.Groups["version"].Value),
                Architecture = "msil",
                Platform = "multi"
            };
        }
        return new PluginInfo(Path.GetFileName(name))
        {
            Version = new Version(0, 0),
            Architecture = "msil",
            Platform = "multi"
        };
    }

    public bool IsEqualTo(PluginInfo pluginInfo)
    {
        return pluginInfo.Name == this.Name && pluginInfo.Version == this.Version
            && pluginInfo.Platform.Equals(this.Platform) && pluginInfo.Architecture.Equals(this.Architecture);
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name}-{Version}";
}
