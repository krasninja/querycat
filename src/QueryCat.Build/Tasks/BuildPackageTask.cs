using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Core.IO.Arguments;
using Cake.Frosting;
using Cake.Git;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Package")]
[TaskDescription("Build NuGet package")]
public sealed class BuildPackageTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var currentSha = context.GitLogTip("../../").Sha;
        context.DotNetPack(context.BackendProjectDirectory, new DotNetPackSettings
        {
            NoLogo = true,
            OutputDirectory = context.OutputDirectory,
            Configuration = DotNetConstants.ConfigurationRelease,
            // https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets#packing-using-a-nuspec
            ArgumentCustomization = pag =>
            {
                pag.Append(new TextArgument("-p:NuspecFile=QueryCat.nuspec"));
                pag.Append(new TextArgument($"-p:NuspecProperties=\"version={context.Version};CommitHash={currentSha}\""));
                return pag;
            },
        });
        context.DotNetPack(context.PluginClientProjectDirectory, new DotNetPackSettings
        {
            NoLogo = true,
            OutputDirectory = context.OutputDirectory,
            Configuration = DotNetConstants.ConfigurationRelease,
            ArgumentCustomization = pag =>
            {
                pag.Append(new TextArgument("-p:NuspecFile=QueryCat.Plugins.Client.nuspec"));
                pag.Append(new TextArgument($"-p:NuspecProperties=\"CommitHash={currentSha}\""));
                return pag;
            },
        });

        return base.RunAsync(context);
    }
}
