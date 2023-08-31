using Cake.Common.IO;
using Cake.Common.IO.Paths;
using Cake.Common.Tools.GitVersion;
using Cake.Core;
using Cake.Frosting;

namespace QueryCat.Build;

/// <summary>
/// The main build settings (version, etc).
/// </summary>
public class BuildContext : FrostingContext
{
    public bool Delay { get; }

    public ConvertableDirectoryPath ConsoleAppProjectDirectory => this.Directory("../QueryCat.Cli");

    public ConvertableDirectoryPath TimeItAppProjectDirectory => this.Directory("../TimeIt");

    public ConvertableDirectoryPath TestPluginAppProjectDirectory => this.Directory("../QueryCat.Plugin.Test");

    public ConvertableDirectoryPath OutputDirectory => this.Directory("../../output");

    public string Version { get; }

    public BuildContext(ICakeContext context) : base(context)
    {
        Delay = context.Arguments.HasArgument("delay");
        Version = context.Arguments.GetArgument("version") ?? GetGitVersion();
    }

    private string GetGitVersion()
    {
        var gitVersion = this.GitVersion();
        return gitVersion.SemVer;
    }
}
