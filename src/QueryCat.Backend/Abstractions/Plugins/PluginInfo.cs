using System.Text.RegularExpressions;

namespace QueryCat.Backend.Abstractions.Plugins;

/// <summary>
/// Plugin information. Usually we get it from file name. Right now the following patterns are supported:
/// - QCat.Plugins.Yandex-0.1.0-linux-x64.
/// - QCat.Plugins.Yandex-0.1.0-win-x64.exe.
/// - QCat.Plugins.Yandex.0.1.0.nupkg.
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
    public string Platform { get; set; } = Application.PlatformUnknown;

    /// <summary>
    /// Plugin architecture.
    /// </summary>
    public string Architecture { get; set; } = Application.ArchitectureUnknown;

    /// <summary>
    /// Is plugin installed and ready to use.
    /// </summary>
    public bool IsInstalled { get; set; }

    private PluginInfo(string name)
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
                Architecture = Application.ArchitectureMsil,
                Platform = Application.PlatformMulti,
            };
        }
        return new PluginInfo(Path.GetFileName(name))
        {
            Version = new Version(0, 0),
            Architecture = Application.ArchitectureUnknown,
            Platform = Application.PlatformUnknown,
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
